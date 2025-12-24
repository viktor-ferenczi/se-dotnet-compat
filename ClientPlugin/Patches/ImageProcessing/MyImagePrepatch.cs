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

        var scope = module.AssemblyReferences.First(r => r.Name == "SixLabors.ImageSharp");
        SixLaborsHelpers.ReplacePixelFormats(module, scope, il);

        // Empty the method, so it can be replaced by a transpiler
        // Keep only: ldnull, ret
        var instructions = method.Body.Instructions;
        var count = instructions.Count;
        var ldnull = instructions[count - 2];
        var ret = instructions[count - 1];
        instructions.Clear();
        instructions.Add(ldnull);
        instructions.Add(ret);
        
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