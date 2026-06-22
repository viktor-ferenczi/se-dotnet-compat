// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Reflection;
using System.Collections.Generic;
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
    // Assembly names this plugin overrides via AssemblyResolve. Loaded from the
    // plugin's NuGet dependencies (staged by Magnetar into NuGet/bin/<hash>/ and
    // served by Magnetar's AssemblyResolver hook on AppDomain.AssemblyResolve).
    private static readonly HashSet<string> OverriddenAssemblies = new(StringComparer.Ordinal)
    {
        "System.Management",
        "System.Drawing.Common",
        "System.Diagnostics.PerformanceCounter",
    };

    // Tracks assembly names currently being resolved on this thread to break
    // AssemblyResolve recursion. See ResolveOverriddenAssembly for details.
    [System.ThreadStatic]
    private static HashSet<string> _resolvingTls;
    private static HashSet<string> Resolving =>
        _resolvingTls ??= new HashSet<string>(StringComparer.Ordinal);

    private static Assembly ResolveOverriddenAssembly(object sender, ResolveEventArgs args)
    {
        var targetName = new AssemblyName(args.Name).Name;
        if (!OverriddenAssemblies.Contains(targetName))
            return null;

        // Re-entry guard: Assembly.Load(name) fires AssemblyResolve again if the
        // runtime can't bind the name to a TPA/probe path. Without a guard the
        // handler recurses until the stack overflows (exit code 0xC00000FD) with
        // no useful diagnostic. If we re-enter for the same name, bail with a
        // clear error instead.
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

        // Override game DLLs with the versions added as NuGet dependency by this plugin.
        // Magnetar's AssemblyResolver serves these from NuGet/bin/<hash>/ via the same
        // AppDomain.AssemblyResolve event when ResolveOverriddenAssembly delegates to
        // Assembly.Load(name) and the runtime probe fails.
        AppDomain.CurrentDomain.AssemblyResolve += ResolveOverriddenAssembly;

#if DEBUG && HARMONY_DEBUG
        Harmony.DEBUG = true;
#endif

        var harmony = new Harmony("DotNetCompat");
        harmony.PatchCategory("Finish");
    }
}
