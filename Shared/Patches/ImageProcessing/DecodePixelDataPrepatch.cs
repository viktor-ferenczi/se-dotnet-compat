using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Shared.Tools;

namespace Shared.Patches.ImageProcessing;

// Retry partial DeflateStream reads until each PNG scanline is complete.
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

    // il[1] starts the loop body with an empty stack.
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

    // The first Ldarg_1 starts the inner Read with a valid stack.
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
                "DecodePixelDataPrepatch: no Ldarg_1 in DecodeInterlacedPixelData"
            );

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
                "DecodePixelDataPrepatch: no Ret in DecodeInterlacedPixelData"
            );

        il.RecordPatchedCode(method);
    }
}
