using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Sandbox;
using Sandbox.Graphics.GUI;

namespace ClientPlugin.Patches.CrashReporting;

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

    [HarmonyPrefix]
    [HarmonyPatch("OnDotNetHotfixPopupClosed")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private static bool OnDotNetHotfixPopupClosedPrefix(MyGuiScreenMessageBox.ResultEnum result)
    {
        // Process.Start would fail with System.ComponentModel.Win32Exception: The system cannot find the file specified
        // So we skip starting the browser and just close the popup
        MySandboxGame.ClosePopup(result);
        return false;
    }
}