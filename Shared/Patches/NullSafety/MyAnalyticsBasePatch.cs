using System;
using HarmonyLib;
using VRage.Analytics;

namespace Shared.Patches.NullSafety;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MyAnalyticsBase))]
public static class MyAnalyticsBasePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(
        nameof(MyAnalyticsBase.ReportEvent),
        typeof(IMyAnalyticsEvent),
        typeof(DateTime),
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(Exception)
    )]
    private static bool ReportEventPrefix(IMyAnalyticsEvent analyticsEvent)
    {
        // Stock analytics still crashes on .NET 10.
        return false;
    }
}
