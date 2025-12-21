#if SIXLABORS_FIXES

using Mono.Cecil;

namespace ClientPlugin.Patches.ImageProcessing;

public static class MyImagePrepatch
{
    public static void Prepatch(AssemblyDefinition asmDef)
    {
        if (asmDef.Name.Name != "VRage.Render")
            return;
        
        // Follow SixLabors API change
        // TODO: Implement patching of the VRage.Render.Image.MyImage class and its methods to match the code changes you can find in the MyImage.original.cs to the MyImage.modified.cs file.
    }
}

#endif
