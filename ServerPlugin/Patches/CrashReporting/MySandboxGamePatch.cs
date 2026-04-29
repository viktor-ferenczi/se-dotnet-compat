using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Sandbox;

namespace ServerPlugin.Patches.CrashReporting;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MySandboxGame))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MySandboxGamePatch
{
    [HarmonyPrefix]
    [HarmonyPatch("InitModAPI")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private static bool InitModAPIPrefix()
    {
        // Replacement with no error handling, so it does not hide initialization errors and they can be debugged
        MySandboxGame.InitIlCompiler();
        MySandboxGame.InitIlChecker();

        // Do NOT ever set ShowHotfixPopup!

        // Replacement patch, do not call the original
        return false;
    }

    // OnDotNetHotfixPopupClosed patch from the client plugin is not ported - the dedicated server has no GUI popups.
}
