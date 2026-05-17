using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ClientPlugin.Tools;
using HarmonyLib;
using Sandbox.Engine.Voxels;
using SharpDX.Multimedia;
using SharpDX.Toolkit.Graphics;
using VRage;
using VRage.Audio;
using VRage.Library.Compression;
using VRage.Render11.Shader;
using VRageRender.Import;

namespace ClientPlugin.Patches.Serialization;

// NOTE: DecodePixelDataPrepatch fixes the same partial-read issue inside
// PngDecoderCore.DecodePixelData<T> AND DecodeInterlacedPixelData<T>. Those
// have to remain preloader (Cecil) patches because the splash image is loaded
// before this Harmony category runs. The targets below cover the FileStream-
// level short reads that surround the decoder (chunk framing and the
// ZlibInflateStream's inner reads).

[HarmonyPatch]
[HarmonyPatchCategory("Finish")]
// ReSharper disable once UnusedType.Global
static class StreamReadPatch
{
    private static readonly MethodInfo OriginalReadMethod = AccessTools.Method(typeof(Stream), nameof(Stream.Read), [typeof(byte[]), typeof(int), typeof(int)]);
    private static readonly MethodInfo ReplacementReadMethod = AccessTools.Method(typeof(StreamReadPatch), nameof(ReplacementRead));
    private static readonly MethodInfo ReplacementReadDrainMethod = AccessTools.Method(typeof(StreamReadPatch), nameof(ReplacementReadDrain));

    // Methods whose Stream.Read sites should use the drain variant (loop until
    // either count bytes are read OR the stream is exhausted, returning the
    // actual count) instead of the default exact variant (throw on short).
    // Use the drain variant for "give me up to N bytes" semantics; use the
    // default for fixed-size reads where short == corruption.
    private static readonly HashSet<MethodBase> DrainTargets = [];

    // ReSharper disable once UnusedMember.Global
    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        // Resolve the nested types we need from MySoundStream.At9WaveFormat.
        var at9WaveFormatType = typeof(MySoundStream).GetNestedType("At9WaveFormat", BindingFlags.Public | BindingFlags.NonPublic);
        if (at9WaveFormatType == null)
            throw new InvalidOperationException("StreamReadPatch: could not find nested type MySoundStream.At9WaveFormat (VRage.Audio). The game may have renamed or removed it.");

        var readChunkMethod = AccessTools.Method(at9WaveFormatType, "ReadChunk");
        if (readChunkMethod == null)
            throw new InvalidOperationException("StreamReadPatch: could not find MySoundStream.At9WaveFormat.ReadChunk<T> (VRage.Audio).");

        var fmtChunkType = at9WaveFormatType.GetNestedType("FmtChunk", BindingFlags.NonPublic);
        if (fmtChunkType == null)
            throw new InvalidOperationException("StreamReadPatch: could not find nested struct MySoundStream.At9WaveFormat.FmtChunk (VRage.Audio).");

        var factChunkType = at9WaveFormatType.GetNestedType("FactChunk", BindingFlags.NonPublic);
        if (factChunkType == null)
            throw new InvalidOperationException("StreamReadPatch: could not find nested struct MySoundStream.At9WaveFormat.FactChunk (VRage.Audio).");

        // SixLabors.ImageSharp.Formats.Png.PngDecoderCore is internal; resolve by name.
        var pngDecoderCoreType = AccessTools.TypeByName("SixLabors.ImageSharp.Formats.Png.PngDecoderCore");
        if (pngDecoderCoreType == null)
            throw new InvalidOperationException(
                "StreamReadPatch: could not find SixLabors.ImageSharp.Formats.Png.PngDecoderCore. " +
                "The SixLabors.ImageSharp assembly must be loaded before Preloader.Finish runs " +
                "(or the type has been renamed in a newer ImageSharp version).");

        // SixLabors.ImageSharp.Formats.Png.Zlib.ZlibInflateStream is internal; resolve by name.
        // Its instance Read(byte[], int, int) decrements currentDataRemaining by the *requested*
        // count before calling innerStream.Read, then never reconciles with the actual return
        // value. If innerStream short-reads, the method also short-returns and the IDAT chunk
        // tracker desyncs. Hardening innerStream.Read here keeps that whole path consistent.
        var zlibInflateStreamType = AccessTools.TypeByName("SixLabors.ImageSharp.Formats.Png.Zlib.ZlibInflateStream");
        if (zlibInflateStreamType == null)
            throw new InvalidOperationException(
                "StreamReadPatch: could not find SixLabors.ImageSharp.Formats.Png.Zlib.ZlibInflateStream.");
        var zlibInflateRead = AccessTools.Method(
            zlibInflateStreamType, "Read", [typeof(byte[]), typeof(int), typeof(int)]);

