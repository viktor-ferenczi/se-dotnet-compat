using HarmonyLib;
using VRage.Render11.RenderContext;

namespace ClientPlugin.Patches.NullSafety;

[HarmonyPatch(typeof(MyRenderContextStatistics))]
// ReSharper disable once UnusedType.Global
public static class MyRenderContextStatisticsPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyRenderContextStatistics.Gather))]
    // ReSharper disable once UnusedMember.Local
    private static bool GatherPrefix(MyRenderContextStatistics other)
    {
        // Prevent crash in MyRenderContextStatistics.Gather during game startup
        return other != null;
    }
}