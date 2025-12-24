#if SIXLABORS_FIXES

using System.Diagnostics;
using System.Linq;
using ClientPlugin.Tools;
using Mono.Cecil;
using Mono.Cecil.Cil;
using VRageRender;

namespace ClientPlugin.Patches.ImageProcessing;

public static class MyImagePrepatch
{
    public static void Prepatch(AssemblyDefinition asmDef)
    {
        if (asmDef.Name.Name != "VRage.Render")
            return;

        var module = asmDef.MainModule;

        // Patch the static MyImage class
        var myImageType = module.GetType("VRage.Render.Image.MyImage");
        PatchStaticConstructor(module, myImageType);
        ReplaceTypes(module, myImageType);
        PatchLoadStream(module, myImageType);

        // Patch the generic MyImage<TData> class
        var myImageGenericType = module.GetTypes().First(t => t.FullName.StartsWith("VRage.Render.Image.MyImage`1"));
        PatchCreateStream(module, myImageGenericType);
    }

    private static void PatchStaticConstructor(ModuleDefinition module, TypeDefinition type)
    {
        // Find the static constructor (.cctor)
        var method = type.Methods.First(m => m.Name == ".cctor");
        var il = method.Body.Instructions;

        il.RecordOriginalCode(method);

        // Patch the MemoryAllocator namespace change from SixLabors.Memory to SixLabors.ImageSharp.Memory
        // Original IL:
        //   IL_0005: newobj instance void [SixLabors.Core]SixLabors.Memory.SimpleGcMemoryAllocator::.ctor()
        //   IL_000a: callvirt instance void [SixLabors.ImageSharp]SixLabors.ImageSharp.Configuration::set_MemoryAllocator(class [SixLabors.Core]SixLabors.Memory.MemoryAllocator)
        var imageSharpRef = module.AssemblyReferences.First(r => r.Name == "SixLabors.ImageSharp");
        var i = il.FindFirstIndex(i => i.OpCode == OpCodes.Newobj && i.Operand is MethodReference mr && mr.DeclaringType.Name == "SimpleGcMemoryAllocator");
        Debug.Assert(i != -1, "Could not find the use of SimpleGcMemoryAllocator");
        var newAllocatorType = new TypeReference("SixLabors.ImageSharp.Memory", "SimpleGcMemoryAllocator", module, imageSharpRef, false);
        newAllocatorType = (TypeReference)module.ImportReference(newAllocatorType);
        il[i].Operand = new MethodReference(".ctor", module.TypeSystem.Void, newAllocatorType) { HasThis = true };

        // Replace set_MemoryAllocator parameter type
        i = il.FindFirstIndex(i => i.OpCode == OpCodes.Callvirt && i.Operand is MethodReference mr && mr.DeclaringType.Name == "Configuration");
        var configType = new TypeReference("SixLabors.ImageSharp", "Configuration", module, imageSharpRef, false);
        configType = (TypeReference)module.ImportReference(configType);
        var memoryAllocatorType = new TypeReference("SixLabors.ImageSharp.Memory", "MemoryAllocator", module, imageSharpRef, false);
        memoryAllocatorType = (TypeReference)module.ImportReference(memoryAllocatorType);
        var newSetterRef = new MethodReference("set_MemoryAllocator", module.TypeSystem.Void, configType) { HasThis = true };
        newSetterRef.Parameters.Add(new ParameterDefinition(memoryAllocatorType));
        il[i].Operand = newSetterRef;

        il.RecordPatchedCode(method);
    }

    private static void ReplaceTypes(ModuleDefinition module, TypeDefinition type)
    {
        var scope = module.AssemblyReferences.First(r => r.Name == "SixLabors.ImageSharp");
        foreach (var method in type.Methods)
        {
            SixLaborsHelpers.ReplaceType(module, scope, method, "IImageInfo", "SixLabors.ImageSharp", "ImageInfo");
            SixLaborsHelpers.ReplaceType(module, scope, method, "PngMetaData", "SixLabors.ImageSharp.Formats.Png", "PngMetadata");
        }
    }

