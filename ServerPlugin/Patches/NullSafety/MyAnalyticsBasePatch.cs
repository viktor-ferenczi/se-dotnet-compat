using System;
using HarmonyLib;
using VRage.Analytics;

namespace ServerPlugin.Patches.NullSafety;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyAnalyticsBase))]
// ReSharper disable once UnusedType.Global
public static class MyAnalyticsBasePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyAnalyticsBase.ReportEvent),
        typeof(IMyAnalyticsEvent),
        typeof(DateTime),
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(Exception))]
    // ReSharper disable once UnusedMember.Local
    private static bool ReportEventPrefix(IMyAnalyticsEvent analyticsEvent)
    {
        // Prevent crash
        //return analyticsEvent != null;

        // Still crashes on Linux, so do not send the analytics at all
        return false;
    }
}
