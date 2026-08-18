using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Replication.StateGroups;
using VRage.Network;
using VRage.Sync;

namespace Shared.Patches.NullSafety;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyPropertySyncStateGroup))]
public static class MyPropertySyncStateGroupPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(MethodType.Constructor, typeof(IMyReplicable), typeof(SyncType))]
    private static bool ConstructorPrefix()
    {
        return !Sandbox.Game.Multiplayer.Sync.IsServer || MyMultiplayer.Static != null;
    }
}
