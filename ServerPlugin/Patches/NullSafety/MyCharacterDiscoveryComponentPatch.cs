using HarmonyLib;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SpaceEngineers.Game.EntityComponents.GameLogic.Discovery;

namespace ServerPlugin.Patches.NullSafety;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyCharacterDiscoveryComponent))]
// ReSharper disable once UnusedType.Global
public static class MyCharacterDiscoveryComponentPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnFactionDiscovered")]
    // ReSharper disable once UnusedMember.Local
    private static bool OnFactionDiscoveredPrefix()
    {
        // Prevent crash when LocalHumanPlayer is null
        return !Sync.IsDedicated && MySession.Static?.LocalHumanPlayer != null;
    }
}
