#if SIXLABORS_FIXES

using System.Linq;
using ClientPlugin.Tools;
using Mono.Cecil;

namespace ClientPlugin.Patches.ImageProcessing;

public static class MyTextureDataPrepatch
{
    public static void Prepatch(AssemblyDefinition asmDef)
    {
        if (asmDef.Name.Name != "VRage.Render11")
            return;

        var module = asmDef.MainModule;

        var myTextureDataType = module.GetType("VRageRender.MyTextureData");
        PrepatchSave(module, myTextureDataType);
    }

    private static void PrepatchSave(ModuleDefinition module, TypeDefinition type)
    {
        // Find the Save(Format, Stream, FileFormat, IntPtr, int, Vector2I) method
        var method = type.Methods.First(m =>
            m.Name == "Save" &&
            m.Parameters.Count == 6 &&
            m.Parameters[0].ParameterType.Name == "Format");

        var il = method.Body.Instructions;
        il.RecordOriginalCode(method);
        
        var scope = module.AssemblyReferences.First(r => r.Name == "SixLabors.ImageSharp");
        SixLaborsHelpers.ReplacePixelFormats(module, scope, il);
        
        il.RecordPatchedCode(method);
    }
}

#endif