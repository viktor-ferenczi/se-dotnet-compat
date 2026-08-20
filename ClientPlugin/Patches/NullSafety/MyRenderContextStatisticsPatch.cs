using HarmonyLib;
using VRage.Render11.RenderContext;

namespace ClientPlugin.Patches.NullSafety;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyRenderContextStatistics))]
public static class MyRenderContextStatisticsPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyRenderContextStatistics.Gather))]
    private static bool GatherPrefix(MyRenderContextStatistics other)
    {
        return other != null;
    }
}
