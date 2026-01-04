using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Replication.StateGroups;
using VRage.Network;
using VRage.Sync;

namespace ClientPlugin.Patches.NullSafety;

[HarmonyPatch(typeof(MyPropertySyncStateGroup))]
// ReSharper disable once UnusedType.Global
public static class MyPropertySyncStateGroupPatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyPrefix]
    [HarmonyPatch(MethodType.Constructor, typeof(IMyReplicable), typeof(SyncType))]
    private static bool ConstructorPrefix()
    {
        return !Sandbox.Game.Multiplayer.Sync.IsServer || MyMultiplayer.Static != null;
    }
}