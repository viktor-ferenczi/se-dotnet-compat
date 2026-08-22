using System;
using System.IO;
using System.Runtime.InteropServices;
using HarmonyLib;
using Havok;
using VRage.FileSystem;
using VRage.Library.Threading;

namespace Shared.Patches.Miscellaneous;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(
    typeof(HkBaseSystem),
    nameof(HkBaseSystem.Init),
    [typeof(int), typeof(Action<string>), typeof(bool), typeof(ISharedCriticalSection)]
)]
public static class HkBaseSystemPatch
{
    [HarmonyPrefix]
    private static void InitPrefix()
    {
        if (OperatingSystem.IsWindows())
            NativeLibrary.Load(Path.Combine(MyFileSystem.ExePath, "Havok.dll"));
    }
}
