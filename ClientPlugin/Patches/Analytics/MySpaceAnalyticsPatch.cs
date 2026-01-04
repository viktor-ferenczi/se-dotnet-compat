using HarmonyLib;
using Sandbox.Engine.Analytics;

namespace ClientPlugin.Patches.Analytics;

[HarmonyPatch(typeof(MySpaceAnalytics))]
public static class MySpaceAnalyticsPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("StartSession")]
    // ReSharper disable once UnusedMember.Local
    private static bool StartSessionPrefix()
    {
        // DISABLED ANALYTICS
        return false;
    }
}