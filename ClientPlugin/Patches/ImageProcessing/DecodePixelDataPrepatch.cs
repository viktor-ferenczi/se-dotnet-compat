using System.Linq;
using ClientPlugin.Tools;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ClientPlugin.Patches.ImageProcessing;

// Fixes the partial-read short-circuit in both PNG scanline decoders inside
// SixLabors.ImageSharp.Formats.Png.PngDecoderCore. The original DecodePixelData<T>
// fix is by @SpaceGT.
//
// Both DecodePixelData<T> (non-interlaced) and DecodeInterlacedPixelData<T> read
// one scanline at a time via `compressedStream.Read(...)` and break/return when
// the read returns fewer bytes than requested. That worked on .NET Framework
// where DeflateStream filled the requested buffer; on .NET 6+ (and noticeably
// on .NET 10) DeflateStream can return partial reads even when more decoded
// data is available, which makes both methods early-exit and leaves the image
// silently truncated — or, when the surrounding chunk reader resumes mid-IDAT,
// surfaces as `Bad method for ZLIB header: cmf=<n>`. The Penumbra planet's
// interlaced 16-bit grayscale heightmaps trip the interlaced variant.
//
// Verified by HeightmapRepro: rewriting the first `ret` of each method into a
// back-branch to the read setup makes the loop retry until the scanline is
// fully filled, matching the original Framework behaviour.
public static class DecodePixelDataPrepatch
{
    public static void Prepatch(AssemblyDefinition asmDef)
    {
        if (asmDef.Name.Name != "SixLabors.ImageSharp")
            return;

        var decoderType = asmDef.MainModule.Types.First(t =>
            t.FullName == "SixLabors.ImageSharp.Formats.Png.PngDecoderCore"
        );

        PatchDecodePixelData(decoderType);
        PatchDecodeInterlacedPixelData(decoderType);
    }

    // Non-interlaced scanline decoder. il[0] is the unconditional `br` jumping
    // to the loop condition (C# while-loop layout), so branching to il[1] from
    // the rewritten Ret lands on the start of the loop body with an empty
    // stack — which is what we want.
    private static void PatchDecodePixelData(TypeDefinition decoderType)
    {
        var method = decoderType.Methods.First(m =>
            m.Name == "DecodePixelData" && m.HasGenericParameters
        );

        var il = method.Body.Instructions;
        il.RecordOriginalCode(method);
        il.VerifyCodeHash(method, "8e787d98");

        var target = il[1];

        foreach (var instr in il)
            if (instr.OpCode == OpCodes.Ret)
            {
                instr.OpCode = OpCodes.Br;
                instr.Operand = target;
                break;
            }

        il.RecordPatchedCode(method);
    }

    // Interlaced (Adam7) scanline decoder. il[0]=`ldarg.0` pushes `this` for
    // the subsequent ldflda — branching to il[1] from an empty stack would be
    // invalid IL. The first Ldarg_1 (`compressedStream`) marks the start of
    // the inner Read setup and is the correct back-branch target.
    private static void PatchDecodeInterlacedPixelData(TypeDefinition decoderType)
    {
        var method = decoderType.Methods.First(m =>
            m.Name == "DecodeInterlacedPixelData" && m.HasGenericParameters
        );

        var il = method.Body.Instructions;
        il.RecordOriginalCode(method);

        Instruction target = null;
        foreach (var instr in il)
            if (instr.OpCode == OpCodes.Ldarg_1)
            {
                target = instr;
                break;
            }
        if (target == null)
            throw new System.InvalidOperationException(
                "DecodePixelDataPrepatch: no Ldarg_1 in DecodeInterlacedPixelData");

        var rewritten = false;
        foreach (var instr in il)
            if (instr.OpCode == OpCodes.Ret)
            {
                instr.OpCode = OpCodes.Br;
                instr.Operand = target;
                rewritten = true;
                break;
            }
        if (!rewritten)
            throw new System.InvalidOperationException(
                "DecodePixelDataPrepatch: no Ret in DecodeInterlacedPixelData");

        il.RecordPatchedCode(method);
    }
}
