using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ClientPlugin.Tools;
using HarmonyLib;
using VRage.Audio;

namespace ClientPlugin.Patches.Audio;

[HarmonyPatch(typeof(MyXAudio2))]
// ReSharper disable once UnusedType.Global
public static class MyXAudio2Patch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyTranspiler]
    [HarmonyPatch("CreateX3DAudio")]
    private static IEnumerable<CodeInstruction> CreateX3DAudioTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod)
    {
        var il = instructions.ToList();
        il.RecordOriginalCode(patchedMethod);

        // Change X3DAudioVersion.Version29 to X3DAudioVersion.Default
        // Replace ldc.i4.3 (X3DAudioVersion.Version29) with ldc.i4.0
        var index = il.FindIndex(ci => ci.opcode == OpCodes.Ldc_I4_3);
        if (index == -1)
            throw new CodeInstructionNotFound("Failed to find ldc.i4.3 (X3DAudioVersion.Version29) in the IL code of method CreateX3DAudio");

        il[index] = new CodeInstruction(OpCodes.Ldc_I4_0);

        il.RecordPatchedCode(patchedMethod);
        return il;
    }
}