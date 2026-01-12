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

        // Scan for clashing extension methods
        HashSet<IMethodSymbol> conflicts = [];
        foreach (var tree in initialTrees)
        {
            var model = analysisCompilation.GetSemanticModel(tree);
            var collector = new ConflictingExtensionCollector(model);
            collector.Visit(tree.GetRoot());

            conflicts.UnionWith(collector.Conflicts);
        }

        // Replace bad usings (FIXME: use MyScriptManager.m_compatibilityChanges)
        initialTrees = [.. initialTrees.Select(tree => {
            var root = tree.GetRoot();
            var rewriter = new RemoveUsingRewriter();
            var newRoot = rewriter.Visit(root);
            return CSharpSyntaxTree.Create((CSharpSyntaxNode)newRoot, parseOptions, tree.FilePath, Encoding.UTF8);;
        })];

        return initialTrees;
    }
}
