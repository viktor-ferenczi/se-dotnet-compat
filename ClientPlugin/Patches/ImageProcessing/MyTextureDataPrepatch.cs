#if SIXLABORS_FIXES

using System.Linq;
using ClientPlugin.Tools;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ClientPlugin.Patches.ImageProcessing;

public static class MyTextureDataPrepatch
{
    public static void Prepatch(AssemblyDefinition asmDef)
    {
        if (asmDef.Name.Name != "VRage.Render11")
            return;
        
        var module = asmDef.MainModule;
        
        var myTextureDataType = module.GetType("VRageRender.MyTextureData");
        PatchSaveMethod(module, myTextureDataType);
    }

    private static void PatchSaveMethod(ModuleDefinition module, TypeDefinition type)
    {
        // Find the Save(Format, Stream, FileFormat, IntPtr, int, Vector2I) method
        var method = type.Methods.First(m =>
            m.Name == "Save" &&
            m.Parameters.Count == 6 &&
            m.Parameters[0].ParameterType.Name == "Format");
        
        var il = method.Body.Instructions;
        
        il.RecordOriginalCode(method);
        
        // Patch all Gray8 -> L8 and Gray16 -> L16 references in MyImage.Save<TPixel> calls
        // These are calls like: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Gray16>(...)
        for (var i = 0; i < il.Count; i++)
        {
            var instr = il[i];
            if (instr.OpCode != OpCodes.Call || instr.Operand is not MethodReference methodRef)
                continue;
            
            // Check if this is a MyImage.Save method with a generic pixel format argument
            if (methodRef.Name != "Save" || methodRef.DeclaringType.Name != "MyImage")
                continue;
            
            if (methodRef is not GenericInstanceMethod genericMethod)
                continue;
            
            // Check the generic argument for Gray8 or Gray16
            for (var j = 0; j < genericMethod.GenericArguments.Count; j++)
            {
                var genArg = genericMethod.GenericArguments[j];
                
                if (genArg.Name == "Gray8")
                {
                    // Replace with L8
                    var l8Type = new TypeReference("SixLabors.ImageSharp.PixelFormats", "L8", module, 
                        module.AssemblyReferences.First(r => r.Name == "SixLabors.ImageSharp"), true);
                    genericMethod.GenericArguments[j] = l8Type;
                }
                else if (genArg.Name == "Gray16")
                {
                    // Replace with L16
                    var l16Type = new TypeReference("SixLabors.ImageSharp.PixelFormats", "L16", module, 
                        module.AssemblyReferences.First(r => r.Name == "SixLabors.ImageSharp"), true);
                    genericMethod.GenericArguments[j] = l16Type;
                }
            }
        }
        
        il.RecordPatchedCode(method);
    }
}

#endif
