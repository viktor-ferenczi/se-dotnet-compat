using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using HarmonyLib;
using Sandbox;
using System.Reflection.Emit;
using ClientPlugin.Tools;

namespace ClientPlugin.Patches.CrashReporting;

[HarmonyPatch(typeof(MyInitializer))]
// ReSharper disable once UnusedType.Global
public static class MyInitializerPatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyTranspiler]
    [HarmonyPatch("InitExceptionHandling")]
    private static IEnumerable<CodeInstruction> InitExceptionHandlingTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod, ILGenerator ilGenerator)
    {
        var il = instructions.ToList();
        il.RecordOriginalCode(patchedMethod);
        il.VerifyCodeHash(patchedMethod, "0561eef4");

        var setNameIndex = il.FindIndex(i => i.opcode == OpCodes.Callvirt && (i.operand?.ToString() ?? "").Contains("set_Name"));
        Debug.Assert(setNameIndex != -1, "Could not find set_Name");

        var start1 = il.FindIndex(setNameIndex + 1, i => i.opcode == OpCodes.Ldloc_0);
        Debug.Assert(start1 != -1, "Could not find Ldloc_0 after set_Name");

        var end1 = il.FindIndex(i => i.opcode == OpCodes.Callvirt && (i.operand?.ToString() ?? "").Contains("SetNativeExceptionHandler"));
        Debug.Assert(end1 != -1, "Could not find SetNativeExceptionHandler");

        var cleanupIndex = il.FindIndex(i => i.opcode == OpCodes.Callvirt && (i.operand?.ToString() ?? "").Contains("CleanupCrashAnalytics"));
        var start2 = cleanupIndex - 1;
        Debug.Assert(il[start2].opcode == OpCodes.Call, "Could not find: call static VRage.IMyCrashReporting Sandbox.MyInitializer::get_ErrorPlatform()");

        var end2 = il.FindIndex(i => i.opcode == OpCodes.Call && (i.operand?.ToString() ?? "").Contains("UpdateHangAnalytics"));
        Debug.Assert(end2 != -1, "Could not find UpdateHangAnalytics");

        var label1 = ilGenerator.DefineLabel();
        il[end1 + 1].labels.Add(label1);

        var label2 = ilGenerator.DefineLabel();
        il[end2 + 1].labels.Add(label2);

        il.Insert(start2, new CodeInstruction(OpCodes.Br, label2));
        il.Insert(start1, new CodeInstruction(OpCodes.Br, label1));

        il.RecordPatchedCode(patchedMethod);
        return il;
    }
}