using HarmonyLib;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.SessionComponents.Clipboard;

namespace ClientPlugin.Patches.NullSafety;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyGridClipboard))]
// ReSharper disable once UnusedType.Global
public static class MyGridClipboardPatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyPrefix]
    [HarmonyPatch("Deactivate")]
    private static bool DeactivatePrefix()
    {
        return MyClipboardComponent.Static != null;
    }
}