        // (label, MethodBase) pairs so we can name anything that fails to resolve.
        var targets = new List<(string Label, MethodBase Method)>
        {
            // MySoundStream.At9WaveFormat.ReadChunk<T> - audio chunk reading (VRage.Audio)
            ("MySoundStream.At9WaveFormat.ReadChunk<FmtChunk>", readChunkMethod.MakeGenericMethod(fmtChunkType)),
            ("MySoundStream.At9WaveFormat.ReadChunk<FactChunk>", readChunkMethod.MakeGenericMethod(factChunkType)),

            // DDSHelper - texture loading
            // FREEZES THE GAME: AccessTools.Method(typeof(DDSHelper), "CreateCompressedImageFromStream"),
            ("DDSHelper.TryReadDDSHeader", AccessTools.Method(typeof(DDSHelper), "TryReadDDSHeader")),

            // MyModelImporter - model loading
            ("MyModelImporter.ImportData", AccessTools.Method(typeof(MyModelImporter), "ImportData")),

            // SoundStream (SharpDX) - audio loading
            ("SharpDX.Multimedia.SoundStream.ToDataStream", AccessTools.Method(typeof(SoundStream), nameof(SoundStream.ToDataStream))),

            // StreamExtensions - core stream reading utilities (VRage.Library)
            ("StreamExtensions.CheckGZipHeader", AccessTools.Method(typeof(StreamExtensions), nameof(StreamExtensions.CheckGZipHeader))),
            ("StreamExtensions.ReadNoAlloc", AccessTools.Method(typeof(StreamExtensions), nameof(StreamExtensions.ReadNoAlloc))),
            ("StreamExtensions.ReadString", AccessTools.Method(typeof(StreamExtensions), nameof(StreamExtensions.ReadString))),
            ("StreamExtensions.SkipBytes", AccessTools.Method(typeof(StreamExtensions), nameof(StreamExtensions.SkipBytes))),

            // MyCompression - compression/decompression utilities (VRage.Library)
            ("MyCompression.Compress", AccessTools.Method(typeof(MyCompression), nameof(MyCompression.Compress))),
            ("MyCompression.Decompress", AccessTools.Method(typeof(MyCompression), nameof(MyCompression.Decompress))),
            ("MyCompression.DecompressFile", AccessTools.Method(typeof(MyCompression), "DecompressFile")),

            // MyCompressionFileLoad - file-based compression loading (VRage.Library)
            ("MyCompressionFileLoad..ctor(string)", AccessTools.Constructor(typeof(MyCompressionFileLoad), [typeof(string)])),
            ("MyCompressionFileLoad.GetInt32", AccessTools.Method(typeof(MyCompressionFileLoad), nameof(MyCompressionFileLoad.GetInt32))),
            ("MyCompressionFileLoad.GetCompressedBuffer", AccessTools.Method(typeof(MyCompressionFileLoad), nameof(MyCompressionFileLoad.GetCompressedBuffer))),

            // MyCompressionStreamLoad - stream-based compression loading (VRage.Library)
            ("MyCompressionStreamLoad..ctor(byte[])", AccessTools.Constructor(typeof(MyCompressionStreamLoad), [typeof(byte[])])),
            ("MyCompressionStreamLoad.GetInt32", AccessTools.Method(typeof(MyCompressionStreamLoad), nameof(MyCompressionStreamLoad.GetInt32))),
            // GetBytes' inner Read is a single BufferedStream.Read that's allowed to
            // short-return; callers (e.g. BigGustave-based PNG inflate in the
            // "Ore Detector Reforged" mod) assume one call drains the requested
            // amount, then truncate their buffer to the returned count. Use the
            // drain variant so we loop until either the request is satisfied or
            // the underlying stream is exhausted, and return the actual count.
            ("MyCompressionStreamLoad.GetBytes", AccessTools.Method(typeof(MyCompressionStreamLoad), nameof(MyCompressionStreamLoad.GetBytes))),

            // MyStorageBase - voxel storage loading (Sandbox.Game)
            // Note: The actual Stream.Read call is in a compiler-generated local function,
            // so we target the closure class method instead of LoadFromFile itself
            ("MyStorageBase.LoadFromFile.PerformLoad (closure)", GetMyStorageBasePerformLoadMethod()),

            // MyStorageBaseCompatibility - legacy voxel format loading (Sandbox.Game)
            ("MyStorageBaseCompatibility.Compatibility_LoadCellVoxelMaterial", AccessTools.Method(typeof(MyStorageBaseCompatibility), "Compatibility_LoadCellVoxelMaterial")),

            // MyShaderCache - shader cache loading (VRage.Render11)
            ("MyShaderCache.GetCacheContent", AccessTools.Method(typeof(MyShaderCache), nameof(MyShaderCache.GetCacheContent))),

            // SixLabors.ImageSharp.Formats.Png.PngDecoderCore - PNG chunk framing on the
            // FileStream. TryReadChunkLength / ReadChunkType / ReadChunkCrc do 4-byte
            // reads; ReadChunkData reads the chunk payload (zTXt / iCCP can be several KB);
            // ReadNextDataChunk reads the 4-byte CRC + the next length+type between
            // consecutive IDATs. ReadChunkData and ReadNextDataChunk don't validate the
            // returned count at all, so a short read on the FileStream shifts the position
            // by 1-3 bytes silently. The decoder's payload-level partial-read bug is
            // handled by DecodePixelDataPrepatch (covers both DecodePixelData and
            // DecodeInterlacedPixelData) — these targets harden the framing layer below it.
            ("PngDecoderCore.TryReadChunkLength", AccessTools.Method(pngDecoderCoreType, "TryReadChunkLength")),
            ("PngDecoderCore.ReadChunkType", AccessTools.Method(pngDecoderCoreType, "ReadChunkType")),
            ("PngDecoderCore.ReadChunkCrc", AccessTools.Method(pngDecoderCoreType, "ReadChunkCrc")),
            ("PngDecoderCore.ReadChunkData", AccessTools.Method(pngDecoderCoreType, "ReadChunkData")),
            ("PngDecoderCore.ReadNextDataChunk", AccessTools.Method(pngDecoderCoreType, "ReadNextDataChunk")),

            // ZlibInflateStream.Read - inner-loop partial-read desync (see resolve site above).
            ("ZlibInflateStream.Read", zlibInflateRead),
        };

