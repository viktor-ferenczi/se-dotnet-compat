using System;
using HarmonyLib;
using VRage.Platform.Windows.Sys;

namespace Shared.Patches.Windows;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MyWindowsSystem))]
public static class MyWindowsSystemPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyWindowsSystem.GetOsName))]
    private static bool GetOsNamePrefix(ref string __result)
    {
        // The native binding cannot marshal its by-value Out string on .NET 10.
        __result = OperatingSystem.IsLinux() ? "Linux" : "Windows";
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyWindowsSystem.LogEnvironmentInformation))]
    private static bool LogEnvironmentInformationPrefix()
    {
        return false;
    }

    // Both getters P/Invoke GfnRuntimeSdk.dll with no guard. The DLL does not
    // resolve on .NET 10 (a headless server has no reason to ship it anyway),
    // and the analytics session start reaches the first getter during world
    // load. Answer "not on GeForce NOW" without touching the native library.
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyWindowsSystem.IsUsingGeforceNow), MethodType.Getter)]
    private static bool IsUsingGeforceNowPrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyWindowsSystem.IsUsingGeforceNowCloud), MethodType.Getter)]
    private static bool IsUsingGeforceNowCloudPrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyWindowsSystem.GetInfoCPU))]
    private static bool GetInfoCPUPrefix(
        MyWindowsSystem __instance,
        out uint frequency,
        out uint physicalCores,
        ref string __result
    )
    {
        var m_cpuInfo = __instance.m_cpuInfo;
        if (m_cpuInfo.Name == null)
        {
            m_cpuInfo.Cores = (uint)Environment.ProcessorCount;
            m_cpuInfo.Name = $"Generic with {m_cpuInfo.Cores} cores";
            m_cpuInfo.MaxClock = 3600u;
        }

        frequency = m_cpuInfo.MaxClock;
        physicalCores = m_cpuInfo.Cores;
        __result = m_cpuInfo.Name;

        return false;
    }
}
