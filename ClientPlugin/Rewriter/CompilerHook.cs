using ClientPlugin.Rewriter;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Scripting;

namespace ClientPlugin.Rewriter
{
    /// <summary>
    /// Extension point for other plugins to register additional Roslyn
    /// rewriters that run after the built-in mod compilation passes.
    ///
    /// Other plugins (e.g. se-linux-compat's path-substitution pass) locate
    /// this type by reflection from <c>AppDomain.CurrentDomain</c>, fetch the
    /// <see cref="RewriterFactories"/> field, and append a factory delegate.
    /// Each factory is invoked once per syntax tree with that tree's
    /// <see cref="SemanticModel"/>; the returned rewriter visits the tree and
    /// the result replaces the tree in the compilation.
    ///
    /// Registration must happen before <see cref="MyScriptCompiler"/> compiles
    /// any mod (the patched <c>CreateCompilation</c> is the consumer). Plugin
    /// <c>Init</c> hooks are the natural place — they all run before the game
    /// loads any session.
    /// </summary>
    public static class CompilerHookExtensions
    {
        public static readonly List<Func<SemanticModel, CSharpSyntaxRewriter>> RewriterFactories = [];

        /// <summary>
        /// Captures the <see cref="MyApiTarget"/> of the currently running
        /// <c>MyScriptCompiler.Compile</c> call so that the
        /// <c>CreateCompilation</c> prefix (which does not receive the target
        /// itself) can decide whether to run external rewriters. External
        /// rewriters target the mod API and must not run for in-game
        /// (Programmable Block) scripts or for unrestricted test compilations.
        ///
        /// <see cref="AsyncLocal{T}"/> rather than <c>[ThreadStatic]</c>:
        /// <c>Compile</c> is async, and the value must flow into the
        /// synchronous <c>CreateCompilation</c> call that runs inside it.
        ///
        /// Lifetime: set by the <c>Compile</c> prefix and cleared back to
        /// <c>null</c> by the <c>CreateCompilation</c> prefix the moment it
        /// has read the value. Nothing further in the <c>Compile</c> flow
        /// needs it, so this confines the value to a single synchronous
        /// span and prevents the
        /// <see cref="System.Threading.ExecutionContext"/> of the caller
        /// from carrying a stale target indefinitely.
        /// </summary>
        public static readonly AsyncLocal<MyApiTarget?> CurrentTarget = new();
    }
}

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyScriptCompiler), "Compile")]
static class Compile_Prefix
{
    public static void Prefix(MyApiTarget target)
    {
        CompilerHookExtensions.CurrentTarget.Value = target;
    }
}

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyScriptCompiler), "CreateCompilation")]
static class CreateCompilation_Prefix
{
    // Copied from Keen. TODO: Replace with a transpiler on the Select
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
        // Read-and-clear: from this point on, no code path needs the
        // captured target, so we drop it immediately to avoid leaving a
        // value in the caller's ExecutionContext (AsyncLocal would
        // otherwise keep the last-set target alive on any context that
        // forked off this thread).
        var target = CompilerHookExtensions.CurrentTarget.Value;
        CompilerHookExtensions.CurrentTarget.Value = null;

        List<SyntaxTree> initialTrees = [.. scripts.Select((Script s) => CSharpSyntaxTree.ParseText(s.Code, parseOptions, s.Name, Encoding.UTF8))];
        var analysisCompilation = CSharpCompilation.Create("compat-analysis-compilation", initialTrees, __instance.m_metadataReferences, options);

        // Built-in conflict-fixing passes only run when the unmodified mod
        // already fails to compile — they are last-ditch repairs ("prevent
        // rewriter edge cases from affecting mods who's compilation
        // succeeded"). External rewriter passes (below) are unconditional
        // because their job is platform translation, not error recovery.
        if (analysisCompilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            Dictionary<SyntaxTree, SemanticModel> initialTreeInfo = initialTrees.ToDictionary(tree => tree, tree => analysisCompilation.GetSemanticModel(tree));

            // Scan for clashing extension methods
            Dictionary<IMethodSymbol, List<IMethodSymbol>> conflicts = [];
            foreach (var tree in initialTrees)
            {
                var model = initialTreeInfo[tree];
                var collector = new ConflictingExtensionCollector(model, conflicts);
                collector.Visit(tree.GetRoot());
            }

            // These will replace the tree objects and break the above dictionaries
            // ORDER OF EXECUTION MATTERS!

            // Fix clashing extension methods
            initialTrees = [.. initialTrees.Select(tree => {
                var model = initialTreeInfo[tree];
                var rewriter = new ConflictingExtensionRewriter(model, conflicts);
                var newRoot = rewriter.Visit(tree.GetRoot());
                return CSharpSyntaxTree.Create((CSharpSyntaxNode)newRoot, parseOptions, tree.FilePath, Encoding.UTF8);;
            })];
        }

