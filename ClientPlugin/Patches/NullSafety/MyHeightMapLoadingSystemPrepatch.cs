using System.Linq;
using ClientPlugin.Tools;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ClientPlugin.Patches.NullSafety;

[HarmonyPatch]
public static class MyHeightMapLoadingSystemPrepatch
{
    public static void Prepatch(AssemblyDefinition asmDef)
    {
        if (asmDef.Name.Name != "Sandbox.Game")
            return;

        var type = asmDef.MainModule.GetType("Sandbox.Game.GameSystems.MyHeightMapLoadingSystem");
        var method = type.Methods.First(m => m.Name == "Release");
        var methodBody = method.Body;
        var il = methodBody.Instructions;

        il.RecordOriginalCode(method);

        // The maps can already be set to null during unload
        // If maps == null, then skip to the ret instruction at the end of method
        il.Insert(0, Instruction.Create(OpCodes.Ldarg_3));
        il.Insert(1, Instruction.Create(OpCodes.Brfalse_S, il.Last()));

        il.RecordPatchedCode(method);
    }
}