#if SIXLABORS_FIXES

using Mono.Cecil;

namespace ClientPlugin.Patches.ImageProcessing;

public static class MyTextureDataPrepatch
{
    public static void Prepatch(AssemblyDefinition asmDef)
    {
        if (asmDef.Name.Name != "VRage.Render11")
            return;
        
        // MyTextureData calls MyImage.Save with a SixLabors PixelFormat generic parameter
        // TODO: Implement patching of the VRageRender.MyTextureData class and its methods to match the code changes you can find in the MyTextureData.original.cs (IL code: MyTextureData.original.il) to the MyTextureData.modified.cs file.
    }
}

#endif
