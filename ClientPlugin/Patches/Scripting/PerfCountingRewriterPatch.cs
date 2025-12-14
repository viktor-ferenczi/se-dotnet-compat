using HarmonyLib;
using VRage.Scripting.Rewriters;

namespace ClientPlugin.Patches.Scripting;

[HarmonyPatch(typeof(PerfCountingRewriter))]
// ReSharper disable once UnusedType.Global
public static class PerfCountingRewriterPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Rewrite")]
    // ReSharper disable once UnusedMember.Local
    private static bool RewritePrefix(Microsoft.CodeAnalysis.SyntaxTree syntaxTree, out Microsoft.CodeAnalysis.SyntaxTree __result)
    {
        // Disabled performance counting, otherwise mod compilation fails with repeated diagnostic error messages:
        // The type or namespace name 'CompilerMethods' does not exist in the namespace 'VRage.Scripting' (are you missing an assembly reference?)
        __result = syntaxTree;
        return false;
    }
}