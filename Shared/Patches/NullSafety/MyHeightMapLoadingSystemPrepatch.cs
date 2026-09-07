using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Shared.Tools;

namespace Shared.Patches.NullSafety;

// Cecil prepatch, invoked explicitly from Preloader.Patch. Not a Harmony patch
// class, so it carries no Harmony attributes or category.
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
        il.VerifyCodeHash(method, "62847c5b");

        // Unload may already have cleared the maps.
        il.Insert(0, Instruction.Create(OpCodes.Ldarg_3));
        il.Insert(1, Instruction.Create(OpCodes.Brfalse_S, il.Last()));

        il.RecordPatchedCode(method);
    }
}
