using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using VRage.FileSystem;

namespace Shared.Patches.Windows;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(
    typeof(MyFileProviderAggregator),
    nameof(MyFileProviderAggregator.GetFiles),
    [typeof(string), typeof(string), typeof(MySearchOption)]
)]
public static class MyFileProviderAggregatorPatch
{
    public static void Postfix(ref IEnumerable<string> __result)
    {
        // Some callers depend on the ordering returned by .NET Framework.
        __result = [.. __result.Order()];
    }
}
