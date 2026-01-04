// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Reflection;
using HarmonyLib;
using System.Collections.Generic;
using ClientPlugin.Patches.ImageProcessing;
using ClientPlugin.Patches.NullSafety;
using ClientPlugin.Patches.Serialization;
using Mono.Cecil;

// IMPORTANT: MUST NOT USE A NAMESPACE, otherwise Pulsar won't find the Preloader class! 
//namespace ClientPlugin;

// ReSharper disable once UnusedType.Global
public static class Preloader
{
    // ReSharper disable once UnusedMember.Global
    public static IEnumerable<string> TargetDLLs { get; } =
    [
        // Game DLLs
        "HavokWrapper.dll",
        "Sandbox.Common.dll",
        "Sandbox.Game.dll",
        "Sandbox.Graphics.dll",
        "SpaceEngineers.Game.dll",
        "VRage.dll",
        "VRage.Audio.dll",
        "VRage.Game.dll",
        "VRage.Input.dll",
        "VRage.Library.dll",
        "VRage.Math.dll",
        "VRage.Network.dll",
        "VRage.Platform.Windows.dll",
        "VRage.Render.dll",
        "VRage.Render11.dll",
        "VRage.Scripting.dll",

        // Dependency DLLs
        "SharpDX.dll",
        "SharpDX.DXGI.dll",
        "SharpDX.XAudio2.dll",
        "SixLabors.ImageSharp.dll"
    ];

    // ReSharper disable once UnusedMember.Global
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static void Patch(AssemblyDefinition asmDef)
    {
        AppContext.SetSwitch("System.Reflection.AssemblyLoadContext.EnableDiagnostics", true);

        DecodePixelDataPrepatch.Prepatch(asmDef);
        MyHeightMapLoadingSystemPrepatch.Prepatch(asmDef);
        XmlSerializationPrepatch.Prepatch(asmDef);
    }

    // ReSharper disable once UnusedMember.Global
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static void Finish()
    {
        // Enabling BinaryFormatter. This may not work on .Net 9
        // See https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide
        AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);

#if DEBUG && HARMONY_DEBUG
        Harmony.DEBUG = true;
#endif

        var harmony = new Harmony("DotNetCompat");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}