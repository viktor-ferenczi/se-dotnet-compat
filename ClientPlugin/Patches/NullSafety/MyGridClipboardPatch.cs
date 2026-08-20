using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.SessionComponents.Clipboard;
using VRage.Game;
using VRage.Game.Entity;

namespace ClientPlugin.Patches.NullSafety;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyGridClipboard))]
public static class MyGridClipboardPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyGridClipboard.Deactivate))]
    private static bool DeactivatePrefix()
    {
        return MyClipboardComponent.Static != null;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyGridClipboard.AddSingleBlockRequirements))]
    private static bool AddSingleBlockRequirementsPrefix(MyObjectBuilder_CubeBlock block, MyComponentList buildComponents)
    {
        if (block?.ConstructionStockpile?.Items != null) 
            return true;
        
        if (block != null && buildComponents != null)
        {
            MyComponentStack.GetMountedComponents(buildComponents, block);
        }

        return false;
    }
}
