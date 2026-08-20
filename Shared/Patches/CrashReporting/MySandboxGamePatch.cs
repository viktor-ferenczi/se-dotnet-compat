using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Sandbox;

namespace Shared.Patches.CrashReporting;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MySandboxGame))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MySandboxGamePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MySandboxGame.InitModAPI))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private static bool InitModAPIPrefix()
    {
        // Keen's wrapper swallows useful errors and enables the obsolete hotfix popup.
        MySandboxGame.InitIlCompiler();
        MySandboxGame.InitIlChecker();
        return false;
    }
}
