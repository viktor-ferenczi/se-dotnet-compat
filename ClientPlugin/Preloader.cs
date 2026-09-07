using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil;
using Shared.Patches.ImageProcessing;
using Shared.Patches.NullSafety;
using Shared.Patches.Serialization;

// Pulsar requires Preloader in the global namespace.

public static class Preloader
{
    // Pulsar stages these NuGet dependencies and loads them by name.
    private static readonly HashSet<string> OverriddenAssemblies = new(StringComparer.Ordinal)
    {
        "System.Management",
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
                $"[DotNetCompat] AssemblyResolve recursion for '{targetName}'. "
                    + "The runtime cannot locate this assembly by name; Pulsar must "
                    + "stage it in a probe path (e.g. plugin Bin folder). Returning null "
                    + "to abort the resolve chain."
            );
            return null;
        }
        try
        {
            return Assembly.Load(targetName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[DotNetCompat] Failed to load '{targetName}': {ex.GetType().Name}: {ex.Message}"
            );
            return null;
        }
        finally
        {
            Resolving.Remove(targetName);
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.NoInlining
    )]
    public static void Initialize()
    {
        // Must run before any plugin's Finish hook applies the first Harmony
        // patch, which installs MonoMod's JIT hook.
        Shared.Tools.ThreadStaticsPrewarm.Run();
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
        "SixLabors.ImageSharp.dll",
    ];

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.NoInlining
    )]
    public static void Patch(AssemblyDefinition asmDef)
    {
        AppContext.SetSwitch("System.Reflection.AssemblyLoadContext.EnableDiagnostics", true);

        DecodePixelDataPrepatch.Prepatch(asmDef);
        MyHeightMapLoadingSystemPrepatch.Prepatch(asmDef);
        XmlSerializationPrepatch.Prepatch(asmDef);
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.NoInlining
    )]
    public static void Finish()
    {
        // See https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide
        AppContext.SetSwitch(
            "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization",
            true
        );

        // Load this before the game can bind to Keen's copy.
        Assembly.Load("System.Collections.Immutable");

        // .NET Framework resolved every installed Windows codepage through
        // Encoding.GetEncoding; on modern .NET the legacy codepages live in an
        // opt-in provider. Register it before any mod or game code asks for
        // e.g. codepage 1252. This has to happen here rather than in Plugin.Init:
        // a dedicated server loads its world, mods included, before Init runs.
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // JIT these stubs on the main thread before Harmony starts worker threads.
        PrewarmDirectoryEnumerationStubs();

        AppDomain.CurrentDomain.AssemblyResolve += ResolveOverriddenAssembly;

#if DEBUG && HARMONY_DEBUG
        Harmony.DEBUG = true;
#endif
        var harmony = new Harmony("DotNetCompat");
        harmony.PatchCategory("Finish");
    }

    private static void PrewarmDirectoryEnumerationStubs()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(baseDir) || !System.IO.Directory.Exists(baseDir))
                return;

            using (var e = System.IO.Directory.EnumerateFiles(baseDir).GetEnumerator())
                e.MoveNext();
            using (var e = System.IO.Directory.EnumerateDirectories(baseDir).GetEnumerator())
                e.MoveNext();

            Console.WriteLine(
                "[DotNetCompat] Pre-warmed Directory.Enumerate* JIT stubs on main thread"
            );
        }
        catch (Exception ex)
        {
            // Failure is harmless, but leaves the JIT race unfixed.
            Console.WriteLine(
                $"[DotNetCompat] Pre-warm of Directory.Enumerate* failed: {ex.GetType().Name}: {ex.Message}"
            );
        }
    }
}
