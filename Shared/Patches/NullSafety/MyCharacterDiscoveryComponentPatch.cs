using HarmonyLib;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SpaceEngineers.Game.EntityComponents.GameLogic.Discovery;

namespace Shared.Patches.NullSafety;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyCharacterDiscoveryComponent))]
public static class MyCharacterDiscoveryComponentPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnFactionDiscovered")]
    private static bool OnFactionDiscoveredPrefix()
    {
        return !Sync.IsDedicated && MySession.Static?.LocalHumanPlayer != null;
    }
}
