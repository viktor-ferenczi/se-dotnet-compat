using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using VRage.Platform.Windows;
using VRage;

namespace ClientPlugin.Patches.CrashReporting;

[HarmonyPatch(typeof(MyCrashReporting))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyCrashReportingPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("PrepareCrashAnalyticsReporting")]
    // ReSharper disable once UnusedMember.Local
    private static bool PrepareCrashAnalyticsReportingPrefix()
    {
        // Crash reporting has been disabled
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("ExtractCrashAnalyticsReport")]
    // ReSharper disable once RedundantAssignment
    private static bool ExtractCrashAnalyticsReportPrefix(out bool exitAfterReport, out string logPath, out CrashInfo info, out bool isUnsupportedGpu, ref bool __result)
    {
        // Crash reporting has been disabled
        logPath = null;
        info = default;
        isUnsupportedGpu = false;
        exitAfterReport = false;
        __result = false;
        return false;
    }
}