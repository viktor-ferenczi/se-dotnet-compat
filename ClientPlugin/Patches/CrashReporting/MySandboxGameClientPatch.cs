using HarmonyLib;
using Sandbox;
using Sandbox.Graphics.GUI;

namespace ClientPlugin.Patches.CrashReporting;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MySandboxGame), "OnDotNetHotfixPopupClosed")]
public static class MySandboxGameClientPatch
{
    [HarmonyPrefix]
    private static bool Prefix(MyGuiScreenMessageBox.ResultEnum result)
    {
        // Process.Start cannot open the browser on this runtime.
        MySandboxGame.ClosePopup(result);
        return false;
    }
}
