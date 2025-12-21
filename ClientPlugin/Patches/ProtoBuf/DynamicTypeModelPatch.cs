#if PROTOBUF_FIXES

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Pulsar.Shared.Utils;
using HarmonyLib;
using ProtoBuf.Meta;
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

        Debug.Assert(il[1].opcode == OpCodes.Ldc_I4_1, "Could not find Ldc_I4_1");
        Debug.Assert(il[2].opcode == OpCodes.Call && il[2].operand is MethodBase mb && mb.Name == "Create", "Could not find the call to Create");

        var createMethod = AccessTools.DeclaredMethod(typeof(RuntimeTypeModel), nameof(RuntimeTypeModel.Create), [typeof(string)]);
        Debug.Assert(createMethod != null, $"Could not find the method: RuntimeTypeModel.Create()");
        il[1].opcode = OpCodes.Ldnull;
        il[2].operand = createMethod;

        il.RecordPatchedCode(patchedMethod);
        return il;
    }
}
#endif
