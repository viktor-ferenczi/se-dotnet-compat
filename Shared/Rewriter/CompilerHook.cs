using HarmonyLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Scripting;

#if DEDICATED
using ServerPlugin.Rewriter;
#else
using ClientPlugin.Rewriter;
#endif

#if DEDICATED
namespace ServerPlugin.Rewriter
#else
namespace ClientPlugin.Rewriter
#endif
{
    /// <summary>
    /// Rewriters registered by other plugins. Each one runs after the built-in fixes.
    /// </summary>
    public static class CompilerHookExtensions
    {
        public static readonly List<Func<SemanticModel, CSharpSyntaxRewriter>> RewriterFactories = [];

        /// <summary>
        /// Holds the API target while the asynchronous Compile call is running.
        /// </summary>
        public static readonly AsyncLocal<MyApiTarget?> CurrentTarget = new();
    }
}

#if DEDICATED
[HarmonyPatchCategory("Finish")]
#else
[HarmonyPatchCategory("Init")]
#endif
[HarmonyPatch(typeof(MyScriptCompiler), "Compile")]
static class Compile_Prefix
{
    public static void Prefix(MyApiTarget target)
    {
        CompilerHookExtensions.CurrentTarget.Value = target;
    }
}

#if DEDICATED
[HarmonyPatchCategory("Finish")]
#else
[HarmonyPatchCategory("Init")]
#endif
[HarmonyPatch(typeof(MyScriptCompiler), "CreateCompilation")]
static class CreateCompilation_Prefix
{
    public static bool Prefix(
        MyScriptCompiler __instance,
        string assemblyFileName,
        IEnumerable<Script> scripts,
        bool enableDebugInformation,
        ref CSharpCompilation __result)
    {
        CSharpCompilationOptions options = ((enableDebugInformation || __instance.EnableDebugInformation) ? __instance.m_debugCompilationOptions : __instance.m_runtimeCompilationOptions);
        IEnumerable<SyntaxTree> syntaxTrees = null;
        if (scripts != null)
        {
            CSharpParseOptions parseOptions = __instance.m_conditionalParseOptions.WithPreprocessorSymbols(__instance.m_conditionalCompilationSymbols);
            syntaxTrees = GetRewrittenTrees(__instance, [.. scripts], parseOptions, options, assemblyFileName);
        }
        __result = CSharpCompilation.Create(MyScriptCompiler.MakeAssemblyName(assemblyFileName), syntaxTrees, __instance.m_metadataReferences, options);
        return false;
    }

    public static IEnumerable<SyntaxTree> GetRewrittenTrees(MyScriptCompiler __instance, List<Script> scripts, CSharpParseOptions parseOptions, CSharpCompilationOptions options, string assemblyFileName)
    {
        var target = CompilerHookExtensions.CurrentTarget.Value;
        CompilerHookExtensions.CurrentTarget.Value = null;

        List<SyntaxTree> initialTrees = [.. scripts.Select((Script s) => CSharpSyntaxTree.ParseText(s.Code, parseOptions, s.Name, Encoding.UTF8))];
        var analysisCompilation = CSharpCompilation.Create("compat-analysis-compilation", initialTrees, __instance.m_metadataReferences, options);

        // Do not rewrite working mods unless another plugin explicitly asked to.
        if (analysisCompilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            Dictionary<SyntaxTree, SemanticModel> initialTreeInfo = initialTrees.ToDictionary(tree => tree, tree => analysisCompilation.GetSemanticModel(tree));

            Dictionary<IMethodSymbol, List<IMethodSymbol>> conflicts = [];
            foreach (var tree in initialTrees)
            {
                var model = initialTreeInfo[tree];
                var collector = new ConflictingExtensionCollector(model, conflicts);
                collector.Visit(tree.GetRoot());
            }

            // These models belong to the original trees and cannot be reused afterward.
            initialTrees = [.. initialTrees.Select(tree => {
                var model = initialTreeInfo[tree];
                var rewriter = new ConflictingExtensionRewriter(model, conflicts);
                var newRoot = rewriter.Visit(tree.GetRoot());
                return CSharpSyntaxTree.Create((CSharpSyntaxNode)newRoot, parseOptions, tree.FilePath, Encoding.UTF8);;
            })];
        }

        // External rewriters are for mods, not programmable blocks or test compilations.
        if (target == MyApiTarget.Mod)
        {
            var factories = CompilerHookExtensions.RewriterFactories;
            if (factories.Count > 0)
            {
                CSharpCompilation passCompilation = null;
                bool needsRecompilation = true;

                foreach (var factory in factories)
                {
                    if (needsRecompilation)
                    {
                        passCompilation = CSharpCompilation.Create(
                            "compat-external-pass", initialTrees, __instance.m_metadataReferences, options);
                        needsRecompilation = false;
                    }

                    bool changedThisPass = false;
                    initialTrees = [.. initialTrees.Select(tree => {
                        var rewriter = factory(passCompilation.GetSemanticModel(tree));
                        if (rewriter == null)
                            return tree;
                        var oldRoot = tree.GetRoot();
                        var newRoot = rewriter.Visit(oldRoot);
                        if (ReferenceEquals(newRoot, oldRoot))
                            return tree;
                        changedThisPass = true;
                        return CSharpSyntaxTree.Create((CSharpSyntaxNode)newRoot, parseOptions, tree.FilePath, Encoding.UTF8);
                    })];

                    if (changedThisPass)
                        needsRecompilation = true;
                }
            }
        }

#if DEBUG && DUMP_REWRITTEN_CODE
        DebugSaveRewrittenCode(initialTrees, assemblyFileName);
#endif

        return initialTrees;
    }

