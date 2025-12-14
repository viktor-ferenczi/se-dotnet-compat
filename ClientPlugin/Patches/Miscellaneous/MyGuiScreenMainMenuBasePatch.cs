using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using ClientPlugin.Tools;
using HarmonyLib;
using Sandbox.Game.Screens;

namespace ClientPlugin.Patches.Miscellaneous;

[HarmonyPatch(typeof(MyGuiScreenMainMenuBase))]
// ReSharper disable once UnusedType.Global
public static class MyGuiScreenMainMenuBasePatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyTranspiler]
    [HarmonyPatch("DrawAppVersion")]
    private static IEnumerable<CodeInstruction> DrawAppVersionTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod)
    {
        var il = instructions.ToList();
        il.RecordOriginalCode(patchedMethod);

        // Find the index of the String.Concat call that constructs the version text
        var concatIndex = il.FindIndex(ci => ci.opcode == OpCodes.Call &&
                                             ci.operand is MethodInfo mi &&
                                             mi.Name == "Concat" &&
                                             mi.DeclaringType == typeof(string) &&
                                             mi.GetParameters().Length == 2);

        if (concatIndex == -1)
            throw new Exception("Failed to find String.Concat call in the IL code of method DrawAppVersion");

        // Insert before stloc.2: Call the AppendFrameworkDescription method
        il.Insert(concatIndex + 1, new CodeInstruction(OpCodes.Call, typeof(MyGuiScreenMainMenuBasePatch).GetMethod(nameof(AppendFrameworkDescription), BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)));

        il.RecordPatchedCode(patchedMethod);
        return il;
    }

    public static string AppendFrameworkDescription(string text)
    {
        return $"{text} on {RuntimeInformation.FrameworkDescription}";
    }
}