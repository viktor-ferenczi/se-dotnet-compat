#if PROTOBUF_FIXES

using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ClientPlugin.Patches.ProtoBuf;

public static class MyObjectBuilderSerializerKeenPrepatch
{
    public static void Prepatch(AssemblyDefinition asmDef)
    {
        if (asmDef.Name.Name != "VRage.Game")
            return;

        // Disable ProtoBuf cloning, because it caused crashes
        var type = asmDef.MainModule.GetType("VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen");
        var staticConstructor = type.Methods.First(m => m.Name == ".cctor");
        Debug.Assert(staticConstructor != null, "Could not find static constructor");
        var body = staticConstructor.Body;
        var il = body.Instructions;
        Debug.Assert(il[0].OpCode == OpCodes.Ldc_I4_1, "Could not find Ldc_I4_1");
        Debug.Assert(il[1].OpCode == OpCodes.Stsfld && il[1].Operand is FieldDefinition fd && fd.Name == "ENABLE_PROTOBUFFERS_CLONING", "Could not find the initialization of ENABLE_PROTOBUFFERS_CLONING field");
        il[0].OpCode = OpCodes.Ldc_I4_0;
    }
}
#endif