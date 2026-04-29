// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Loader;
using ServerPlugin.Patches.ImageProcessing;
using ServerPlugin.Patches.NullSafety;
using ServerPlugin.Patches.Serialization;
using ServerPlugin.Patches.Windows;
using HarmonyLib;
using Mono.Cecil;

// IMPORTANT: MUST NOT USE A NAMESPACE, otherwise the loader won't find the Preloader class!
//namespace ServerPlugin;

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

    // ReSharper disable once UnusedMember.Global
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

    // ReSharper disable once UnusedMember.Global
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static void Finish()
    {
        // See https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide
        AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);

        // Fixes runtime loading the Keen version in some cases by initializing it explicitly
        Assembly.Load("System.Collections.Immutable");

        // Pre-load NuGet dependency DLLs from the Bin directory
        var assemblyDir = Path.GetDirectoryName(typeof(Preloader).Assembly.Location)!;
        var dataRoot = Path.GetDirectoryName(assemblyDir)!;
        var binDirCandidates = new List<string>
        {
            Path.Combine(assemblyDir, "Bin"),
        };
        var githubDir = Path.Combine(dataRoot, "GitHub");
        if (Directory.Exists(githubDir))
        {
            foreach (var pluginDir in Directory.GetDirectories(githubDir, "*", SearchOption.AllDirectories))
            {
                var binDir = Path.Combine(pluginDir, "Bin");
                if (Directory.Exists(binDir))
                    binDirCandidates.Add(binDir);
            }
        }
        string[] dlls = [
            "System.Management",
            "System.Drawing.Common",
            "System.Diagnostics.PerformanceCounter",
        ];
        foreach (var dll in dlls)
        {
            foreach (var binDir in binDirCandidates)
            {
                var dllPath = Path.GetFullPath(Path.Combine(binDir, dll + ".dll"));
                if (!File.Exists(dllPath))
                    continue;
                AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
                break;
            }
        }

#if DEBUG && HARMONY_DEBUG
        Harmony.DEBUG = true;
#endif

        var harmony = new Harmony("DotNetCompat");
        harmony.PatchCategory("Finish");
    }
}
