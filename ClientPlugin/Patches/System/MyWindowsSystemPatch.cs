using System;
using HarmonyLib;
using VRage.Platform.Windows.Sys;

namespace ClientPlugin.Patches.System;

[HarmonyPatch(typeof(MyWindowsSystem))]
public static class MyWindowsSystemPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("GetOsName")]
    private static bool GetOsNamePrefix(ref string __result)
    {
        // Disabled due to:
        // System.Runtime.InteropServices.MarshalDirectiveException: Cannot marshal 'parameter #3': Cannot marshal a string by-value with the [Out] attribute.
        __result = "Windows";
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("LogEnvironmentInformation")]
    private static bool LogEnvironmentInformationPrefix()
    {
        // Prevent crash due to broken C API binding
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("GetInfoCPU")]
    private static bool GetInfoCPUPrefix(MyWindowsSystem __instance, out uint frequency, out uint physicalCores, ref string __result)
    {
        // Replacement implementation

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

        // Do not call the original
        return false;
    }
}