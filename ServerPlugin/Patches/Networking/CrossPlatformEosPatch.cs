using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Sandbox;
using VRage.Dedicated;

namespace ServerPlugin.Patches.Networking;

// Makes the Dedicated Server host crossplay worlds over EOS.
//
// Stock Space Engineers only initializes EOS networking — and the public,
// advertised EOS lobby that crossplay clients discover the server through —
// when SpaceEngineersDedicated.MyProgram.IsEOS() is true, i.e. when the config
// NetworkType is "eos" (or "-eos" is on the command line). The CrossPlatform
// config flag on its own only marks the world as console-compatible content; it
// does NOT switch the transport to EOS. So a CrossPlatform world hosted with the
// default NetworkType="steam" registers on Steam only and stays invisible to
// EOS/console (crossplay) clients — exactly the "clients don't see it" symptom.
//
// We treat CrossPlatform=true as the intent to host a crossplay game and switch
// the server onto EOS networking, the only transport that lets both Steam and
// console/EOS players find and join the same server. (The client side of this
// is enabled by the ClientPlugin EOS-connect fix.)
//
// Hook point: DedicatedServer.RunInternal does, in order,
//     ConfigDedicated.Load();          // config is now populated
//     InitConsoleCompatibility();      // <-- we postfix here
//     InitializeServices(true);        // reads MyProgram.IsEOS() -> NetworkType
// so a postfix on InitConsoleCompatibility sets NetworkType while the config is
// loaded and before the EOS-vs-Steam routing decision is made. The patch lives
// in the "Finish" category (applied by the preloader before MyProgram.Main runs,
// and InitializeServices runs before the plugins' IPlugin.Init / "Init" phase).
// DedicatedServer is a loaded VRage.Dedicated type at that point, unlike the
// internal MyProgram in the not-yet-loaded SpaceEngineersDedicated.exe.
//
// ReSharper disable once UnusedType.Global
[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(DedicatedServer), nameof(DedicatedServer.InitConsoleCompatibility))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class CrossPlatformEosPatch
{
    // ReSharper disable once UnusedMember.Local
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
