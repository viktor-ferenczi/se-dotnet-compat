#if PROTOBUF_FIXES

// Static constructors cannot be patched this way.
// If this is required, then write a prepatch instead.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pulsar.Shared.Utils;
using HarmonyLib;
using VRage.ObjectBuilders.Private;

namespace ClientPlugin.Patches.ProtoBuf;

[HarmonyPatch(typeof(MyObjectBuilderSerializerKeen))]
public static class MyObjectBuilderSerializerKeenPatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyTranspiler]
    [HarmonyPatch(MethodType.StaticConstructor)]
    private static IEnumerable<CodeInstruction> StaticConstructorTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod)
    {
        var il = instructions.ToList();
        il.RecordOriginalCode(patchedMethod);

        // TODO: Implement transpiler to disable protobuf cloning:
        // Wrap ENABLE_PROTOBUFFERS_CLONING = true; in #if THIS_CAUSED_CRASHES
        // This crashed inside protobuf-net code inside calls to MyObjectBuilderSerializerKeen.Clone

        il.RecordPatchedCode(patchedMethod);
        return il;
    }
}
#endif
