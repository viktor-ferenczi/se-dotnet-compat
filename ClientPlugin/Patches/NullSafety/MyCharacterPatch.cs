using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;

namespace ClientPlugin.Patches.NullSafety;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyCharacter))]
public static class MyCharacterPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyCharacter.OnControlReleased))]
    private static bool OnControlReleasedPrefix()
    {
        return MyCubeBuilder.Static != null;
    }
}
