using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using VRage.FileSystem;

namespace ServerPlugin.Patches.Windows;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyFileProviderAggregator), "GetFiles", [typeof(string), typeof(string), typeof(MySearchOption)])]
public static class MyFileProviderAggregatorPatch
{
    public static void Postfix(ref IEnumerable<string> __result)
    {
        // Some functions (eg MyScriptManager.LoadScripts) relied on implicit ordering from Net48
        __result = [.. __result.Order()];
    }
}
