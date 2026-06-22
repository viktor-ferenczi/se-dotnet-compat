using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ServerPlugin.Patches.Windows;

/// <summary>
/// Removes the WinForms configurator setup from the dedicated server entry
/// point so the DS can run on a plain net10.0 host (no Microsoft.WindowsDesktop.App).
///
/// SpaceEngineersDedicated.MyProgram.Main begins by populating three static
/// fields used only by the (never-shown on a headless server) configurator UI:
///   * VRage.Dedicated.Configurator.SelectInstanceForm.LogoImage = Resources.SpaceEngineersDSLogo
///       -> pulls in System.Drawing.Common
///   * VRage.Dedicated.ConfigForm.GameAttributes = Game.SpaceEngineers
///   * VRage.Dedicated.ConfigForm.OnReset = delegate { ... }
///       -> ConfigForm/SelectInstanceForm derive from System.Windows.Forms.Form
///
/// Because Main *references* these Form-derived types, the JIT eagerly tries to
/// load System.Windows.Forms (and System.Drawing) while compiling Main, throwing
/// FileNotFoundException before the server ever starts. Neither assembly exists
/// on a plain net10.0 host.
///
/// We NOP the contiguous configurator block (the logo load through the OnReset
/// assignment). It is self-contained: net stack delta 0, no external branch
/// targets into the range. Everything after it (MyVRageWindows.Init,
/// DedicatedServer.Run, ...) has no WinForms/Drawing references.
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

        // Anchor the block on its first and last instruction by member reference,
        // not by hardcoded IL offset (robust against minor game updates).
        var start = -1;
        var end = -1;

        for (var i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];

            // First instruction: load the DS logo bitmap.
            if (start < 0 &&
                (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) &&
                instr.Operand is MethodReference mr &&
                mr.Name == "get_SpaceEngineersDSLogo")
            {
                start = i;
                continue;
            }

            // Last instruction: assign ConfigForm.OnReset.
            if (start >= 0 &&
                instr.OpCode == OpCodes.Stsfld &&
                instr.Operand is FieldReference fr &&
                fr.Name == "OnReset" &&
                fr.DeclaringType?.FullName == "VRage.Dedicated.ConfigForm")
            {
                end = i;
                break;
            }
        }

        if (start < 0 || end < 0 || end < start)
            return;

        // Neutralize the block in place so object identity (and therefore any
        // branch target instructions, e.g. the delegate-cache brtrue.s within
        // the range) stays valid.
        for (var i = start; i <= end; i++)
        {
            instructions[i].OpCode = OpCodes.Nop;
            instructions[i].Operand = null;
        }
    }
}
