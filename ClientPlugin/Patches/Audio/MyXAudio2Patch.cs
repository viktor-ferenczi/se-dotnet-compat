using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Shared.Tools;
using VRage.Audio;

namespace ClientPlugin.Patches.Audio;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyXAudio2))]
public static class MyXAudio2Patch
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(MyXAudio2.CreateX3DAudio))]
    private static IEnumerable<CodeInstruction> CreateX3DAudioTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod)
    {
        var il = instructions.ToList();
        il.RecordOriginalCode(patchedMethod);
        il.VerifyCodeHash(patchedMethod, "c3f3a592");

        // Use X3DAudioVersion.Default instead of Version29.
        var index = il.FindIndex(ci => ci.opcode == OpCodes.Ldc_I4_3);
        if (index == -1)
            throw new CodeInstructionNotFound("Failed to find ldc.i4.3 (X3DAudioVersion.Version29) in the IL code of method CreateX3DAudio");

        il[index] = new CodeInstruction(OpCodes.Ldc_I4_0);

        il.RecordPatchedCode(patchedMethod);
        return il;
    }
}