    private static void DebugSaveRewrittenCode(List<SyntaxTree> initialTrees, string assemblyFileName)
    {
        // Keep the source tree relative to the common directory.
        try
        {
            string modDirName = SanitizePathSegment(
                System.IO.Path.GetFileName(assemblyFileName ?? ""));
            if (string.IsNullOrWhiteSpace(modDirName))
                modDirName = "_unknown_mod";

            string dumpDir = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "DotNetCompat_Rewritten",
                modDirName);
            System.IO.Directory.CreateDirectory(dumpDir);

            int commonPrefixLen = ComputeCommonPrefixLength(
                initialTrees
                    .Select(t => t.FilePath)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(SplitPath)
                    .ToList());

            var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tree in initialTrees)
            {
                string relDir = "";
                string name = null;
                if (!string.IsNullOrWhiteSpace(tree.FilePath))
                {
                    name = System.IO.Path.GetFileName(tree.FilePath);
                    if (!name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        name += ".cs";

                    var segments = SplitPath(tree.FilePath);
                    var relSegments = segments
                        .Skip(commonPrefixLen)
                        .Take(Math.Max(0, segments.Count - commonPrefixLen - 1))
                        .Select(SanitizePathSegment)
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToArray();
                    if (relSegments.Length > 0)
                        relDir = string.Join(System.IO.Path.DirectorySeparatorChar.ToString(), relSegments);
                }
                if (string.IsNullOrWhiteSpace(name))
                    name = Guid.NewGuid().ToString("N") + ".cs";

                string targetDir = string.IsNullOrEmpty(relDir)
                    ? dumpDir
                    : System.IO.Path.Combine(dumpDir, relDir);
                System.IO.Directory.CreateDirectory(targetDir);

                string finalName = name;
                int suffix = 1;
                string key = System.IO.Path.Combine(targetDir, finalName);
                while (!usedPaths.Add(key))
                {
                    finalName = System.IO.Path.GetFileNameWithoutExtension(name)
                                + "_" + suffix++ + ".cs";
                    key = System.IO.Path.Combine(targetDir, finalName);
                }

                System.IO.File.WriteAllText(
                    key,
                    tree.GetRoot().ToFullString(),
                    Encoding.UTF8);
            }
        }
        catch
        {
            // A failed debug dump must not break mod compilation.
        }
    }

    private static List<string> SplitPath(string path) =>
        [.. path.Replace('\\', '/')
            .Split(['/'], StringSplitOptions.RemoveEmptyEntries)];

    private static int ComputeCommonPrefixLength(List<List<string>> paths)
    {
        if (paths.Count == 0)
            return 0;
        if (paths.Count == 1)
            return Math.Max(0, paths[0].Count - 1);

        int min = paths.Min(p => p.Count);
        int max = Math.Max(0, min - 1);
        int prefix = 0;
        while (prefix < max)
        {
            string s = paths[0][prefix];
            bool allMatch = paths.All(p => string.Equals(p[prefix], s, StringComparison.OrdinalIgnoreCase));
            if (!allMatch)
                break;
            prefix++;
        }
        return prefix;
    }

    private static string SanitizePathSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return segment;
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(segment.Length);
        foreach (var ch in segment)
            sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
        return sb.ToString();
    }
}
