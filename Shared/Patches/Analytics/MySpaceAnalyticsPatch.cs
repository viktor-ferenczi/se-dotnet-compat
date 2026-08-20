using HarmonyLib;
using Sandbox.Engine.Analytics;

namespace Shared.Patches.Analytics;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MySpaceAnalytics))]
public static class MySpaceAnalyticsPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MySpaceAnalytics.StartSession))]
    private static bool StartSessionPrefix()
    {
        return false;
    }
}