        // External rewriter passes registered by other plugins (e.g.
        // se-linux-compat substituting System.IO.Path with its WindowsPath
        // shim). These run on every mod that compiles, not just broken
        // ones, because they perform platform translation that the mod
        // author cannot supply.
        //
        // Restricted to MyApiTarget.Mod: PB (Ingame) scripts and
        // unrestricted (None) compilations are not the right audience for
        // mod-API rewrites and must be left untouched.
        //
        // The passCompilation is rebuilt only when the previous pass
        // actually changed at least one tree; otherwise the trees and
        // therefore the semantic model from the previous pass are still
        // accurate and there is no reason to recompile. A rewriter that
        // does not touch any node returns the same root instance
        // (CSharpSyntaxRewriter contract), so we detect "no change" via
        // ReferenceEquals and keep the original SyntaxTree.
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
        // Diagnostic dump of the rewritten trees. Best-effort: any I/O
        // failure must not break mod compilation, so the whole block is
        // wrapped in a swallowing catch.
        //
        // Layout: <UserProfile>/DotNetCompat_Rewritten/<modName>/<inModRelPath>.
        //
        // <modName> is the basename of the mod assembly path (e.g.
        // "2923698329.sbm_Lima"), not the full path — the caller passes an
        // absolute path in assemblyFileName.
        //
        // <inModRelPath> is the source file's location relative to the
        // common ancestor of all source files in this compilation batch.
        // The common ancestor is effectively the mod's content root, so
        // this reproduces only the structure that exists *inside* the mod
        // (e.g. "Data/Scripts/Foo/Bar.cs") rather than the entire absolute
        // path from /. With a single source file in the batch the common
        // ancestor is its own directory, so it lands directly under
        // <modName>/.
        try
        {
            // The assembly path's basename is the mod ID (e.g. "2923698329.sbm_Lima").
            string modDirName = SanitizePathSegment(
                System.IO.Path.GetFileName(assemblyFileName ?? ""));
            if (string.IsNullOrWhiteSpace(modDirName))
                modDirName = "_unknown_mod";

            string dumpDir = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "DotNetCompat_Rewritten",
                modDirName);
            System.IO.Directory.CreateDirectory(dumpDir);

            // Common-ancestor segment count across all real source paths.
            // Trees with no FilePath are ignored here and later fall back to
            // a GUID name at the mod root.
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

                    // Drop the common ancestor prefix and the file name itself,
                    // leaving only the in-mod directory chain.
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

                // De-duplicate within this batch (across the full relative path).
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
            // Diagnostic-only; never let a dump failure break compilation.
        }
    }

    /// <summary>
    /// Splits a path into segments on both '/' and '\', dropping empties
    /// (so leading separators and double separators contribute nothing).
    /// Segments are returned as-is (no sanitization), since prefix matching
    /// must compare the original on-disk names.
    /// </summary>
    private static List<string> SplitPath(string path) =>
        [.. path.Replace('\\', '/')
            .Split(['/'], StringSplitOptions.RemoveEmptyEntries)];

    /// <summary>
    /// Number of leading segments shared by every input segment list.
    /// Empty input or a single path returns that path's directory length
    /// (count minus the file name) so a singleton batch lands at the root.
    /// </summary>
    private static int ComputeCommonPrefixLength(List<List<string>> paths)
    {
        if (paths.Count == 0)
            return 0;
        if (paths.Count == 1)
            return Math.Max(0, paths[0].Count - 1);

        int min = paths.Min(p => p.Count);
        // Leave at least the file name on the shortest path so callers can
        // still extract a non-empty leaf after the prefix is stripped.
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

    /// <summary>
    /// Replaces characters invalid in a file/directory name with underscores.
    /// </summary>
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
