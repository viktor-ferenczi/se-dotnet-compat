using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ServerPlugin.Patches.Windows;

public static class WindowsServicePrepatch
{
    public static void Prepatch(AssemblyDefinition asmDef)
    {
        if (asmDef.Name.Name != "VRage.Dedicated")
            return;

        PatchDedicatedServerRun(asmDef);
        PatchSelectInstanceForm(asmDef);
    }

    private static void PatchDedicatedServerRun(AssemblyDefinition asmDef)
    {
        var type = asmDef.MainModule.GetType("VRage.Dedicated.DedicatedServer");
        if (type == null)
            return;

        var method = type.Methods.FirstOrDefault(m => m.Name == "Run" && m.Parameters.Count == 2);
        if (method == null)
            return;

        var instructions = method.Body.Instructions;

        for (var i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
            if (instr.OpCode != OpCodes.Newobj)
                continue;

            if (instr.Operand is not MethodReference ctor)
                continue;

            if (ctor.DeclaringType.Name != "WindowsService")
                continue;

            instructions[i] = Instruction.Create(OpCodes.Nop);
            if (i + 1 < instructions.Count)
                instructions[i + 1] = Instruction.Create(OpCodes.Ret);

            break;
        }
    }

    private static void PatchSelectInstanceForm(AssemblyDefinition asmDef)
    {
        var type = asmDef.MainModule.GetType("VRage.Dedicated.Configurator.SelectInstanceForm");
        if (type == null)
            return;

        var instanceType = type.NestedTypes.FirstOrDefault(t => t.Name == "Instance");
        if (instanceType != null)
        {
            var controllerField = instanceType.Fields.FirstOrDefault(f => f.Name == "Controller");
            if (controllerField != null)
            {
                controllerField.FieldType = asmDef.MainModule.TypeSystem.Object;
            }
        }

        foreach (var method in type.Methods)
        {
            if (!method.HasBody)
                continue;

            if (!ReferencesServiceController(method))
                continue;

            ClearMethodBody(method);
        }

        PatchSelectInstanceFormSize(asmDef, type);
    }

    private static void PatchSelectInstanceFormSize(AssemblyDefinition asmDef, TypeDefinition type)
    {
        var initMethod = type.Methods.FirstOrDefault(m => m.Name == "InitializeComponent");
        if (initMethod?.HasBody != true)
            return;

        var instructions = initMethod.Body.Instructions;

        // Fixed min/max sizes break the form after DPI scaling.
        for (var i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
            if (instr.OpCode != OpCodes.Call && instr.OpCode != OpCodes.Callvirt)
                continue;

            if (instr.Operand is not MethodReference setter)
                continue;

            if (setter.Name != "set_MaximumSize" && setter.Name != "set_MinimumSize")
                continue;

            // The four preceding instructions build the Size argument and load this.
            for (var j = i; j >= 0 && j > i - 5; j--)
            {
                instructions[j] = Instruction.Create(OpCodes.Nop);
            }
        }
    }

    private static bool ReferencesServiceController(MethodDefinition method)
    {
        foreach (var instr in method.Body.Instructions)
        {
            if (instr.Operand is MemberReference memberRef)
            {
                var typeName = memberRef switch
                {
                    MethodReference mr => mr.DeclaringType?.FullName,
                    FieldReference fr => fr.DeclaringType?.FullName ?? fr.FieldType?.FullName,
                    TypeReference tr => tr.FullName,
                    _ => null,
                };

                if (typeName != null && typeName.Contains("ServiceController"))
                    return true;
            }

            if (
                instr.Operand is TypeReference typeRef
                && typeRef.FullName.Contains("ServiceController")
            )
                return true;
        }

        foreach (var variable in method.Body.Variables)
        {
            if (variable.VariableType.FullName.Contains("ServiceController"))
                return true;
        }

        return false;
    }

    private static void ClearMethodBody(MethodDefinition method)
    {
        var il = method.Body.GetILProcessor();
        method.Body.Instructions.Clear();
        method.Body.ExceptionHandlers.Clear();
        method.Body.Variables.Clear();

        if (method.ReturnType.FullName != "System.Void")
        {
            if (method.ReturnType.IsValueType)
            {
                var local = new VariableDefinition(method.ReturnType);
                method.Body.Variables.Add(local);
                il.Append(Instruction.Create(OpCodes.Ldloca_S, local));
                il.Append(Instruction.Create(OpCodes.Initobj, method.ReturnType));
                il.Append(Instruction.Create(OpCodes.Ldloc_0));
            }
            else
            {
                il.Append(Instruction.Create(OpCodes.Ldnull));
            }
        }

        il.Append(Instruction.Create(OpCodes.Ret));
    }
}
