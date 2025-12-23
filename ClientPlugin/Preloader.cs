// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Reflection;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using ClientPlugin;
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
    ];

    // ReSharper disable once UnusedMember.Global
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static void Patch(AssemblyDefinition asmDef)
    {
        Console.WriteLine($"{Plugin.Name}: Prepatch: {asmDef.Name}");

        AppContext.SetSwitch("System.Reflection.AssemblyLoadContext.EnableDiagnostics", true);

#if SIXLABORS_FIXES
        MyImagePrepatch.Prepatch(asmDef);
        MyTextureDataPrepatch.Prepatch(asmDef);
#endif

#if NULLABILITY_FIXES
        MyHeightMapLoadingSystemPrepatch.Prepatch(asmDef);
#endif

#if XML_FIXES
        XmlSerializationPrepatch.Prepatch(asmDef);
#endif
    }

    // ReSharper disable once UnusedMember.Global
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static void Hook()
    {
        Console.WriteLine($"{Plugin.Name}: Hook");

        // Enabling BinaryFormatter. This may not work on .Net 9
        // See https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide
        AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);

#if DEBUG
        Harmony.DEBUG = true;
#endif

        var harmony = new Harmony("DotNetCompat");
        // harmony.PatchAll(Assembly.GetExecutingAssembly());

        var assembly = Assembly.GetExecutingAssembly();
        var typesFromAssembly = AccessTools.GetTypesFromAssembly(assembly).ToList();
        typesFromAssembly.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        typesFromAssembly.Do(type =>
        {
#if DEBUG
            Console.WriteLine($"Patching: {type.Name}");
#endif
            harmony.CreateClassProcessor(type).Patch();
        });
    }
}