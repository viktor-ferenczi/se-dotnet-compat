using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace ClientPlugin.Patches.ImageProcessing;

public static class SixLaborsHelpers
{
    public static void ReplacePixelFormats(ModuleDefinition module, AssemblyNameReference scope, Collection<Instruction> il)
    {
        // Map SixLabors 1.0.0 image format names 
        var typeReplacements = new Dictionary<string, TypeReference>
        {
            { "Gray8", new TypeReference("SixLabors.ImageSharp.PixelFormats", "L8", module, scope, true) },
            { "Gray16", new TypeReference("SixLabors.ImageSharp.PixelFormats", "L16", module, scope, true) },
        };

        // Replace type in generic arguments
        foreach (var instruction in il)
        {
            switch (instruction.Operand)
            {
                case GenericInstanceMethod mr:
                    var i = 0;
                    foreach (var ga in mr.GenericArguments.ToList())
                    {
                        if (typeReplacements.TryGetValue(ga.Name, out var replacement))
                        {
                            mr.GenericArguments[i] = replacement;
                        }

                        i++;
                    }

                    break;
            }
        }
    }

    public static void ReplaceType(ModuleDefinition module, AssemblyNameReference scope, MethodDefinition method, string originalTypeName, string replacementTypeNamespace, string replacementTypeName)
    {
        // Create ImageInfo type reference
        var replacementType = module.ImportReference(new TypeReference(replacementTypeNamespace, replacementTypeName, module, scope, false));
        
        // Replace in return type
        ReplaceTypeInGenericParameters(method.ReturnType.GenericParameters, originalTypeName, replacementType);
        if (method.ReturnType.Name == originalTypeName)
            method.ReturnType = replacementType;
        
        // Replace in generic parameter
        ReplaceTypeInGenericParameters(method.GenericParameters, originalTypeName, replacementType);
        foreach (var pa in method.Parameters)
        {
            ReplaceTypeInGenericParameters(pa.ParameterType.GenericParameters, originalTypeName, replacementType);
            if (pa.ParameterType.Name == originalTypeName)
                pa.ParameterType = replacementType;
        }
        
        // Replace in all variable types
        foreach (var variable in method.Body.Variables)
        {
            ReplaceTypeInGenericParameters(variable.VariableType.GenericParameters, originalTypeName, replacementType);
            if (variable.VariableType.Name == originalTypeName)
                variable.VariableType = replacementType;
        }
        
        // Replace in all method calls
        foreach (var instruction in method.Body.Instructions)
        {
            switch (instruction.Operand)
            {
                case MethodReference mr:
                    if (mr.ReturnType.Name == originalTypeName)
                        mr.ReturnType = replacementType;
                    if (mr.DeclaringType.Name == originalTypeName)
                        mr.DeclaringType = replacementType;
                    ReplaceTypeInGenericParameters(mr.GenericParameters, originalTypeName, replacementType);
                    foreach (var pa in mr.Parameters)
                    {
                        if (pa.ParameterType.Name == originalTypeName)
                            pa.ParameterType = replacementType;
                    }
                    break;
            }
        }
        
        /* FIXME
        // Change preceding ldloc.0 to ldloca.s 0 for value type method call
        if (i > 0)
        {
            var prevInstr = il[i - 1];
            if (prevInstr.OpCode == OpCodes.Ldloc_0)
            {
                prevInstr.OpCode = OpCodes.Ldloca_S;
                prevInstr.Operand = variables[0];
            }
            else if (prevInstr.OpCode == OpCodes.Ldloc && prevInstr.Operand is VariableDefinition varDef && varDef.Index == 0)
            {
                prevInstr.OpCode = OpCodes.Ldloca;
            }
        }
        */
    }

    private static void ReplaceTypeInGenericParameters(Collection<GenericParameter> genericParameters, string originalTypeName, TypeReference replacementType)
    {
        if (genericParameters == null)
            return;
        
        foreach (var gp in genericParameters)
        {
            if (gp.DeclaringType != null && gp.DeclaringType.Name == originalTypeName)
                gp.DeclaringType = replacementType;
        }
    }
}