    private static void PatchLoadStream(ModuleDefinition module, TypeDefinition type)
    {
        // Find the Load(Stream, bool, bool, string) method
        var method = type.Methods.First(m =>
            m.Name == "Load" &&
            m.Parameters.Count == 4 &&
            m.Parameters[0].ParameterType.Name == "Stream");

        var il = method.Body.Instructions;
        il.RecordOriginalCode(method);

        var sixLaborsImageSharpScope = module.AssemblyReferences.First(r => r.Name == "SixLabors.ImageSharp");
        SixLaborsHelpers.ReplacePixelFormats(module, sixLaborsImageSharpScope, il);

        // Replace this code line:
        // - oneChannel = imageInfo.MetaData.GetFormatMetaData(PngFormat.Instance).ColorType == PngColorType.Grayscale;
        // + oneChannel = imageInfo.Metadata.GetPngMetadata().ColorType == PngColorType.Grayscale;
        //
        // Original IL:
        // IL_0012: ldloc.0
        // IL_0013: callvirt instance class [SixLabors.ImageSharp]SixLabors.ImageSharp.MetaData.ImageMetaData [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo::get_MetaData()
        // IL_0018: call class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngFormat [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngFormat::get_Instance()
        // IL_001d: callvirt instance !!0 [SixLabors.ImageSharp]SixLabors.ImageSharp.MetaData.ImageMetaData::GetFormatMetaData<class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngMetaData>(class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.IImageFormat`1<!!0>)
        // IL_0022: callvirt instance valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngColorType [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngMetaData::get_ColorType()
        // IL_0027: ldc.i4.0
        // IL_0028: ceq
        // IL_002a: starg.s oneChannel
        //
        // Replacement IL:
        // IL_001a: ldloc.0      // imageInfo
        // IL_001b: callvirt     instance class [SixLabors.ImageSharp]SixLabors.ImageSharp.Metadata.ImageMetadata [SixLabors.ImageSharp]SixLabors.ImageSharp.ImageInfo::get_Metadata()
        // IL_0020: call         class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngMetadata [SixLabors.ImageSharp]SixLabors.ImageSharp.MetadataExtensions::GetPngMetadata(class [SixLabors.ImageSharp]SixLabors.ImageSharp.Metadata.ImageMetadata)
        // IL_0025: callvirt     instance valuetype [System.Runtime]System.Nullable`1<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngColorType> [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngMetadata::get_ColorType()
        // IL_002a: stloc.2      // V_2
        // IL_002b: ldc.i4.0
        // IL_002c: stloc.3      // V_3
        // IL_002d: ldloca.s     V_2
        // IL_002f: call         instance !0/*valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngColorType*/ valuetype [System.Runtime]System.Nullable`1<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngColorType>::GetValueOrDefault()
        // IL_0034: ldloc.3      // V_3
        // IL_0035: ceq
        // IL_0037: ldloca.s     V_2
        // IL_0039: call         instance bool valuetype [System.Runtime]System.Nullable`1<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngColorType>::get_HasValue()
        // IL_003e: and
        // IL_003f: starg.s      oneChannel
        var i = il.FindFirstIndex(i => i.OpCode == OpCodes.Callvirt && i.Operand is MethodReference mr && mr.DeclaringType.Name == "ImageInfo" && mr.Name == "get_MetaData");
        Debug.Assert(i != -1, "Could not find the use of get_MetaData");
        
        // Remove original block
        i -= 1;
        for(var j = 0; j < 8; j++)
            il.RemoveAt(i);
        
        // Insert replacement block
        var start = i;
        
        // Create type references
        var imageInfoType = module.ImportReference(new TypeReference("SixLabors.ImageSharp", "ImageInfo", module, sixLaborsImageSharpScope, false));
        var imageMetadataType = module.ImportReference(new TypeReference("SixLabors.ImageSharp.Metadata", "ImageMetadata", module, sixLaborsImageSharpScope, false));
        var metadataExtensionsType = module.ImportReference(new TypeReference("SixLabors.ImageSharp", "MetadataExtensions", module, sixLaborsImageSharpScope, false));
        var pngMetadataType = module.ImportReference(new TypeReference("SixLabors.ImageSharp.Formats.Png", "PngMetadata", module, sixLaborsImageSharpScope, false));
        var pngColorTypeType = module.ImportReference(new TypeReference("SixLabors.ImageSharp.Formats.Png", "PngColorType", module, sixLaborsImageSharpScope, true));
        var systemRuntimeScope = module.AssemblyReferences.FirstOrDefault(r => r.Name == "System.Runtime") 
                               ?? module.AssemblyReferences.First(r => r.Name == "netstandard");
        var nullableTypeRef = module.ImportReference(new TypeReference("System", "Nullable`1", module, systemRuntimeScope, true));
        var nullablePngColorType = new GenericInstanceType(nullableTypeRef);
        nullablePngColorType.GenericArguments.Add(pngColorTypeType);
        
        // Create method references with proper return types
        var getMetadataMethod = new MethodReference("get_Metadata", imageMetadataType, imageInfoType) { HasThis = true };
        var getPngMetadataMethod = new MethodReference("GetPngMetadata", pngMetadataType, metadataExtensionsType);
        getPngMetadataMethod.Parameters.Add(new ParameterDefinition(imageMetadataType));
        var getColorTypeMethod = new MethodReference("get_ColorType", nullablePngColorType, pngMetadataType) { HasThis = true };
        
        // Insert IL instructions
        il.Insert(i++, Instruction.Create(OpCodes.Ldloc_0));
        il.Insert(i++, Instruction.Create(OpCodes.Callvirt, module.ImportReference(getMetadataMethod)));
        il.Insert(i++, Instruction.Create(OpCodes.Call, module.ImportReference(getPngMetadataMethod)));
        il.Insert(i++, Instruction.Create(OpCodes.Callvirt, module.ImportReference(getColorTypeMethod)));
        il.Insert(i++, Instruction.Create(OpCodes.Stloc_2));
        il.Insert(i++, Instruction.Create(OpCodes.Ldc_I4_0));
        il.Insert(i++, Instruction.Create(OpCodes.Stloc_3));
        il.Insert(i++, Instruction.Create(OpCodes.Ldloca_S, method.Body.Variables[2]));
        var getValueOrDefaultMethod = new MethodReference("GetValueOrDefault", pngColorTypeType, nullablePngColorType) { HasThis = true };
        il.Insert(i++, Instruction.Create(OpCodes.Call, module.ImportReference(getValueOrDefaultMethod)));
        il.Insert(i++, Instruction.Create(OpCodes.Ldloc_3));
        il.Insert(i++, Instruction.Create(OpCodes.Ceq));
        il.Insert(i++, Instruction.Create(OpCodes.Ldloca_S, method.Body.Variables[2]));
        var getHasValueMethod = new MethodReference("get_HasValue", module.TypeSystem.Boolean, nullablePngColorType) { HasThis = true };
        il.Insert(i++, Instruction.Create(OpCodes.Call, module.ImportReference(getHasValueMethod)));
        il.Insert(i++, Instruction.Create(OpCodes.And));
        il.Insert(i++, Instruction.Create(OpCodes.Starg_S, method.Parameters.First(p => p.Name == "oneChannel")));
        var size = i - start;
        Debug.Assert(size == 15, $"Wrong replacement block size of {size}, should be 15");

        // First, fix any remaining get_MetaData calls (old API with uppercase 'D') to get_Metadata (new API with lowercase 'd')
        // This must be done BEFORE looking for the GetFormatMetaData pattern
        var newMetadataGetter = new MethodReference("get_Metadata", imageMetadataType, imageInfoType) { HasThis = true };
        var newColorTypeGetter = new MethodReference("get_ColorType", nullablePngColorType, pngMetadataType) { HasThis = true };
        foreach (var ins in il)
        {
            if (ins.OpCode == OpCodes.Callvirt && ins.Operand is MethodReference mr1 && mr1.Name == "get_MetaData")
                ins.Operand = module.ImportReference(newMetadataGetter);
            if (ins.OpCode == OpCodes.Callvirt && ins.Operand is MethodReference mr2 && mr2.Name == "get_ColorType")
                ins.Operand = module.ImportReference(newColorTypeGetter);
        }

        // AI generated:
        // Now fix the second occurrence: Replace get_Metadata().GetFormatMetaData(PngFormat.Instance) with get_Metadata().GetPngMetadata()
        // This pattern appears later in the method and needs to be replaced
        // Original pattern (3 instructions):
        //   callvirt get_Metadata()
        //   call PngFormat.get_Instance()
        //   callvirt GetFormatMetaData<PngMetaData>()
        // Replacement (2 instructions):
        //   callvirt get_Metadata()
        //   call GetPngMetadata()
        
        i = il.FindFirstIndex(i => 
            i.OpCode == OpCodes.Call && 
            i.Operand is MethodReference mr && 
            mr.DeclaringType.Name == "PngFormat" && 
            mr.Name == "get_Instance");
        
        if (i != -1)
        {
            // Verify the pattern: should have get_Metadata before and GetFormatMetaData after
            Debug.Assert(i > 0 && il[i - 1].OpCode == OpCodes.Callvirt && 
                        il[i - 1].Operand is MethodReference mr1 && mr1.Name == "get_Metadata",
                        "Expected get_Metadata call before get_Instance");
            Debug.Assert(i + 1 < il.Count && il[i + 1].OpCode == OpCodes.Callvirt &&
                        il[i + 1].Operand is MethodReference mr2 && mr2.Name == "GetFormatMetaData",
                        "Expected GetFormatMetaData call after get_Instance");
            
            // Remove the get_Instance and GetFormatMetaData calls (2 instructions)
            il.RemoveAt(i); // Remove get_Instance
            il.RemoveAt(i); // Remove GetFormatMetaData (now at position i)
            
            // Insert GetPngMetadata call
            il.Insert(i, Instruction.Create(OpCodes.Call, module.ImportReference(getPngMetadataMethod)));
        }
        
        il.RecordPatchedCode(method);
    }

