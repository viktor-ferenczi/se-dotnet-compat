using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using HarmonyLib;
using Sandbox;

namespace ServerPlugin.Patches.CrashReporting;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MyInitializer))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyInitializerServerPatch
{
    [DllImport("kernel32.dll")]
    private static extern uint SetErrorMode(uint uMode);

    private const uint SEM_NOGPFAULTERRORBOX = 0x0002;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyInitializer.InitExceptionHandling))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private static void InitExceptionHandlingPrefix()
    {
        // Windows-only API. The prefix used to be dead on every platform (it was
        // applied after InitExceptionHandling had already run), so the Linux
        // server never reached this P/Invoke before.
        if (OperatingSystem.IsWindows())
            SetErrorMode(SEM_NOGPFAULTERRORBOX);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyInitializer.OnCrash))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private static bool OnCrashPrefix(string logPath, Exception exception, bool oom)
    {
        try
        {
            Console.Error.WriteLine();

            if (oom)
                Console.Error.WriteLine("FATAL: Out of memory");

            if (exception != null)
                Console.Error.WriteLine(
                    $"FATAL: Unhandled exception:{Environment.NewLine}{exception}"
                );
            else
                Console.Error.WriteLine("FATAL: Native crash");

            if (logPath != null)
                Console.Error.WriteLine($"Log: {logPath}");

            Console.Error.Flush();
        }
        catch { }

        Environment.Exit(1);
        return false;
    }
}
