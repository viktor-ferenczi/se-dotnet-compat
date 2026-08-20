using System.Diagnostics;
using System.Linq;
using Shared.Tools;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Shared.Patches.Serialization;

public static class XmlSerializationPrepatch
{
    private const string XSI_NS_URL = "http://www.w3.org/2001/XMLSchema-instance";

    public static void Prepatch(AssemblyDefinition asmDef)
    {
        if (asmDef.Name.Name != "VRage")
            return;

        var module = asmDef.MainModule;

        PrepatchCustomRootWriter(module);
        PrepatchMyAbstractXmlSerializer(module);
    }

    private static void PrepatchCustomRootWriter(ModuleDefinition module)
    {
        var type = module.GetType("VRage.CustomRootWriter");
        var method = type.Methods.First(m => m.Name == "Init");
        Debug.Assert(method != null, "Could not find the Init method");
        var body = method.Body;
        var il = body.Instructions;

        il.RecordOriginalCode(method);
        il.VerifyCodeHash(method, "c8bac690");

        //- m_target.WriteAttributeString("xsi:type", m_customRootType);
        //+ m_target.WriteAttributeString("xsi", "type", XSI_NS_URL, m_customRootType);

        var xsiTypeInstructionIndex = -1;
        for (var i = 0; i < il.Count; i++)
            if (il[i].OpCode == OpCodes.Ldstr && il[i].Operand is string str && str == "xsi:type")
            {
                xsiTypeInstructionIndex = i;
                break;
            }

        Debug.Assert(xsiTypeInstructionIndex != -1, "Could not find ldstr \"xsi:type\" instruction");

        var callvirtIndex = -1;
        for (var i = xsiTypeInstructionIndex; i < il.Count; i++)
            if (il[i].OpCode == OpCodes.Callvirt && il[i].Operand is MethodReference mr && mr.Name == "WriteAttributeString")
            {
                callvirtIndex = i;
                break;
            }

        Debug.Assert(callvirtIndex != -1, "Could not find callvirt WriteAttributeString instruction");

        var existingMethodRef = (MethodReference)il[callvirtIndex].Operand;
        var xmlWriterType = existingMethodRef.DeclaringType;
        var writeAttributeString4Param = new MethodReference("WriteAttributeString", module.TypeSystem.Void, xmlWriterType)
        {
            HasThis = true
        };
        writeAttributeString4Param.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
        writeAttributeString4Param.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
        writeAttributeString4Param.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
        writeAttributeString4Param.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));

        il[xsiTypeInstructionIndex].Operand = "xsi";

        il.Insert(xsiTypeInstructionIndex + 1, Instruction.Create(OpCodes.Ldstr, "type"));
        il.Insert(xsiTypeInstructionIndex + 2, Instruction.Create(OpCodes.Ldstr, XSI_NS_URL));

        il[callvirtIndex + 2].Operand = writeAttributeString4Param;

        il.RecordPatchedCode(method);
    }

    private static void PrepatchMyAbstractXmlSerializer(ModuleDefinition module)
    {
        var type = module.GetTypes().First(t => t.Name.Contains("MyAbstractXmlSerializer"));
        var method = type.Methods.First(m => m.Name == "GetTypeAttribute");
        Debug.Assert(method != null, "Could not find the GetTypeAttribute method");
        var body = method.Body;
        var il = body.Instructions;

        il.RecordOriginalCode(method);
        il.VerifyCodeHash(method, "320acfb0");

        //- return reader.GetAttribute("xsi:type");
        //+ return reader.GetAttribute("type", XSI_NS_URL);

        var xsiTypeInstructionIndex = -1;
        for (var i = 0; i < il.Count; i++)
            if (il[i].OpCode == OpCodes.Ldstr && il[i].Operand is string str && str == "xsi:type")
            {
                xsiTypeInstructionIndex = i;
                break;
            }

        Debug.Assert(xsiTypeInstructionIndex != -1, "Could not find ldstr \"xsi:type\" instruction");

        var callvirtIndex = -1;
        for (var i = xsiTypeInstructionIndex; i < il.Count; i++)
            if (il[i].OpCode == OpCodes.Callvirt && il[i].Operand is MethodReference mr && mr.Name == "GetAttribute")
            {
                callvirtIndex = i;
                break;
            }

        Debug.Assert(callvirtIndex != -1, "Could not find callvirt GetAttribute instruction");

        var existingMethodRef = (MethodReference)il[callvirtIndex].Operand;
        var xmlReaderType = existingMethodRef.DeclaringType;
        var getAttribute2Param = new MethodReference("GetAttribute", module.TypeSystem.String, xmlReaderType)
        {
            HasThis = true
        };
        getAttribute2Param.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
        getAttribute2Param.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));

        il[xsiTypeInstructionIndex].Operand = "type";

        il.Insert(xsiTypeInstructionIndex + 1, Instruction.Create(OpCodes.Ldstr, XSI_NS_URL));

        il[callvirtIndex + 1].Operand = getAttribute2Param;

        il.RecordPatchedCode(method);
    }
}
