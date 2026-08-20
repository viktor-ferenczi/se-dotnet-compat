using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Sandbox.Engine.Voxels;
using SharpDX.Multimedia;
using SharpDX.Toolkit.Graphics;
using VRage;
using VRage.Audio;
using VRage.Library.Compression;
using VRage.Render11.Shader;
using VRageRender.Import;
using Shared.Tools;

namespace ClientPlugin.Patches.Serialization;

// The game expects Stream.Read to fill the requested buffer, as it usually did on .NET Framework.

[HarmonyPatch]
[HarmonyPatchCategory("Finish")]
static class StreamReadPatch
{
    private static readonly MethodInfo OriginalReadMethod = AccessTools.Method(typeof(Stream), nameof(Stream.Read), [typeof(byte[]), typeof(int), typeof(int)]);
    private static readonly MethodInfo ReplacementReadMethod = AccessTools.Method(typeof(StreamReadPatch), nameof(ReplacementRead));
    private static readonly MethodInfo ReplacementReadDrainMethod = AccessTools.Method(typeof(StreamReadPatch), nameof(ReplacementReadDrain));

    // These methods ask for at most N bytes, so EOF is not an error for them.
    private static readonly HashSet<MethodBase> DrainTargets = [];

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
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

        var pngDecoderCoreType = AccessTools.TypeByName("SixLabors.ImageSharp.Formats.Png.PngDecoderCore");
        if (pngDecoderCoreType == null)
            throw new InvalidOperationException(
                "StreamReadPatch: could not find SixLabors.ImageSharp.Formats.Png.PngDecoderCore. " +
                "The SixLabors.ImageSharp assembly must be loaded before Preloader.Finish runs " +
                "(or the type has been renamed in a newer ImageSharp version).");

        // ZlibInflateStream subtracts the requested count, not the count actually read.
        var zlibInflateStreamType = AccessTools.TypeByName("SixLabors.ImageSharp.Formats.Png.Zlib.ZlibInflateStream");
        if (zlibInflateStreamType == null)
            throw new InvalidOperationException(
                "StreamReadPatch: could not find SixLabors.ImageSharp.Formats.Png.Zlib.ZlibInflateStream.");
        var zlibInflateRead = AccessTools.Method(
            zlibInflateStreamType, "Read", [typeof(byte[]), typeof(int), typeof(int)]);

        var targets = new List<(string Label, MethodBase Method)>
        {
            ("MySoundStream.At9WaveFormat.ReadChunk<FmtChunk>", readChunkMethod.MakeGenericMethod(fmtChunkType)),
            ("MySoundStream.At9WaveFormat.ReadChunk<FactChunk>", readChunkMethod.MakeGenericMethod(factChunkType)),

            ("DDSHelper.TryReadDDSHeader", AccessTools.Method(typeof(DDSHelper), "TryReadDDSHeader")),

            ("MyModelImporter.ImportData", AccessTools.Method(typeof(MyModelImporter), "ImportData")),

            ("SharpDX.Multimedia.SoundStream.ToDataStream", AccessTools.Method(typeof(SoundStream), nameof(SoundStream.ToDataStream))),

            ("StreamExtensions.CheckGZipHeader", AccessTools.Method(typeof(StreamExtensions), nameof(StreamExtensions.CheckGZipHeader))),
            ("StreamExtensions.ReadNoAlloc", AccessTools.Method(typeof(StreamExtensions), nameof(StreamExtensions.ReadNoAlloc))),
            ("StreamExtensions.ReadString", AccessTools.Method(typeof(StreamExtensions), nameof(StreamExtensions.ReadString))),
            ("StreamExtensions.SkipBytes", AccessTools.Method(typeof(StreamExtensions), nameof(StreamExtensions.SkipBytes))),

            ("MyCompression.Compress", AccessTools.Method(typeof(MyCompression), nameof(MyCompression.Compress))),
            ("MyCompression.Decompress", AccessTools.Method(typeof(MyCompression), nameof(MyCompression.Decompress))),
            ("MyCompression.DecompressFile", AccessTools.Method(typeof(MyCompression), "DecompressFile")),

            ("MyCompressionFileLoad..ctor(string)", AccessTools.Constructor(typeof(MyCompressionFileLoad), [typeof(string)])),
            ("MyCompressionFileLoad.GetInt32", AccessTools.Method(typeof(MyCompressionFileLoad), nameof(MyCompressionFileLoad.GetInt32))),
            ("MyCompressionFileLoad.GetCompressedBuffer", AccessTools.Method(typeof(MyCompressionFileLoad), nameof(MyCompressionFileLoad.GetCompressedBuffer))),

            ("MyCompressionStreamLoad..ctor(byte[])", AccessTools.Constructor(typeof(MyCompressionStreamLoad), [typeof(byte[])])),
            ("MyCompressionStreamLoad.GetInt32", AccessTools.Method(typeof(MyCompressionStreamLoad), nameof(MyCompressionStreamLoad.GetInt32))),
            ("MyCompressionStreamLoad.GetBytes", AccessTools.Method(typeof(MyCompressionStreamLoad), nameof(MyCompressionStreamLoad.GetBytes))),

            ("MyStorageBase.LoadFromFile.PerformLoad (closure)", GetMyStorageBasePerformLoadMethod()),

            ("MyStorageBaseCompatibility.Compatibility_LoadCellVoxelMaterial", AccessTools.Method(typeof(MyStorageBaseCompatibility), "Compatibility_LoadCellVoxelMaterial")),

            ("MyShaderCache.GetCacheContent", AccessTools.Method(typeof(MyShaderCache), nameof(MyShaderCache.GetCacheContent))),

            ("PngDecoderCore.TryReadChunkLength", AccessTools.Method(pngDecoderCoreType, "TryReadChunkLength")),
            ("PngDecoderCore.ReadChunkType", AccessTools.Method(pngDecoderCoreType, "ReadChunkType")),
            ("PngDecoderCore.ReadChunkCrc", AccessTools.Method(pngDecoderCoreType, "ReadChunkCrc")),
            ("PngDecoderCore.ReadChunkData", AccessTools.Method(pngDecoderCoreType, "ReadChunkData")),
            ("PngDecoderCore.ReadNextDataChunk", AccessTools.Method(pngDecoderCoreType, "ReadNextDataChunk")),

            ("ZlibInflateStream.Read", zlibInflateRead),
        };

        var unresolved = targets.Where(t => t.Method == null).Select(t => t.Label).ToList();
        if (unresolved.Count > 0)
            throw new InvalidOperationException(
                "StreamReadPatch: failed to resolve the following target method(s): " +
                string.Join(", ", unresolved) +
                ". A game update or dependency upgrade likely changed their name or signature.");

        var getBytes = AccessTools.Method(typeof(MyCompressionStreamLoad), nameof(MyCompressionStreamLoad.GetBytes));
        DrainTargets.Add(getBytes);

        return targets.Select(t => t.Method);
    }

    private static MethodBase GetMyStorageBasePerformLoadMethod()
    {
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
