using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using HarmonyLib;
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

        if (mode == "trace") modes = ["trace"];
        if (mode == "fixed") modes = ["fixed"];
        if (mode == "deflate") modes = ["deflate"];
        if (mode == "patch-dll") modes = ["patch-dll"];

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
                case "trace":
                    RunTrace(path);
                    break;
                case "fixed":
                    RunFixed(path);
                    break;
                case "deflate":
                    RunDeflateProbe(path);
                    break;
                case "patch-dll":
                    RunPatchDll();
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

    // Patch-dll mode: in-place patch of bin\Debug\...\SixLabors.ImageSharp.dll
    // to fix the partial-read early-return in PngDecoderCore.DecodeInterlacedPixelData.
    //
    // Important: this method MUST NOT reference any ImageSharp type so the
    // runtime doesn't load the DLL before we patch it (loaded modules can't be
    // overwritten on Windows). Mono.Cecil reads the DLL via a FileStream copy
    // and writes back to a temp file then atomically moves it.
    //
    // Workflow:
    //   1. dotnet build
    //   2. dotnet HeightmapRepro --mode patch-dll      (no MSBuild — direct exe)
    //   3. dotnet HeightmapRepro --mode load-only      (now succeeds)
    private static void RunPatchDll()
    {
        var binDir = AppContext.BaseDirectory;
        var dllPath = Path.Combine(binDir, "SixLabors.ImageSharp.dll");
        if (!File.Exists(dllPath))
        {
            Console.Error.WriteLine($"DLL not found: {dllPath}");
            throw new FileNotFoundException(dllPath);
        }
        Console.WriteLine($"  patching: {dllPath}");

        // Read entire DLL into memory so we don't hold a lock on the file.
        var dllBytes = File.ReadAllBytes(dllPath);

        var resolver = new Mono.Cecil.DefaultAssemblyResolver();
        resolver.AddSearchDirectory(binDir);
        var asm = Mono.Cecil.AssemblyDefinition.ReadAssembly(
            new MemoryStream(dllBytes),
            new Mono.Cecil.ReaderParameters { AssemblyResolver = resolver, ReadWrite = false });

        var decoderType = asm.MainModule.Types.First(t =>
            t.FullName == "SixLabors.ImageSharp.Formats.Png.PngDecoderCore");

        // Patch both DecodePixelData (non-interlaced) AND DecodeInterlacedPixelData.
        // The existing ClientPlugin prepatch covers the first; the interlaced
        // version has the same bug and is what Penumbra heightmaps hit.
        //
        // The fix: convert the early-bail `ret` (partial-read short-circuit)
        // into a back-branch to the start of the scanline-Read sequence —
        // identified by the first `ldarg.1` (load `compressedStream`) in the
        // method body. Re-reading from there appends the next chunk of bytes
        // to `currentRowBytesRead` until the full scanline is read.
        //
        // Branching to instructions[1] (as the existing ClientPlugin patch does
        // for DecodePixelData) works there because il[0] is `br <loophead>`
        // (stack-neutral); it does NOT work for DecodeInterlacedPixelData
        // because il[0]=`ldarg.0` pushes `this` that il[1]=`ldflda` consumes,
        // so branching to il[1] from empty stack produces invalid IL.
        foreach (var name in new[] { "DecodePixelData", "DecodeInterlacedPixelData" })
        {
            var method = decoderType.Methods.First(m => m.Name == name && m.HasGenericParameters);
            var il = method.Body.Instructions;

            // Target = first `ldarg.1` (compressedStream) — start of the Read setup.
            Mono.Cecil.Cil.Instruction target = null;
            foreach (var ins in il)
            {
                if (ins.OpCode == Mono.Cecil.Cil.OpCodes.Ldarg_1)
                {
                    target = ins;
                    break;
                }
            }
            if (target == null)
                throw new InvalidOperationException($"no Ldarg_1 found in {name}");

            var found = false;
            foreach (var instr in il)
            {
                if (instr.OpCode == Mono.Cecil.Cil.OpCodes.Ret)
                {
                    instr.OpCode = Mono.Cecil.Cil.OpCodes.Br;
                    instr.Operand = target;
                    found = true;
                    break;
                }
            }
            if (!found)
                throw new InvalidOperationException($"no Ret instruction found in {name}");
            Console.WriteLine($"  {name}: rewrote first Ret -> Br to {target.OpCode} @IL_{target.Offset:x4}");
        }

        // Write to a temp file then atomic-replace the original.
        var tmpPath = dllPath + ".patched";
        asm.Write(tmpPath);
        asm.Dispose();
        File.Copy(tmpPath, dllPath, overwrite: true);
        File.Delete(tmpPath);
        Console.WriteLine("  DLL patched in place.");
    }

    // Deflate-probe mode: builds the same view that ZlibInflateStream presents to
    // System.IO.Compression.DeflateStream — a stream that exposes ONLY the
    // concatenated IDAT chunk data (skipping the chunk-header/CRC framing) — then
    // reads from DeflateStream using the same .Read(buffer,offset,count) pattern
    // that PngDecoderCore.DecodeInterlacedPixelData uses. Logs every Read return
    // count, so we can prove the bug: on .NET 10 the DeflateStream returns
    // partial reads even when more compressed data is available, which trips the
    // early-return in DecodeInterlacedPixelData.
    private static void RunDeflateProbe(string path)
    {
        var bytes = File.ReadAllBytes(path);
        // Walk PNG chunks and collect the IDAT data byte ranges.
        int pos = 8; // skip PNG signature
        var idatRanges = new List<(int Offset, int Length)>();
        while (pos < bytes.Length - 12)
        {
            int len = (bytes[pos] << 24) | (bytes[pos + 1] << 16) | (bytes[pos + 2] << 8) | bytes[pos + 3];
            string type = System.Text.Encoding.ASCII.GetString(bytes, pos + 4, 4);
            if (type == "IDAT") idatRanges.Add((pos + 8, len));
            if (type == "IEND") break;
            pos = pos + 8 + len + 4;
        }
        Console.WriteLine($"  found {idatRanges.Count} IDAT chunks; total compressed bytes={idatRanges.Sum(r => r.Length):N0}");
        // First IDAT byte is CMF, second is FLG. Past those is the deflate stream.
        var firstIdat = idatRanges[0];
        Console.WriteLine($"  first IDAT@{firstIdat.Offset}: CMF=0x{bytes[firstIdat.Offset]:x2} FLG=0x{bytes[firstIdat.Offset + 1]:x2}");

        // Build a single concatenated MemoryStream of just the deflate payload
        // (CMF/FLG skipped, all IDATs joined). This is what DeflateStream would
        // see if the chunk framing didn't exist.
        using var deflatePayload = new MemoryStream();
        for (int i = 0; i < idatRanges.Count; i++)
        {
            var r = idatRanges[i];
            int start = (i == 0) ? r.Offset + 2 : r.Offset; // skip CMF/FLG in first IDAT
            int length = (i == 0) ? r.Length - 2 : r.Length;
            deflatePayload.Write(bytes, start, length);
        }
        deflatePayload.Position = 0L;

        using var deflate = new DeflateStream(deflatePayload, CompressionMode.Decompress, leaveOpen: false);

        // Replay the read pattern used by DecodeInterlacedPixelData:
        // For Adam7-interlaced 1024x1024 16bpp Gray, scanlines vary by pass, but
        // the typical read size is in the order of 2KB. Use 8192 to match a
        // generous chunk and observe partial returns.
        const int readSize = 8192;
        var buf = new byte[readSize];
        int totalDecompressed = 0;
        int partialCount = 0;
        int totalReads = 0;
        for (int iter = 0; iter < 1000; iter++)
        {
            int n = deflate.Read(buf, 0, readSize);
            if (n == 0) break;
            totalReads++;
            totalDecompressed += n;
            if (n < readSize) partialCount++;
            if (iter < 20)
            {
                Console.WriteLine($"  iter {iter}: Read({readSize}) -> {n}{(n < readSize ? "  <-- PARTIAL" : "")}");
            }
        }
        Console.WriteLine($"  totals: reads={totalReads} bytes={totalDecompressed:N0} partial_returns={partialCount}");
        if (partialCount > 0)
        {
            Console.WriteLine("  >>> DeflateStream returns partial reads. This is the root cause: <<<");
            Console.WriteLine("  >>> PngDecoderCore.DecodeInterlacedPixelData early-returns on partial reads, <<<");
            Console.WriteLine("  >>> leaving the outer Decode loop to re-parse junk as chunk headers.       <<<");
        }
    }

    // Fixed mode: install a Harmony postfix on ZlibInflateStream.InitializeInflateStream
    // that wraps the internal DeflateStream in a "read fully" stream — so that
    // PngDecoderCore.DecodePixelData / DecodeInterlacedPixelData never see a
    // partial read and don't bail out early. This proves the root cause.
    private static bool harmonyInstalled;
    private static void RunFixed(string path)
    {
        InstallHarmonyFix();
        using var stream = File.OpenRead(path);
        using var img = Image.Load<Gray16>(stream);
        Console.WriteLine($"  loaded:     {img.Width}x{img.Height}, frames={img.Frames.Count}");
    }

    private static void InstallHarmonyFix()
    {
        if (harmonyInstalled) return;
        harmonyInstalled = true;

        var asm = typeof(Image).Assembly;
        var zlibType = asm.GetType("SixLabors.ImageSharp.Formats.Png.Zlib.ZlibInflateStream", throwOnError: true);
        var initMethod = zlibType.GetMethod("InitializeInflateStream", BindingFlags.Instance | BindingFlags.NonPublic);
        if (initMethod == null) throw new InvalidOperationException("InitializeInflateStream not found");

        var harmony = new Harmony("heightmap-repro");
        var postfix = typeof(Program).GetMethod(nameof(InitializeInflateStreamPostfix), BindingFlags.Static | BindingFlags.NonPublic);
        harmony.Patch(initMethod, postfix: new HarmonyMethod(postfix));
        Console.WriteLine($"  harmony patch installed on {zlibType.FullName}.InitializeInflateStream");
    }

    private static FieldInfo compressedStreamField;
    private static void InitializeInflateStreamPostfix(object __instance)
    {
        if (compressedStreamField == null)
        {
            compressedStreamField = __instance.GetType().GetField("compressedStream", BindingFlags.Instance | BindingFlags.NonPublic);
            if (compressedStreamField == null) throw new InvalidOperationException("compressedStream field not found");
        }
        var inner = (DeflateStream)compressedStreamField.GetValue(__instance);
        if (inner == null) return;
        if (inner is ReadFullyDeflateWrapper) return;
        compressedStreamField.SetValue(__instance, new ReadFullyDeflateWrapper(inner));
    }

    // Wraps a DeflateStream and retries Read until either the requested count
    // is filled or the underlying stream reports EOS. Caller-visible API is
    // identical to DeflateStream — but every Read returns the full count
    // whenever data is still available.
    //
    // Inherits from DeflateStream so the private field type check in
    // ZlibInflateStream / PngDecoderCore still passes.
    private sealed class ReadFullyDeflateWrapper : DeflateStream
    {
        private readonly DeflateStream inner;

        public ReadFullyDeflateWrapper(DeflateStream inner)
            // Pass a no-op underlying stream; we delegate everything to `inner`.
            : base(Stream.Null, CompressionMode.Decompress, leaveOpen: true)
        {
            this.inner = inner;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int n = inner.Read(buffer, offset + totalRead, count - totalRead);
                if (n <= 0) break;
                totalRead += n;
            }
            return totalRead;
        }

        public override int Read(Span<byte> buffer)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int n = inner.Read(buffer.Slice(totalRead));
                if (n <= 0) break;
                totalRead += n;
            }
            return totalRead;
        }

        public override int ReadByte() => inner.ReadByte();
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }
    }

    // Trace mode: wrap a MemoryStream in a TracingStream that logs every Read,
    // ReadByte, Seek, Position assignment so we can see exactly where the bug
    // happens — what offset CMF is read from and whether earlier reads
    // short-returned (which would cascade-shift the position).
    private static void RunTrace(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var inner = new MemoryStream(bytes, writable: false);
        using var stream = new TracingStream(inner, bytes);
        try
        {
            using var img = Image.Load<Gray16>(stream);
            Console.WriteLine($"  loaded:     {img.Width}x{img.Height}, frames={img.Frames.Count}");
        }
        finally
        {
            stream.DumpSummary();
        }
    }

    // Wraps an inner stream and logs every read/seek/position call so we can
    // see exactly what offset each byte is read from. Also records the actual
    // bytes returned vs. what is at the requested offset in the source.
    private sealed class TracingStream : Stream
    {
        private readonly Stream inner;
        private readonly byte[] truth;
        private readonly List<string> log = new();
        private int reads;
        private int totalBytesRead;

        public TracingStream(Stream inner, byte[] truth)
        {
            this.inner = inner;
            this.truth = truth;
        }

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set
            {
                Log($"Position={value}");
                inner.Position = value;
            }
        }

        public override void Flush() => inner.Flush();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
        {
            var before = inner.Position;
            var result = inner.Seek(offset, origin);
            Log($"Seek({offset},{origin}) {before} -> {result}");
            return result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var posBefore = inner.Position;
            var n = inner.Read(buffer, offset, count);
            reads++;
            totalBytesRead += n;
            string preview = "";
            int previewLen = Math.Min(n, 16);
            if (previewLen > 0)
            {
                var sb = new System.Text.StringBuilder();
                for (var i = 0; i < previewLen; i++) sb.Append(buffer[offset + i].ToString("x2") + " ");
                preview = " bytes=" + sb.ToString().TrimEnd();
            }
            // Highlight short reads
            string flag = (n < count) ? " *SHORT*" : "";
            Log($"Read(@{posBefore},count={count}) -> {n}{flag}{preview}");
            return n;
        }

        public override int Read(Span<byte> buffer)
        {
            var posBefore = inner.Position;
            var n = inner.Read(buffer);
            reads++;
            totalBytesRead += n;
            string preview = "";
            int previewLen = Math.Min(n, 16);
            if (previewLen > 0)
            {
                var sb = new System.Text.StringBuilder();
                for (var i = 0; i < previewLen; i++) sb.Append(buffer[i].ToString("x2") + " ");
                preview = " bytes=" + sb.ToString().TrimEnd();
            }
            string flag = (n < buffer.Length) ? " *SHORT*" : "";
            Log($"Read(Span @{posBefore},count={buffer.Length}) -> {n}{flag}{preview}");
            return n;
        }

        public override int ReadByte()
        {
            var posBefore = inner.Position;
            var b = inner.ReadByte();
            reads++;
            if (b >= 0) totalBytesRead++;
            Log($"ReadByte(@{posBefore}) -> {(b >= 0 ? b.ToString("x2") : "EOF")}");
            return b;
        }

        private void Log(string msg)
        {
            log.Add(msg);
        }

        public void DumpSummary()
        {
            Console.WriteLine($"  trace: {reads} read calls, {totalBytesRead} bytes total");
            // Dump first 80 events and last 30 events
            var first = Math.Min(80, log.Count);
            Console.WriteLine($"  first {first} events:");
            for (var i = 0; i < first; i++) Console.WriteLine("    " + log[i]);
            if (log.Count > first + 30)
            {
                Console.WriteLine("    ...");
                for (var i = log.Count - 30; i < log.Count; i++) Console.WriteLine("    " + log[i]);
            }
            else if (log.Count > first)
            {
                for (var i = first; i < log.Count; i++) Console.WriteLine("    " + log[i]);
            }
        }
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
