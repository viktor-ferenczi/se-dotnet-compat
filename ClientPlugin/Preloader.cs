// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Reflection;
using System.Collections.Generic;
using ClientPlugin.Patches.ImageProcessing;
using ClientPlugin.Patches.NullSafety;
using ClientPlugin.Patches.Serialization;
using HarmonyLib;
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
        // See https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide
        AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);

        // Fixes runtime loading the Keen version in some cases by initializing it explicitly
        Assembly.Load("System.Collections.Immutable");

        // JIT-prewarm the Directory.Enumerate* call chain on the main thread.
        //
        // MonoMod's V60 JIT hook (used by Harmony on .NET 6 layout) SEGVs on
        // .NET 10 when CoreCLR re-enters compileMethod from a worker thread for
        // the LibraryImportGenerator-emitted P/Invoke stub of the runtime's
        // thread-static helpers (StaticsHelpers.<GetThreadStaticsByIndex>g____PInvoke).
        // That stub is JIT'd on first use of Directory.EnumerateFiles /
        // SharedArrayPool<char>.Rent. In a normal Pulsar startup the first use
        // happens on a ParallelTasks worker inside MyXAudio2.Preload, which is
        // exactly the racy context that trips the hook.
        //
        // Compiling the stub here, on the main thread, before any Harmony
        // patching or parallel preload runs, removes the race entirely. The
        // IL stub is process-wide JIT'd code (only the per-thread storage it
        // accesses is per-thread), so subsequent first-touches from worker
        // threads call the already-compiled stub and never re-enter
        // compileMethod. See Docs/Fixes.md (2026-05-01 entry).
        PrewarmDirectoryEnumerationStubs();
        
        // Override game DLLs with the versions added as NuGet dependency by this plugin
        string[] dlls = [
            "System.Management",
        ];
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var targetName = new AssemblyName(args.Name).Name;
            return dlls.Contains(targetName) ? Assembly.Load(targetName) : null;
        };
        
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

            // Iterating one entry is enough to JIT the FileSystemEnumerator<T>
            // generic specialization, SharedArrayPool<char>.Rent, the lazy
            // thread-static initializer for the array pool, and the P/Invoke
            // stub the initializer reaches via StaticsHelpers — i.e. the full
            // chain shown in the crashing core dump.
            using (var e = System.IO.Directory.EnumerateFiles(baseDir).GetEnumerator())
                e.MoveNext();
            using (var e = System.IO.Directory.EnumerateDirectories(baseDir).GetEnumerator())
                e.MoveNext();

            Console.WriteLine("[DotNetCompat] Pre-warmed Directory.Enumerate* JIT stubs on main thread");
        }
        catch (Exception ex)
        {
            // Pre-warm is purely preventative; a failure here is not fatal.
            // Worst case the original race window is back.
            Console.WriteLine($"[DotNetCompat] Pre-warm of Directory.Enumerate* failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}