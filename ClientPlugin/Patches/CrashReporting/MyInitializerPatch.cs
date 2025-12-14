#if DISABLE_CRASH_REPORTING

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pulsar.Shared.Utils;
using HarmonyLib;
using Sandbox;

namespace ClientPlugin.Patches.CrashReporting;

[HarmonyPatch(typeof(MyInitializer))]
// ReSharper disable once UnusedType.Global
public static class MyInitializerPatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyTranspiler]
    [HarmonyPatch("InvokeBeforeRun")]
    private static IEnumerable<CodeInstruction> InvokeBeforeRunTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod)
    {
        var il = instructions.ToList();
        il.RecordOriginalCode(patchedMethod);

        // TODO: If this is a DEBUG build, then remove these pieces of the method:
        // IMySimplifiedErrorReporter creation
        // ErrorPlatform.CleanupCrashAnalytics()
        // MyErrorReporter.UpdateHangAnalytics()
        // UnhandledManagedException handler

        il.RecordPatchedCode(patchedMethod);
        return il;
    }
}
#endif
