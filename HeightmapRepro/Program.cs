using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HeightmapRepro;

// Standalone reproducer for the "Bad method for ZLIB header: cmf=<n>" crash that
// blocks heightmap loading on .NET 10. Mirrors what
// Sandbox.Engine.Voxels.Planet.MyPlanetTextureMapProvider.GetHeightMap eventually
// does via VRage.Render.MyImage.Load(path, oneChannel: true):
//
//   1. Open a FileStream on the .png.
//   2. Image.Identify(stream)  // 1st pass — reads PNG chunks for metadata
//   3. stream.Position = 0
//   4. Image.Load<Gray16>(stream)  // 2nd pass — full decode
//
// Step 4 is what throws on Penumbra's heightmaps when running on .NET 10.
//
// Modes:
//   --mode game        Identify -> rewind -> Load<Gray16>  (default; matches game)
//   --mode load-only   Skip Identify, just Load<Gray16> from a fresh FileStream
//   --mode memory      Read all bytes, then Load<Gray16> from MemoryStream
//                      (control case — should always succeed; isolates the bug
//                      to the FileStream partial-read behavior)
//   --mode all         Run all three modes against the same path, report each
//
// Iterate by editing this file and running the CLI; no plugin / Pulsar in the
// loop. Exit codes: 0 = success, 1 = the heightmap failed to decode in the
// configured mode, 2 = argument / file error.
internal static class Program
{
    private const string DefaultPath =
        @"C:\Program Files (x86)\Steam\steamapps\workshop\content\244850\3077440417\Data\PlanetDataFiles\Penumbra\back.png";

    private static int Main(string[] args)
    {
        var path = DefaultPath;
        var mode = "game";
        var verbose = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--mode" when i + 1 < args.Length:
                    mode = args[++i];
                    break;
                case "-v":
                case "--verbose":
                    verbose = true;
                    break;
                case "-h":
                case "--help":
                    PrintUsage();
                    return 0;
                default:
                    if (args[i].StartsWith("--"))
                    {
                        Console.Error.WriteLine($"unknown option: {args[i]}");
                        PrintUsage();
                        return 2;
                    }
                    path = args[i];
                    break;
            }
        }

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"file not found: {path}");
            return 2;
        }

        Console.WriteLine($"runtime: .NET {Environment.Version}");
        Console.WriteLine($"file:    {path}");
        Console.WriteLine($"size:    {new FileInfo(path).Length:N0} bytes");
        Console.WriteLine($"mode:    {mode}");
        Console.WriteLine();

        var modes = mode == "all"
            ? new[] { "game", "load-only", "memory" }
            : [mode];

        var anyFailed = false;
        foreach (var m in modes)
        {
            var ok = RunMode(m, path, verbose);
            if (!ok) anyFailed = true;
        }

        return anyFailed ? 1 : 0;
    }

    private static bool RunMode(string mode, string path, bool verbose)
    {
        Console.WriteLine($"--- mode: {mode} ---");
        var sw = Stopwatch.StartNew();
        try
        {
            switch (mode)
            {
                case "game":
                    RunGameMode(path);
                    break;
                case "load-only":
                    RunLoadOnly(path);
                    break;
                case "memory":
                    RunMemory(path);
                    break;
                default:
                    Console.Error.WriteLine($"unknown mode: {mode}");
                    return false;
            }

            sw.Stop();
            Console.WriteLine($"OK ({sw.ElapsedMilliseconds} ms)");
            Console.WriteLine();
            return true;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($"FAILED ({sw.ElapsedMilliseconds} ms): {ex.GetType().FullName}: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine();
                Console.WriteLine("inner exception chain:");
                var e = ex;
                var depth = 0;
                while (e != null)
                {
                    Console.WriteLine($"  [{depth}] {e.GetType().FullName}: {e.Message}");
                    e = e.InnerException;
                    depth++;
                }
                Console.WriteLine();
                Console.WriteLine("stack trace:");
                Console.WriteLine(ex.StackTrace);
            }
            Console.WriteLine();
            return false;
        }
    }

    // Mirrors VRage.Render.MyImage.Load(path, oneChannel: true) for a 16-bit
    // grayscale heightmap PNG: open FileStream, Identify, rewind, Load.
    private static void RunGameMode(string path)
    {
        using var stream = File.OpenRead(path);

        var info = Image.Identify(stream);
        if (info == null)
            throw new InvalidDataException("Image.Identify returned null");
        Console.WriteLine($"  identified: {info.Width}x{info.Height}, {info.PixelType.BitsPerPixel}bpp");

        stream.Position = 0L;

        using var img = Image.Load<Gray16>(stream);
        Console.WriteLine($"  loaded:     {img.Width}x{img.Height}, frames={img.Frames.Count}");
    }

    // Skip the Identify pass — just decode from a fresh FileStream. Lets us see
    // whether Identify-then-Load (which leaves the stream in a different cache
    // state) is the trigger vs. raw Load.
    private static void RunLoadOnly(string path)
    {
        using var stream = File.OpenRead(path);
        using var img = Image.Load<Gray16>(stream);
        Console.WriteLine($"  loaded:     {img.Width}x{img.Height}, frames={img.Frames.Count}");
    }

    // Control case: copy the whole file into a MemoryStream first. MemoryStream
    // never short-reads, so this should always succeed even if the FileStream
    // path is broken. If THIS fails, the bug is in ImageSharp itself, not in
    // the FileStream <-> ImageSharp interaction.
    private static void RunMemory(string path)
    {
        var bytes = File.ReadAllBytes(path);
        using var stream = new MemoryStream(bytes, writable: false);
        using var img = Image.Load<Gray16>(stream);
        Console.WriteLine($"  loaded:     {img.Width}x{img.Height}, frames={img.Frames.Count}");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: HeightmapRepro [options] [path-to-png]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --mode {game|load-only|memory|all}   reproduction mode (default: game)");
        Console.WriteLine("  -v, --verbose                        print exception chain + stack trace");
        Console.WriteLine("  -h, --help                           show this help");
        Console.WriteLine();
        Console.WriteLine("If no path is given, defaults to the Penumbra mod's back.png.");
    }
}
