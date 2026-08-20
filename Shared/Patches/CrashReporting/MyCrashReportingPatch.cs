using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using VRage.Platform.Windows;
using VRage;

namespace Shared.Patches.CrashReporting;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MyCrashReporting))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyCrashReportingPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("PrepareCrashAnalyticsReporting")]
    private static bool PrepareCrashAnalyticsReportingPrefix()
    {
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("ExtractCrashAnalyticsReport")]
    private static bool ExtractCrashAnalyticsReportPrefix(out bool exitAfterReport, out string logPath, out CrashInfo info, out bool isUnsupportedGpu, ref bool __result)
    {
        logPath = null;
        info = default;
        isUnsupportedGpu = false;
        exitAfterReport = false;
        __result = false;
        return false;
    }
}
