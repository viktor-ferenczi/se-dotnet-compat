using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using VRage;
using VRage.Platform.Windows.Forms;

namespace ServerPlugin.Patches.CrashReporting;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MyWindowsWindows))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyWindowsWindowsPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(
        nameof(MyWindowsWindows.MessageBox),
        typeof(string),
        typeof(string),
        typeof(MessageBoxOptions)
    )]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private static bool MessageBoxPrefix(string text, string caption, ref MessageBoxResult __result)
    {
        Console.Error.WriteLine($"[{caption}] {text}");
        Console.Error.Flush();
        __result = MessageBoxResult.Ok;
        return false;
    }
}
