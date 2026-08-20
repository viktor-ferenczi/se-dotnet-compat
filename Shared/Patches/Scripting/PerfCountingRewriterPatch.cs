using HarmonyLib;
using VRage.Scripting.Rewriters;

namespace Shared.Patches.Scripting;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(PerfCountingRewriter))]
public static class PerfCountingRewriterPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PerfCountingRewriter.Rewrite))]
    private static bool RewritePrefix(
        Microsoft.CodeAnalysis.SyntaxTree syntaxTree,
        out Microsoft.CodeAnalysis.SyntaxTree __result
    )
    {
        // Performance counting references the missing VRage.Scripting.CompilerMethods type.
        __result = syntaxTree;
        return false;
    }
}
