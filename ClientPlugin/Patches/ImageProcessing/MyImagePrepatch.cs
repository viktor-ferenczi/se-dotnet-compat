#if SIXLABORS_FIXES

using System.Diagnostics;
using System.Linq;
using ClientPlugin.Tools;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
        PatchMyImageStaticConstructor(module, myImageType);
        PatchMyImageLoad(module, myImageType);
        
        // Patch the generic MyImage<TData> class
        var myImageGenericType = module.GetTypes().First(t => t.FullName.StartsWith("VRage.Render.Image.MyImage`1"));
        PatchMyImageGenericCreateStream(module, myImageGenericType);
    }

    private static void PatchMyImageStaticConstructor(ModuleDefinition module, TypeDefinition type)
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
        
        for (var i = 0; i < il.Count; i++)
        {
            var instr = il[i];
            
            // Replace SimpleGcMemoryAllocator constructor
            if (instr.OpCode == OpCodes.Newobj && instr.Operand is MethodReference ctorRef)
            {
                if (ctorRef.DeclaringType.Name == "SimpleGcMemoryAllocator" && 
                    ctorRef.DeclaringType.Namespace == "SixLabors.Memory")
                {
                    // Change namespace from SixLabors.Memory to SixLabors.ImageSharp.Memory
                    var newAllocatorType = new TypeReference("SixLabors.ImageSharp.Memory", "SimpleGcMemoryAllocator", module, imageSharpRef, false);
                    newAllocatorType = (TypeReference)module.ImportReference(newAllocatorType);
                    var newCtorRef = new MethodReference(".ctor", module.TypeSystem.Void, newAllocatorType) { HasThis = true };
                    instr.Operand = newCtorRef;
                }
            }
            
            // Replace set_MemoryAllocator parameter type
            if (instr.OpCode == OpCodes.Callvirt && instr.Operand is MethodReference setterRef)
            {
                if (setterRef.Name == "set_MemoryAllocator" && setterRef.DeclaringType.Name == "Configuration")
                {
                    // Create new method reference with updated parameter type
                    var configType = new TypeReference("SixLabors.ImageSharp", "Configuration", module, imageSharpRef, false);
                    configType = (TypeReference)module.ImportReference(configType);
                    var memoryAllocatorType = new TypeReference("SixLabors.ImageSharp.Memory", "MemoryAllocator", module, imageSharpRef, false);
                    memoryAllocatorType = (TypeReference)module.ImportReference(memoryAllocatorType);
                    
                    var newSetterRef = new MethodReference("set_MemoryAllocator", module.TypeSystem.Void, configType) { HasThis = true };
                    newSetterRef.Parameters.Add(new ParameterDefinition(memoryAllocatorType));
                    instr.Operand = newSetterRef;
                }
            }
        }
        
        il.RecordPatchedCode(method);
    }

    private static void PatchMyImageLoad(ModuleDefinition module, TypeDefinition type)
    {
        // Find the Load(Stream, bool, bool, string) method
        var method = type.Methods.First(m =>
            m.Name == "Load" &&
            m.Parameters.Count == 4 &&
            m.Parameters[0].ParameterType.Name == "Stream");
        
        var il = method.Body.Instructions;
        
        il.RecordOriginalCode(method);
        
        var imageSharpRef = module.AssemblyReferences.First(r => r.Name == "SixLabors.ImageSharp");
        
        // Create ImageInfo type reference
        var imageInfoType = new TypeReference("SixLabors.ImageSharp", "ImageInfo", module, imageSharpRef, false);
        imageInfoType = module.ImportReference(imageInfoType);
        
        // Patch local variable 0: IImageInfo -> ImageInfo
        Debug.Assert(method.Body.Variables.Count > 0, "Expected at least one local variable");
        Debug.Assert(method.Body.Variables[0].VariableType.Name == "IImageInfo", 
            $"Expected local 0 to be IImageInfo, got {method.Body.Variables[0].VariableType.Name}");
        method.Body.Variables[0].VariableType = imageInfoType;
        
        // Patch Image.Identify() return type and all IImageInfo interface calls
        // Since ImageInfo is a value type, we need to change ldloc.0 to ldloca.s 0 before method calls
        for (var i = 0; i < il.Count; i++)
        {
            var instr = il[i];
            
            // Patch Image.Identify() call - return type changes from IImageInfo to ImageInfo
            if (instr.OpCode == OpCodes.Call && instr.Operand is MethodReference identifyRef)
            {
                if (identifyRef.Name == "Identify" && identifyRef.DeclaringType.Name == "Image")
                {
                    var imageType = new TypeReference("SixLabors.ImageSharp", "Image", module, imageSharpRef, false);
                    imageType = (TypeReference)module.ImportReference(imageType);
                    var newIdentifyRef = new MethodReference("Identify", imageInfoType, imageType);
                    newIdentifyRef.Parameters.Add(new ParameterDefinition(module.ImportReference(typeof(global::System.IO.Stream))));
                    instr.Operand = newIdentifyRef;
                }
            }
            
            // Patch IImageInfo interface calls to use ImageInfo struct
            if (instr.OpCode == OpCodes.Callvirt && instr.Operand is MethodReference interfaceMethodRef)
            {
                if (interfaceMethodRef.DeclaringType.Name == "IImageInfo")
                {
                    // Change get_PixelType, get_Width, get_Height to use ImageInfo
                    var methodName = interfaceMethodRef.Name;
                    var returnType = interfaceMethodRef.ReturnType;
                    
                    // For ImageInfo (struct), we need to use Call instead of Callvirt
                    if (methodName == "get_PixelType" || methodName == "get_Width" || methodName == "get_Height")
                    {
                        var newMethodRef = new MethodReference(methodName, returnType, imageInfoType) { HasThis = true };
                        instr.Operand = newMethodRef;
                        // Change from callvirt to call for value type
                        instr.OpCode = OpCodes.Call;
                        
                        // Change preceding ldloc.0 to ldloca.s 0 for value type method call
                        if (i > 0)
                        {
                            var prevInstr = il[i - 1];
                            if (prevInstr.OpCode == OpCodes.Ldloc_0)
                            {
                                prevInstr.OpCode = OpCodes.Ldloca_S;
                                prevInstr.Operand = method.Body.Variables[0];
                            }
                            else if (prevInstr.OpCode == OpCodes.Ldloc && prevInstr.Operand is VariableDefinition varDef && varDef.Index == 0)
                            {
                                prevInstr.OpCode = OpCodes.Ldloca;
                            }
                        }
                    }
                }
            }
        }
        
        // Patch all Gray8 -> L8 and Gray16 -> L16 references in method call operands
        // These are calls to MyImage<TData>.Create<TPixel> with various pixel format type arguments
        for (var i = 0; i < il.Count; i++)
        {
            var instr = il[i];
            if (instr.OpCode != OpCodes.Call || instr.Operand is not MethodReference methodRef)
                continue;
            
            // Check if this is a Create method with a generic pixel format argument
            if (methodRef.Name != "Create" || methodRef is not GenericInstanceMethod genericMethod)
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
        
        // Patch MyImage<T>.Create<TPixel>(IImageInfo) method parameter from IImageInfo to ImageInfo
        // This is critical because ImageInfo is now a struct (value type) instead of an interface
        for (var i = 0; i < il.Count; i++)
        {
            var instr = il[i];
            if (instr.OpCode != OpCodes.Call || instr.Operand is not GenericInstanceMethod genericMethod)
                continue;
            
            // Check if this is a Create method on MyImage<T>
            if (genericMethod.Name != "Create" || !genericMethod.DeclaringType.Name.StartsWith("MyImage`1"))
                continue;
            
            // Check if it has a parameter of type IImageInfo
            if (genericMethod.Parameters.Count != 1)
                continue;
            
            var paramType = genericMethod.Parameters[0].ParameterType;
            if (paramType.Name != "IImageInfo")
                continue;
            
            // Create new method reference with ImageInfo parameter instead of IImageInfo
            var declaringType = genericMethod.DeclaringType;
            var returnType = genericMethod.ReturnType;
            
            // Create the base method reference
            var newMethodRef = new MethodReference("Create", returnType, declaringType);
            newMethodRef.Parameters.Add(new ParameterDefinition(imageInfoType));
            
            // Copy generic parameters from the method (TPixel)
            foreach (var genParam in genericMethod.GenericParameters)
            {
                newMethodRef.GenericParameters.Add(new GenericParameter(genParam.Name, newMethodRef));
            }
            
            // Create the generic instance with the same generic arguments
            var newGenericMethod = new GenericInstanceMethod(newMethodRef);
            foreach (var genArg in genericMethod.GenericArguments)
            {
                newGenericMethod.GenericArguments.Add(genArg);
            }
            
            // Replace the instruction's operand
            instr.Operand = newGenericMethod;
        }
        
        // Patch GetFormatMetaData<PngMetaData>(PngFormat.Instance) -> GetPngMetadata()
        // And MetaData -> Metadata property access
        for (var i = il.Count - 1; i >= 0; i--)
        {
            var instr = il[i];
            
            // Replace get_MetaData with get_Metadata (property name change)
            if (instr.OpCode == OpCodes.Callvirt && instr.Operand is MethodReference propMethodRef)
            {
                if (propMethodRef.Name == "get_MetaData" && propMethodRef.DeclaringType.Name == "IImageInfo")
                {
                    // Need to change from IImageInfo.get_MetaData() to ImageInfo.get_Metadata()
                    var metadataType = new TypeReference("SixLabors.ImageSharp.Metadata", "ImageMetadata", module,
                        module.AssemblyReferences.First(r => r.Name == "SixLabors.ImageSharp"), false);
                    
                    var getMetadataMethod = new MethodReference("get_Metadata", metadataType, imageInfoType) { HasThis = true };
                    instr.Operand = getMetadataMethod;
                    
                    // Change from callvirt to call for value type
                    instr.OpCode = OpCodes.Call;
                    
                    // Change preceding ldloc.0 to ldloca.s 0 for value type method call
                    if (i > 0)
                    {
                        var prevInstr = il[i - 1];
                        if (prevInstr.OpCode == OpCodes.Ldloc_0)
                        {
                            prevInstr.OpCode = OpCodes.Ldloca_S;
                            prevInstr.Operand = method.Body.Variables[0];
                        }
                        else if (prevInstr.OpCode == OpCodes.Ldloc && prevInstr.Operand is VariableDefinition varDef && varDef.Index == 0)
                        {
                            prevInstr.OpCode = OpCodes.Ldloca;
                        }
                    }
                }
            }
            
            // Replace GetFormatMetaData<PngMetaData>(PngFormat) -> GetPngMetadata()
            if (instr.OpCode == OpCodes.Callvirt && instr.Operand is MethodReference formatMethodRef)
            {
                if (formatMethodRef.Name == "GetFormatMetaData" && formatMethodRef is GenericInstanceMethod)
                {
                    // Find the preceding instruction that loads PngFormat.Instance and remove it
                    Debug.Assert(i >= 1, "Expected preceding instruction for PngFormat.Instance");
                    var prevInstr = il[i - 1];
                    Debug.Assert(prevInstr.OpCode == OpCodes.Call, $"Expected Call OpCode at index {i-1}, got {prevInstr.OpCode}");
                    Debug.Assert(prevInstr.Operand is MethodReference mr && mr.Name == "get_Instance" && mr.DeclaringType.Name == "PngFormat",
                        "Expected call to PngFormat.get_Instance");
                    
                    // Remove the PngFormat.Instance call
                    il.RemoveAt(i - 1);
                    i--; // Adjust index after removal
                    
                    // Replace GetFormatMetaData with GetPngMetadata
                    var imageMetadataType = new TypeReference("SixLabors.ImageSharp.Metadata", "ImageMetadata", module,
                        module.AssemblyReferences.First(r => r.Name == "SixLabors.ImageSharp"), false);
                    var pngMetadataType = new TypeReference("SixLabors.ImageSharp.Formats.Png", "PngMetadata", module,
                        module.AssemblyReferences.First(r => r.Name == "SixLabors.ImageSharp"), false);
                    
                    var getPngMetadataMethod = new MethodReference("GetPngMetadata", pngMetadataType, imageMetadataType) { HasThis = true };
                    il[i].Operand = getPngMetadataMethod;
                }
            }
        }
        
        il.RecordPatchedCode(method);
    }
    
    private static void PatchMyImageGenericCreateStream(ModuleDefinition module, TypeDefinition type)
    {
        // Find the Create<TImage>(Stream) method
        var method = type.Methods.First(m =>
            m.Name == "Create" &&
            m.Parameters.Count == 1 &&
            m.Parameters[0].ParameterType.Name == "Stream");
        
        var il = method.Body.Instructions;
        
        il.RecordOriginalCode(method);
        
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
