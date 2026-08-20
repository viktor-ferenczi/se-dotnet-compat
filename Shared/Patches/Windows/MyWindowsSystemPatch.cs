using System;
using HarmonyLib;
using VRage.Platform.Windows.Sys;

namespace Shared.Patches.Windows;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MyWindowsSystem))]
public static class MyWindowsSystemPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("GetOsName")]
    private static bool GetOsNamePrefix(ref string __result)
    {
        // The native binding cannot marshal its by-value Out string on .NET 10.
        __result = "Windows";
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("LogEnvironmentInformation")]
    private static bool LogEnvironmentInformationPrefix()
    {
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("GetInfoCPU")]
    private static bool GetInfoCPUPrefix(MyWindowsSystem __instance, out uint frequency, out uint physicalCores, ref string __result)
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
