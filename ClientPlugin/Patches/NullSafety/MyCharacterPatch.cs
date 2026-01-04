using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;

namespace ClientPlugin.Patches.NullSafety;

[HarmonyPatch(typeof(MyCharacter))]
// ReSharper disable once UnusedType.Global
public static class MyCharacterPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnControlReleased")]
    // ReSharper disable once UnusedMember.Local
    private static bool OnControlReleasedPrefix()
    {
        // Prevent crash in MyCubeBuilder.Static.Deactivate()
        return MyCubeBuilder.Static != null;
    }
}