#if PROTOBUF_FIXES

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Pulsar.Shared.Utils;
using HarmonyLib;
using VRage.Platform.Windows.Serialization;

namespace ClientPlugin.Patches.ProtoBuf;

[HarmonyPatch(typeof(DynamicTypeModel))]
// ReSharper disable once UnusedType.Global
public static class DynamicTypeModelPatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyTranspiler]
    [HarmonyPatch("CreateTypeModel")]
    private static IEnumerable<CodeInstruction> CreateTypeModelTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod)
    {
        var il = instructions.ToList();
        il.RecordOriginalCode(patchedMethod);

        // var i = il.FindIndex(i => i.opcode == OpCodes.Call && i.operand is MethodBase m && m.Name.EndsWith("Create"));
        // Debug.Assert(il.Count >= 0, "Could not find the call to RuntimeTypeModel.Create");
        // il.RemoveAt(i - 1);

        il.RecordPatchedCode(patchedMethod);
        return il;
    }
}
#endif
