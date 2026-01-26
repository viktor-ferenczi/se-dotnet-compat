using ClientPlugin.Rewriter;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Scripting;

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
            syntaxTrees = GetRewrittenTrees(__instance, [.. scripts], parseOptions, options);
        }
        __result = CSharpCompilation.Create(MyScriptCompiler.MakeAssemblyName(assemblyFileName), syntaxTrees, __instance.m_metadataReferences, options);
        return false;
    }

    public static IEnumerable<SyntaxTree> GetRewrittenTrees(MyScriptCompiler __instance, List<Script> scripts, CSharpParseOptions parseOptions, CSharpCompilationOptions options)
    {
        List<SyntaxTree> initialTrees = [.. scripts.Select((Script s) => CSharpSyntaxTree.ParseText(s.Code, parseOptions, s.Name, Encoding.UTF8))];
        var analysisCompilation = CSharpCompilation.Create("compat-analysis-compilation", initialTrees, __instance.m_metadataReferences, options);

        // Prevent rewriter edge cases from affecting mods who's compilation succeeded
        if (!analysisCompilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            return initialTrees;

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

    //    System.IO.File.WriteAllText(
    //System.IO.Path.Combine(
    //    System.Environment.GetFolderPath(
    //        System.Environment.SpecialFolder.UserProfile),
    //    "Downloads\\dump.txt"),
    //initialTrees.Where(x=>x.FilePath.Contains("SettingsMenu.cs") && x.FilePath.Contains("Input")).First().GetRoot().ToString());

        return initialTrees;
    }
}
