#if SIXLABORS_FIXES

using ClientPlugin.Tools;
using Mono.Cecil;

namespace ClientPlugin.Patches.ImageProcessing;

public static class MyImagePrepatch
{
    public static void Prepatch(AssemblyDefinition asmDef)
    {
        var plugin = CecilExtensions.PluginAssemblyDefinition;

        switch (asmDef.Name.Name)
        {
            case "VRage.Render":
                // Follow SixLabors API change
                asmDef.MainModule.ReplaceType("VRage.Render.Image.MyImage", plugin.MainModule, "ClientPlugin.Patches.ImageProcessing.MyImage");
                break;
            
            case "VRage.Render11":
                // MyTextureData calls MyImage.Save with a SixLabors PixelFormat generic parameter
                asmDef.MainModule.ReplaceType("VRageRender.MyTextureData", plugin.MainModule, "ClientPlugin.Patches.ImageProcessing.MyTextureData");
                break;
        }
    }
}

#endif
