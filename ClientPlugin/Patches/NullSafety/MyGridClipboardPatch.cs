using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.SessionComponents.Clipboard;
using VRage.Game;
using VRage.Game.Entity;

namespace ClientPlugin.Patches.NullSafety;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyGridClipboard))]
// ReSharper disable once UnusedType.Global
public static class MyGridClipboardPatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyPrefix]
    [HarmonyPatch("Deactivate")]
    private static bool DeactivatePrefix()
    {
        return MyClipboardComponent.Static != null;
    }

    // ReSharper disable once UnusedMember.Local
    [HarmonyPrefix]
    [HarmonyPatch("AddSingleBlockRequirements")]
    private static bool AddSingleBlockRequirementsPrefix(MyObjectBuilder_CubeBlock block, MyComponentList buildComponents)
    {
        // Run the original if the stockpile items are present
        if (block?.ConstructionStockpile?.Items != null) 
            return true;
        
        // Still need to get mounted components even if stockpile is missing
        if (block != null && buildComponents != null)
        {
            // This is the first line of the original method
            MyComponentStack.GetMountedComponents(buildComponents, block);
        }
        
        // Skip original method
        return false;
    }
}
