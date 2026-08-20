using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Sandbox;
using VRage.Dedicated;

namespace ServerPlugin.Patches.Networking;

// CrossPlatform does not select EOS on its own. Set it before services start.
[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(DedicatedServer), nameof(DedicatedServer.InitConsoleCompatibility))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class CrossPlatformEosPatch
{
    [HarmonyPostfix]
    private static void InitConsoleCompatibilityPostfix()
    {
        var config = MySandboxGame.ConfigDedicated;
        if (config is not { CrossPlatform: true })
            return;

        if (string.Equals(config.NetworkType, "eos", StringComparison.OrdinalIgnoreCase))
            return;

        config.NetworkType = "eos";

        Console.WriteLine(
            "[DotNetCompat] CrossPlatform world: switching to EOS networking so crossplay clients can discover this server");
    }
}
