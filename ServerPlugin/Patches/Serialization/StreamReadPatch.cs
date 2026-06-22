using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ServerPlugin.Tools;
using HarmonyLib;
using Sandbox.Engine.Voxels;
using SharpDX.Multimedia;
using SharpDX.Toolkit.Graphics;
using VRage;
using VRage.Audio;
using VRage.Library.Compression;
using VRage.Render11.Shader;
using VRageRender.Import;

namespace ServerPlugin.Patches.Serialization;

[HarmonyPatch]
[HarmonyPatchCategory("Finish")]
// ReSharper disable once UnusedType.Global
static class StreamReadPatch
{
    private static readonly MethodInfo OriginalReadMethod = AccessTools.Method(typeof(Stream), nameof(Stream.Read), [typeof(byte[]), typeof(int), typeof(int)]);
    private static readonly MethodInfo ReplacementReadMethod = AccessTools.Method(typeof(StreamReadPatch), nameof(ReplacementRead));

    // ReSharper disable once UnusedMember.Global
    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var at9WaveFormatType = typeof(MySoundStream).GetNestedType("At9WaveFormat", BindingFlags.Public | BindingFlags.NonPublic)!;
        var readChunkMethod = AccessTools.Method(at9WaveFormatType, "ReadChunk");

        var fmtChunkType = at9WaveFormatType.GetNestedType("FmtChunk", BindingFlags.NonPublic)!;
        var factChunkType = at9WaveFormatType.GetNestedType("FactChunk", BindingFlags.NonPublic)!;

        var methods = new List<MethodBase>
        {
            readChunkMethod.MakeGenericMethod(fmtChunkType),
            readChunkMethod.MakeGenericMethod(factChunkType),

            AccessTools.Method(typeof(DDSHelper), "TryReadDDSHeader"),

            AccessTools.Method(typeof(MyModelImporter), "ImportData"),

            AccessTools.Method(typeof(SoundStream), nameof(SoundStream.ToDataStream)),

            AccessTools.Method(typeof(StreamExtensions), nameof(StreamExtensions.CheckGZipHeader)),
            AccessTools.Method(typeof(StreamExtensions), nameof(StreamExtensions.ReadNoAlloc)),
            AccessTools.Method(typeof(StreamExtensions), nameof(StreamExtensions.ReadString)),
            AccessTools.Method(typeof(StreamExtensions), nameof(StreamExtensions.SkipBytes)),

            AccessTools.Method(typeof(MyCompression), nameof(MyCompression.Compress)),
            AccessTools.Method(typeof(MyCompression), nameof(MyCompression.Decompress)),
            AccessTools.Method(typeof(MyCompression), "DecompressFile"),

            AccessTools.Constructor(typeof(MyCompressionFileLoad), [typeof(string)]),
            AccessTools.Method(typeof(MyCompressionFileLoad), nameof(MyCompressionFileLoad.GetInt32)),
            AccessTools.Method(typeof(MyCompressionFileLoad), nameof(MyCompressionFileLoad.GetCompressedBuffer)),

            AccessTools.Constructor(typeof(MyCompressionStreamLoad), [typeof(byte[])]),
            AccessTools.Method(typeof(MyCompressionStreamLoad), nameof(MyCompressionStreamLoad.GetInt32)),

            GetMyStorageBasePerformLoadMethod(),

            AccessTools.Method(typeof(MyStorageBaseCompatibility), "Compatibility_LoadCellVoxelMaterial"),

            AccessTools.Method(typeof(MyShaderCache), nameof(MyShaderCache.GetCacheContent)),
        };

        foreach (var m in methods)
        {
            Console.WriteLine($"StreamReadPatch.TargetMethods: {m.FullDescription()}");
        }

        return methods;
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

    // ReSharper disable once UnusedMember.Global
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> StreamReadTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod)
    {
        Console.WriteLine($"StreamReadPatch.StreamReadTranspiler: {patchedMethod.FullDescription()}");

        var codeInstructions = instructions as CodeInstruction[] ?? instructions.ToArray();
        var il = codeInstructions.ToList();
        il.RecordOriginalCode(patchedMethod);

        var count = 0;
        for (var i = 0; i < codeInstructions.Length; i++)
        {
            if (il[i].Calls(OriginalReadMethod))
            {
                il[i] = new CodeInstruction(OpCodes.Call, ReplacementReadMethod);
                count++;
            }
        }

        if (count == 0)
            throw new Exception($"Could not find read calls in method: {patchedMethod.FullDescription()}");

        Console.WriteLine($"Patched {count} Stream.Read calls in method: {patchedMethod.FullDescription()}");

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
}