        // Every entry must resolve; a null means the game/library changed shape and the
        // patch is silently broken. Fail loudly with the labels of everything that's missing.
        var unresolved = targets.Where(t => t.Method == null).Select(t => t.Label).ToList();
        if (unresolved.Count > 0)
            throw new InvalidOperationException(
                "StreamReadPatch: failed to resolve the following target method(s): " +
                string.Join(", ", unresolved) +
                ". A game update or dependency upgrade likely changed their name or signature.");

        // Mark drain-semantic targets (bounded-not-exact reads).
        var getBytes = AccessTools.Method(typeof(MyCompressionStreamLoad), nameof(MyCompressionStreamLoad.GetBytes));
        DrainTargets.Add(getBytes);

        return targets.Select(t => t.Method);
    }

    private static MethodBase GetMyStorageBasePerformLoadMethod()
    {
        // Find the compiler-generated closure class for LoadFromFile
        // The class name includes a number that may change between game versions
        var closureType = typeof(MyStorageBase)
            .GetNestedTypes(BindingFlags.NonPublic)
            .FirstOrDefault(t => t.Name.StartsWith("<>c__DisplayClass") &&
                                 t.GetMethod("<LoadFromFile>g__PerformLoad|0", BindingFlags.Instance | BindingFlags.NonPublic) != null);

        if (closureType == null)
            throw new Exception("Could not find MyStorageBase.LoadFromFile closure class");

        var method = closureType.GetMethod("<LoadFromFile>g__PerformLoad|0", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new Exception($"Could not find PerformLoad method in {closureType.FullName}");

        return method;
    }

    // ReSharper disable once UnusedMember.Global
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> StreamReadTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod)
    {
        var codeInstructions = instructions as CodeInstruction[] ?? instructions.ToArray();
        var il = codeInstructions.ToList();
        il.RecordOriginalCode(patchedMethod);

        var replacement = DrainTargets.Contains(patchedMethod) ? ReplacementReadDrainMethod : ReplacementReadMethod;

        var count = 0;
        for (var i = 0; i < codeInstructions.Length; i++)
        {
            if (il[i].Calls(OriginalReadMethod))
            {
                il[i] = new CodeInstruction(OpCodes.Call, replacement);
                count++;
            }
        }

        if (count == 0)
            throw new Exception($"Could not find read calls in method: {patchedMethod.FullDescription()}");

        il.RecordPatchedCode(patchedMethod);
        return il;
    }

    // Retrying the Read call until it reads the bytes requested
    // (game code relies on implementation detail of .NET 4.8)
    private static int ReplacementRead(Stream stream, byte[] array, int offset, int count)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var bytesRead = stream.Read(array, offset + totalRead, count - totalRead);
            if (bytesRead == 0)
                throw new EndOfStreamException();

            totalRead += bytesRead;
        }

        return totalRead;
    }

    // Like ReplacementRead but tolerates premature EOF: loops until either
    // count bytes are read or the underlying stream is exhausted, then returns
    // the actual byte count. Used for bounded-not-exact reads (callers pass a
    // max size and rely on the returned length to know how much really came
    // back), where throwing on EOF would change semantics.
    private static int ReplacementReadDrain(Stream stream, byte[] array, int offset, int count)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var bytesRead = stream.Read(array, offset + totalRead, count - totalRead);
            if (bytesRead == 0)
                break;

            totalRead += bytesRead;
        }

        return totalRead;
    }
}