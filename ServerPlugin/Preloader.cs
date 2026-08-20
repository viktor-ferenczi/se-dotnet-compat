using System;
using System.Reflection;
using System.Collections.Generic;
using ServerPlugin.Patches.Windows;
using HarmonyLib;
using Mono.Cecil;
using Shared.Patches.ImageProcessing;
using Shared.Patches.NullSafety;
using Shared.Patches.Serialization;

// Magnetar requires Preloader in the global namespace.

public static class Preloader
{
    // Magnetar stages and resolves these NuGet dependencies.
    private static readonly HashSet<string> OverriddenAssemblies = new(StringComparer.Ordinal)
    {
        "System.Management",
        "System.Drawing.Common",
        "System.Diagnostics.PerformanceCounter",
    };

    // Assembly.Load can re-enter AssemblyResolve, so track names per thread.
    [System.ThreadStatic]
    private static HashSet<string> _resolvingTls;
    private static HashSet<string> Resolving =>
        _resolvingTls ??= new HashSet<string>(StringComparer.Ordinal);

    private static Assembly ResolveOverriddenAssembly(object sender, ResolveEventArgs args)
    {
        var targetName = new AssemblyName(args.Name).Name;
        if (!OverriddenAssemblies.Contains(targetName))
            return null;

        if (!Resolving.Add(targetName))
        {
            Console.Error.WriteLine(
                $"[DotNetCompat] AssemblyResolve recursion for '{targetName}'. " +
                "The runtime cannot locate this assembly by name; Magnetar must " +
                "stage it in a probe path (e.g. plugin Bin folder). Returning null " +
                "to abort the resolve chain.");
            return null;
        }
        try
        {
            return Assembly.Load(targetName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[DotNetCompat] Failed to load '{targetName}': {ex.GetType().Name}: {ex.Message}");
            return null;
        }
        finally
        {
            Resolving.Remove(targetName);
        }
    }

    public static IEnumerable<string> TargetDLLs { get; } =
    [
        // Game DLLs
        "HavokWrapper.dll",
        "Sandbox.Common.dll",
        "Sandbox.Game.dll",
        "Sandbox.Graphics.dll",
        "SpaceEngineers.Game.dll",
        "VRage.dll",
        "VRage.Game.dll",
        "VRage.Library.dll",
        "VRage.Math.dll",
        "VRage.Network.dll",
        "VRage.Platform.Windows.dll",
        "VRage.Render11.dll",
        "VRage.Scripting.dll",

        // Server DLLs
        "VRage.Dedicated.dll",
        "SpaceEngineersDedicated.exe",

        // Dependency DLLs
        "SixLabors.ImageSharp.dll",
    ];

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static void Patch(AssemblyDefinition asmDef)
    {
        AppContext.SetSwitch("System.Reflection.AssemblyLoadContext.EnableDiagnostics", true);

        DecodePixelDataPrepatch.Prepatch(asmDef);
        MyHeightMapLoadingSystemPrepatch.Prepatch(asmDef);
        XmlSerializationPrepatch.Prepatch(asmDef);
        WindowsServicePrepatch.Prepatch(asmDef);
        MyProgramPrepatch.Prepatch(asmDef);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static void Finish()
    {
        // See https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide
        AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);

        // Load this before the game can bind to Keen's copy.
        Assembly.Load("System.Collections.Immutable");

        AppDomain.CurrentDomain.AssemblyResolve += ResolveOverriddenAssembly;

#if DEBUG && HARMONY_DEBUG
        Harmony.DEBUG = true;
#endif

        var harmony = new Harmony("DotNetCompat");
        harmony.PatchCategory("Finish");
    }
}
