using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ClientPlugin.Tools;
using HarmonyLib;
using VRage.Platform.Windows.Audio;

namespace ClientPlugin.Patches.Audio;

[HarmonyPatch(typeof(MyPlatformAudio))]
// ReSharper disable once UnusedType.Global
public static class MyPlatformAudioPatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyTranspiler]
    [HarmonyPatch("InitAudioEngine")]
    private static IEnumerable<CodeInstruction> InitAudioEngineTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod)
    {
        var il = instructions.ToList();
        il.RecordOriginalCode(patchedMethod);
        il.VerifyCodeHash(patchedMethod, "3bbf9165");

        // Change XAudio2Version.Version29 to XAudio2Version.Default
        // Replace ldc.i4.3 with ldc.i4.0
        var index = il.FindIndex(ci => ci.opcode == OpCodes.Ldc_I4_3);
        if (index == -1)
            throw new CodeInstructionNotFound("Failed to find ldc.i4.3 in the IL code of method InitAudioEngine");

        il[index] = new CodeInstruction(OpCodes.Ldc_I4_0);

        il.RecordPatchedCode(patchedMethod);
        return il;
    }
}