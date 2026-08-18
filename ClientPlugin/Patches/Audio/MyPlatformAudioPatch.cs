using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Shared.Tools;
using VRage.Platform.Windows.Audio;

namespace ClientPlugin.Patches.Audio;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyPlatformAudio))]
public static class MyPlatformAudioPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch("InitAudioEngine")]
    private static IEnumerable<CodeInstruction> InitAudioEngineTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod)
    {
        var il = instructions.ToList();
        il.RecordOriginalCode(patchedMethod);
        il.VerifyCodeHash(patchedMethod, "3bbf9165");

        // Use XAudio2Version.Default instead of Version29.
        var index = il.FindIndex(ci => ci.opcode == OpCodes.Ldc_I4_3);
        if (index == -1)
            throw new CodeInstructionNotFound("Failed to find ldc.i4.3 in the IL code of method InitAudioEngine");

        il[index] = new CodeInstruction(OpCodes.Ldc_I4_0);

        il.RecordPatchedCode(patchedMethod);
        return il;
    }
}
