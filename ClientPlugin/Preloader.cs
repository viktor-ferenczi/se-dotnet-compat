// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Reflection;
using HarmonyLib;
using System.Collections.Generic;
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
        "Sandbox.Game.dll",
        "VRage.dll",
        "VRage.Game.dll",
        "VRage.Render.dll",
        "VRage.Render11.dll",
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
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}