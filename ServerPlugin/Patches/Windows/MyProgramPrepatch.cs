using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ServerPlugin.Patches.Windows;

/// <summary>
/// Removes the unused configurator code that makes Main load WinForms.
/// </summary>
public static class MyProgramPrepatch
{
    public static void Prepatch(AssemblyDefinition asmDef)
    {
        if (asmDef.Name.Name != "SpaceEngineersDedicated")
            return;

        var type = asmDef.MainModule.GetType("SpaceEngineersDedicated.MyProgram");
        if (type == null)
            return;

        var main = type.Methods.FirstOrDefault(m => m.Name == "Main" && m.IsStatic);
        if (main?.HasBody != true)
            return;

        var instructions = main.Body.Instructions;

        // Member names survive small IL changes better than offsets.
        var start = -1;
        var end = -1;

        for (var i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];

            if (
                start < 0
                && (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt)
                && instr.Operand is MethodReference mr
                && mr.Name == "get_SpaceEngineersDSLogo"
            )
            {
                start = i;
                continue;
            }

            if (
                start >= 0
                && instr.OpCode == OpCodes.Stsfld
                && instr.Operand is FieldReference fr
                && fr.Name == "OnReset"
                && fr.DeclaringType?.FullName == "VRage.Dedicated.ConfigForm"
            )
            {
                end = i;
                break;
            }
        }

        if (start < 0 || end < 0 || end < start)
            return;

        // NOP in place because branches may point into this block.
        for (var i = start; i <= end; i++)
        {
            instructions[i].OpCode = OpCodes.Nop;
            instructions[i].Operand = null;
        }
    }
}
