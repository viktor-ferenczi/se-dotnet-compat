using HarmonyLib;
using Sandbox.Engine.Analytics;

namespace ServerPlugin.Patches.Analytics;

[HarmonyPatchCategory("Init")]
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