    private static void PatchCreateStream(ModuleDefinition module, TypeDefinition type)
    {
        // Find the Create<TImage>(Stream) method
        var method = type.Methods.First(m =>
            m.Name == "Create" &&
            m.Parameters.Count == 1 &&
            m.Parameters[0].ParameterType.Name == "Stream");

        var il = method.Body.Instructions;

        il.RecordOriginalCode(method);

        // AI generated:
        // Find GetPixelSpan<TImage> call and replace with GetPixelMemoryGroup().Single().Span
        // Original: call valuetype [System.Memory]System.Span`1<!!0> [SixLabors.ImageSharp]SixLabors.ImageSharp.Advanced.AdvancedImageExtensions::GetPixelSpan<!!TImage>(...)
        for (var i = 0; i < il.Count; i++)
        {
            var instr = il[i];
            if (instr.OpCode != OpCodes.Call || instr.Operand is not MethodReference methodRef)
                continue;

            if (methodRef.Name != "GetPixelSpan")
                continue;

            // Found GetPixelSpan call - need to replace with GetPixelMemoryGroup().Single().Span

            // Get the TImage generic parameter from the method
            var tImageParam = method.GenericParameters[0];

            // Get required type references
            var imageSharpRef = module.AssemblyReferences.First(r => r.Name == "SixLabors.ImageSharp");
            var linqRef = module.AssemblyReferences.FirstOrDefault(r => r.Name == "System.Linq")
                          ?? new AssemblyNameReference("System.Linq", new global::System.Version(4, 0, 0, 0));
            if (!module.AssemblyReferences.Contains(linqRef))
                module.AssemblyReferences.Add(linqRef);

            // Memory<TImage> type
            var memoryOpenType = new TypeReference("System", "Memory`1", module,
                module.AssemblyReferences.First(r => r.Name == "System.Memory"), true);
            var memoryOfTImage = new GenericInstanceType(memoryOpenType);
            memoryOfTImage.GenericArguments.Add(tImageParam);

            // IMemoryGroup<TImage> type
            var memoryGroupInterfaceType = new TypeReference("SixLabors.ImageSharp.Memory", "IMemoryGroup`1", module, imageSharpRef, false);
            var memoryGroupOfTImage = new GenericInstanceType(memoryGroupInterfaceType);
            memoryGroupOfTImage.GenericArguments.Add(tImageParam);

            // Image<TImage> type for the extension method's first parameter
            var imageOpenType = new TypeReference("SixLabors.ImageSharp", "Image`1", module, imageSharpRef, false);
            var imageOfTImage = new GenericInstanceType(imageOpenType);
            imageOfTImage.GenericArguments.Add(tImageParam);

            // GetPixelMemoryGroup<TImage>() method on ImageExtensions
            var imageExtensionsType = new TypeReference("SixLabors.ImageSharp.Advanced", "AdvancedImageExtensions", module, imageSharpRef, false);
            var getPixelMemoryGroupMethod = new MethodReference("GetPixelMemoryGroup", memoryGroupOfTImage, imageExtensionsType);
            getPixelMemoryGroupMethod.Parameters.Add(new ParameterDefinition(imageOfTImage));
            var getPixelMemoryGroupGeneric = new GenericInstanceMethod(getPixelMemoryGroupMethod);
            getPixelMemoryGroupGeneric.GenericArguments.Add(tImageParam);

            // Single<Memory<TImage>>() LINQ extension method - returns Memory<TImage>
            var enumerableType = new TypeReference("System.Linq", "Enumerable", module, linqRef, false);
            var singleMethod = new MethodReference("Single", memoryOfTImage, enumerableType);
            // IEnumerable<Memory<TImage>> parameter
            var ienumerableOpenType = new TypeReference("System.Collections.Generic", "IEnumerable`1", module,
                module.AssemblyReferences.First(r => r.Name == "netstandard" || r.Name == "System.Runtime"), false);
            var ienumerableOfMemory = new GenericInstanceType(ienumerableOpenType);
            ienumerableOfMemory.GenericArguments.Add(memoryOfTImage);
            singleMethod.Parameters.Add(new ParameterDefinition(ienumerableOfMemory));
            var singleMethodGeneric = new GenericInstanceMethod(singleMethod);
            singleMethodGeneric.GenericArguments.Add(memoryOfTImage);

            // Span<TImage> type
            var spanOpenType = new TypeReference("System", "Span`1", module,
                module.AssemblyReferences.First(r => r.Name == "System.Memory"), true);
            var spanOfTImage = new GenericInstanceType(spanOpenType);
            spanOfTImage.GenericArguments.Add(tImageParam);

            // get_Span property on Memory<TImage>
            var getSpanMethod = new MethodReference("get_Span", spanOfTImage, memoryOfTImage) { HasThis = true };

            // Replace the single GetPixelSpan call with three calls:
            // 1. GetPixelMemoryGroup()
            // 2. Single()
            // 3. get_Span

            il[i].Operand = getPixelMemoryGroupGeneric;

            // Insert Single() call after GetPixelMemoryGroup
            il.Insert(i + 1, Instruction.Create(OpCodes.Call, singleMethodGeneric));

            // Insert get_Span call (needs ldloca for value type)
            // Actually, Single returns Memory<T> by value, so we need to store it, get address, then call get_Span
            // Let's add a local variable for Memory<TImage>
            var memoryLocal = new VariableDefinition(memoryOfTImage);
            method.Body.Variables.Add(memoryLocal);
            var localIndex = method.Body.Variables.Count - 1;

            // After Single(), store result in local, load address, call get_Span
            il.Insert(i + 2, Instruction.Create(OpCodes.Stloc, memoryLocal));
            il.Insert(i + 3, Instruction.Create(OpCodes.Ldloca, memoryLocal));
            il.Insert(i + 4, Instruction.Create(OpCodes.Call, getSpanMethod));

            break; // Only one GetPixelSpan call in this method
        }

        il.RecordPatchedCode(method);
    }
}

#endif
