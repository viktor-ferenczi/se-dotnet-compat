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
            syntaxTrees = scripts.Select((Script s) => GetRewrittenTree(__instance, parseOptions, s));
        }
        __result = CSharpCompilation.Create(MyScriptCompiler.MakeAssemblyName(assemblyFileName), syntaxTrees, __instance.m_metadataReferences, options);
        return false;
    }

    public static SyntaxTree GetRewrittenTree(MyScriptCompiler __instance, CSharpParseOptions options, Script s)
    {
        var tree = CSharpSyntaxTree.ParseText(s.Code, options, s.Name, Encoding.UTF8);

        var root = tree.GetRoot();
        var rewriter = new RemoveUsingRewriter();
        var newRoot = rewriter.Visit(root);

        return CSharpSyntaxTree.Create(
            (CSharpSyntaxNode)newRoot,
            options,
            s.Name,
            Encoding.UTF8);
    }
}
