using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ClientPlugin.Tools;

public static class CecilExtensions
{
    public static AssemblyDefinition PluginAssemblyDefinition => AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location);

    private static TypeReference ImportType(ModuleDefinition targetModule, TypeReference type)
    {
        if (type == null)
            return null;

        if (type is GenericParameter)
            return type;

        return targetModule.ImportReference(type);
    }

    private static TypeReference RewriteTypeReferenceForClone(ModuleDefinition targetModule, TypeReference type, IDictionary<GenericParameter, GenericParameter> genericParameterMap)
    {
        if (type == null)
            return null;

        if (type is GenericParameter gp)
        {
            GenericParameter mapped;
            if (genericParameterMap != null && genericParameterMap.TryGetValue(gp, out mapped))
                return mapped;

            return gp;
        }

        if (type is GenericInstanceType git)
        {
            var element = RewriteTypeReferenceForClone(targetModule, git.ElementType, genericParameterMap);
            var newGit = new GenericInstanceType(ImportType(targetModule, element));
            foreach (var ga in git.GenericArguments)
                newGit.GenericArguments.Add(RewriteTypeReferenceForClone(targetModule, ga, genericParameterMap));
            return newGit;
        }

        if (type is ArrayType at)
            return new ArrayType(RewriteTypeReferenceForClone(targetModule, at.ElementType, genericParameterMap), at.Rank);

        if (type is ByReferenceType br)
            return new ByReferenceType(RewriteTypeReferenceForClone(targetModule, br.ElementType, genericParameterMap));

        if (type is PointerType pt)
            return new PointerType(RewriteTypeReferenceForClone(targetModule, pt.ElementType, genericParameterMap));

        if (type is PinnedType pin)
            return new PinnedType(RewriteTypeReferenceForClone(targetModule, pin.ElementType, genericParameterMap));

        if (type is RequiredModifierType rmt)
            return new RequiredModifierType(ImportType(targetModule, rmt.ModifierType), RewriteTypeReferenceForClone(targetModule, rmt.ElementType, genericParameterMap));

        if (type is OptionalModifierType omt)
            return new OptionalModifierType(ImportType(targetModule, omt.ModifierType), RewriteTypeReferenceForClone(targetModule, omt.ElementType, genericParameterMap));

        if (type is SentinelType st)
            return new SentinelType(RewriteTypeReferenceForClone(targetModule, st.ElementType, genericParameterMap));

        if (type is FunctionPointerType)
            return type;

        return ImportType(targetModule, type);
    }

    public static void ReplaceType(this ModuleDefinition targetModule, string targetTypeName, ModuleDefinition replacementModule, string replacementTypeName)
    {
        var targetType = targetModule.GetType(targetTypeName);
        Debug.Assert(targetType != null, $"Could not find the type to replace: {targetTypeName}");

        var replacementType = replacementModule.GetType(replacementTypeName);
        Debug.Assert(replacementType != null, $"Could not find replacement type: {replacementTypeName}");

        // Clone the replacement type into the target module, but keep the original target full name.
        var typeMap = new Dictionary<string, TypeDefinition>(StringComparer.Ordinal);

        var clonedType = CloneTypeDefinition(targetModule, replacementType, typeMap, targetType.Namespace, targetType.Name);

        // Ensure nested types are also mapped for reference rewriting. We match by nested type full name under the type.
        foreach (var oldNested in targetType.NestedTypes)
        {
            if (typeMap.ContainsKey(oldNested.FullName))
                continue;

            var newNested = clonedType.NestedTypes.FirstOrDefault(nt => nt.Name == oldNested.Name);
            if (newNested != null)
                typeMap[oldNested.FullName] = newNested;
        }

        targetModule.Types.Add(clonedType);

        // Rewrite all references in the module.
        targetModule.ReplaceTypeReferences(typeMap);

        // Remove original type.
        targetModule.Types.Remove(targetType);
    }

    private static TypeDefinition CloneTypeDefinition(ModuleDefinition targetModule, TypeDefinition sourceType, Dictionary<string, TypeDefinition> typeMap, string forcedNamespace, string forcedName)
    {
        var clonedType = new TypeDefinition(forcedNamespace, forcedName, sourceType.Attributes);

        // Map before cloning members to allow self-references.
        typeMap[sourceType.FullName.Replace(sourceType.Namespace + "." + sourceType.Name, forcedNamespace + "." + forcedName)] = clonedType;

        if (sourceType.BaseType != null)
            clonedType.BaseType = targetModule.ImportReference(sourceType.BaseType);

        foreach (var iface in sourceType.Interfaces)
            clonedType.Interfaces.Add(new InterfaceImplementation(targetModule.ImportReference(iface.InterfaceType)));

        var typeGenericParameterMap = new Dictionary<GenericParameter, GenericParameter>();

        foreach (var genericParameter in sourceType.GenericParameters)
        {
            var gp = new GenericParameter(genericParameter.Name, clonedType);
            gp.Attributes = genericParameter.Attributes;
            clonedType.GenericParameters.Add(gp);
            typeGenericParameterMap[genericParameter] = gp;
        }

        foreach (var genericParameter in sourceType.GenericParameters)
        {
            var mappedGp = typeGenericParameterMap[genericParameter];
            foreach (var constraint in genericParameter.Constraints)
                mappedGp.Constraints.Add(new GenericParameterConstraint(RewriteTypeReferenceForClone(targetModule, constraint.ConstraintType, typeGenericParameterMap)));
        }

        CloneCustomAttributes(targetModule, sourceType.CustomAttributes, clonedType.CustomAttributes);

        // Nested types (recursively)
        foreach (var nested in sourceType.NestedTypes)
        {
            var clonedNested = CloneTypeDefinition(targetModule, nested, typeMap, string.Empty, nested.Name);
            clonedNested.DeclaringType = clonedType;
            clonedType.NestedTypes.Add(clonedNested);

            // Map the expected old nested full name to the new nested type.
            typeMap[clonedType.FullName + "/" + nested.Name] = clonedNested;
        }

        // Fields
        foreach (var field in sourceType.Fields)
        {
            var clonedField = new FieldDefinition(field.Name, field.Attributes, RewriteTypeReferenceForClone(targetModule, field.FieldType, typeGenericParameterMap));
            clonedField.Constant = field.Constant;
            clonedField.HasConstant = field.HasConstant;
            clonedField.InitialValue = field.InitialValue;
            clonedField.IsNotSerialized = field.IsNotSerialized;
            clonedField.IsSpecialName = field.IsSpecialName;
            clonedField.IsRuntimeSpecialName = field.IsRuntimeSpecialName;
            clonedField.Offset = field.Offset;

            CloneCustomAttributes(targetModule, field.CustomAttributes, clonedField.CustomAttributes);

            clonedType.Fields.Add(clonedField);
        }

        // Methods
        var methodMap = new Dictionary<MethodDefinition, MethodDefinition>();

        foreach (var method in sourceType.Methods)
        {
            var clonedMethod = new MethodDefinition(method.Name, method.Attributes, RewriteTypeReferenceForClone(targetModule, method.ReturnType, typeGenericParameterMap))
            {
                ImplAttributes = method.ImplAttributes,
                SemanticsAttributes = method.SemanticsAttributes,
                IsPInvokeImpl = method.IsPInvokeImpl,
                PInvokeInfo = method.PInvokeInfo,
                HasThis = method.HasThis,
                ExplicitThis = method.ExplicitThis,
                CallingConvention = method.CallingConvention
            };

            var methodGenericParameterMap = new Dictionary<GenericParameter, GenericParameter>(typeGenericParameterMap);

            foreach (var gpSrc in method.GenericParameters)
            {
                var gp = new GenericParameter(gpSrc.Name, clonedMethod);
                gp.Attributes = gpSrc.Attributes;
                clonedMethod.GenericParameters.Add(gp);
                methodGenericParameterMap[gpSrc] = gp;
            }

            foreach (var gpSrc in method.GenericParameters)
            {
                var mappedGp = methodGenericParameterMap[gpSrc];
                foreach (var constraint in gpSrc.Constraints)
                    mappedGp.Constraints.Add(new GenericParameterConstraint(RewriteTypeReferenceForClone(targetModule, constraint.ConstraintType, methodGenericParameterMap)));
            }

            foreach (var p in method.Parameters)
                clonedMethod.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, RewriteTypeReferenceForClone(targetModule, p.ParameterType, methodGenericParameterMap)));

            CloneCustomAttributes(targetModule, method.CustomAttributes, clonedMethod.CustomAttributes);

            clonedType.Methods.Add(clonedMethod);
            methodMap[method] = clonedMethod;
        }

        // Properties
        foreach (var property in sourceType.Properties)
        {
            var clonedProperty = new PropertyDefinition(property.Name, property.Attributes, RewriteTypeReferenceForClone(targetModule, property.PropertyType, typeGenericParameterMap));

            foreach (var p in property.Parameters)
                clonedProperty.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, RewriteTypeReferenceForClone(targetModule, p.ParameterType, typeGenericParameterMap)));

            CloneCustomAttributes(targetModule, property.CustomAttributes, clonedProperty.CustomAttributes);

            if (property.GetMethod != null)
                clonedProperty.GetMethod = methodMap[property.GetMethod];
            if (property.SetMethod != null)
                clonedProperty.SetMethod = methodMap[property.SetMethod];

            foreach (var other in property.OtherMethods)
                clonedProperty.OtherMethods.Add(methodMap[other]);

            clonedType.Properties.Add(clonedProperty);
        }

        // Events
        foreach (var evt in sourceType.Events)
        {
            var clonedEvent = new EventDefinition(evt.Name, evt.Attributes, RewriteTypeReferenceForClone(targetModule, evt.EventType, typeGenericParameterMap));

            CloneCustomAttributes(targetModule, evt.CustomAttributes, clonedEvent.CustomAttributes);

            if (evt.AddMethod != null)
                clonedEvent.AddMethod = methodMap[evt.AddMethod];
            if (evt.RemoveMethod != null)
                clonedEvent.RemoveMethod = methodMap[evt.RemoveMethod];
            if (evt.InvokeMethod != null)
                clonedEvent.InvokeMethod = methodMap[evt.InvokeMethod];

            foreach (var other in evt.OtherMethods)
                clonedEvent.OtherMethods.Add(methodMap[other]);

            clonedType.Events.Add(clonedEvent);
        }

        // Method bodies
        foreach (var kv in methodMap)
        {
            var sourceMethod = kv.Key;
            var clonedMethod = kv.Value;

            if (!sourceMethod.HasBody)
                continue;

            clonedMethod.Body = new Mono.Cecil.Cil.MethodBody(clonedMethod)
            {
                InitLocals = sourceMethod.Body.InitLocals,
                MaxStackSize = sourceMethod.Body.MaxStackSize
            };

            // Variables
            foreach (var v in sourceMethod.Body.Variables)
                clonedMethod.Body.Variables.Add(new VariableDefinition(RewriteTypeReferenceForClone(targetModule, v.VariableType, typeGenericParameterMap)));

            // Instructions (2-pass for branch targets)
            var il = clonedMethod.Body.GetILProcessor();
            var instMap = new Dictionary<Instruction, Instruction>();

            foreach (var inst in sourceMethod.Body.Instructions)
            {
                var clonedInst = CloneInstructionWithoutTargets(targetModule, inst);
                il.Append(clonedInst);
                instMap[inst] = clonedInst;
            }

            foreach (var inst in sourceMethod.Body.Instructions)
            {
                var clonedInst = instMap[inst];

                if (inst.Operand is Instruction target)
                {
                    clonedInst.Operand = instMap[target];
                }
                else if (inst.Operand is Instruction[] targets)
                {
                    var newTargets = new Instruction[targets.Length];
                    for (var i = 0; i < targets.Length; i++)
                        newTargets[i] = instMap[targets[i]];
                    clonedInst.Operand = newTargets;
                }
            }

            // Exception handlers
            foreach (var eh in sourceMethod.Body.ExceptionHandlers)
            {
                var clonedEh = new ExceptionHandler(eh.HandlerType)
                {
                    CatchType = eh.CatchType != null ? targetModule.ImportReference(eh.CatchType) : null,
                    TryStart = instMap[eh.TryStart],
                    TryEnd = eh.TryEnd != null ? instMap[eh.TryEnd] : null,
                    HandlerStart = instMap[eh.HandlerStart],
                    HandlerEnd = eh.HandlerEnd != null ? instMap[eh.HandlerEnd] : null,
                    FilterStart = eh.FilterStart != null ? instMap[eh.FilterStart] : null
                };
                clonedMethod.Body.ExceptionHandlers.Add(clonedEh);
            }

            // Debug info / sequence points intentionally not copied.
        }

        return clonedType;
    }

    private static Instruction CloneInstructionWithoutTargets(ModuleDefinition targetModule, Instruction inst)
    {
        var op = inst.OpCode;
        if (inst.Operand == null)
            return Instruction.Create(op);

        var operand = inst.Operand;

        if (operand is TypeReference tr)
        {
            if (tr is GenericParameter)
                return Instruction.Create(op, tr);

            return Instruction.Create(op, ImportType(targetModule, tr));
        }

        if (operand is MethodReference mr)
            return Instruction.Create(op, mr);

        if (operand is FieldReference fr)
            return Instruction.Create(op, fr);

        // CallSite is not supported by ModuleDefinition.ImportReference in this codebase's Cecil version.
        // If needed later, add explicit CallSite cloning.

        if (operand is string s)
            return Instruction.Create(op, s);

        if (operand is sbyte sb)
            return Instruction.Create(op, sb);
        if (operand is byte b)
            return Instruction.Create(op, b);
        if (operand is int i32)
            return Instruction.Create(op, i32);
        if (operand is long i64)
            return Instruction.Create(op, i64);
        if (operand is float f)
            return Instruction.Create(op, f);
        if (operand is double d)
            return Instruction.Create(op, d);

        if (operand is Instruction)
            return Instruction.Create(op, Instruction.Create(OpCodes.Nop));

        if (operand is Instruction[])
            return Instruction.Create(op, new Instruction[0]);

        if (operand is ParameterDefinition pd)
            return Instruction.Create(op, pd);

        if (operand is VariableDefinition vd)
            return Instruction.Create(op, vd);

        // Fallback - try to keep operand as-is.
        return Instruction.Create(op);
    }

    private static void ReplaceTypeReferences(this ModuleDefinition module, Dictionary<string, TypeDefinition> typeMap)
    {
        foreach (var type in module.GetTypes())
        {
            if (type.BaseType != null)
                type.BaseType = RewriteTypeReference(module, type.BaseType, typeMap);

            for (var i = 0; i < type.Interfaces.Count; i++)
                type.Interfaces[i] = new InterfaceImplementation(RewriteTypeReference(module, type.Interfaces[i].InterfaceType, typeMap));

            foreach (var field in type.Fields)
                field.FieldType = RewriteTypeReference(module, field.FieldType, typeMap);

            foreach (var property in type.Properties)
                property.PropertyType = RewriteTypeReference(module, property.PropertyType, typeMap);

            foreach (var evt in type.Events)
                evt.EventType = RewriteTypeReference(module, evt.EventType, typeMap);

            foreach (var method in type.Methods)
            {
                method.ReturnType = RewriteTypeReference(module, method.ReturnType, typeMap);

                foreach (var p in method.Parameters)
                    p.ParameterType = RewriteTypeReference(module, p.ParameterType, typeMap);

                foreach (var gp in method.GenericParameters)
                    for (var c = 0; c < gp.Constraints.Count; c++)
                        gp.Constraints[c] = new GenericParameterConstraint(RewriteTypeReference(module, gp.Constraints[c].ConstraintType, typeMap));

                if (!method.HasBody)
                    continue;

                foreach (var v in method.Body.Variables)
                    v.VariableType = RewriteTypeReference(module, v.VariableType, typeMap);

                var instructions = method.Body.Instructions;

                for (var i = 0; i < instructions.Count; i++)
                {
                    var inst = instructions[i];
                    if (inst.Operand is TypeReference operandType)
                    {
                        inst.Operand = RewriteTypeReference(module, operandType, typeMap);
                    }
                    else if (inst.Operand is MethodReference operandMethod)
                    {
                        inst.Operand = RewriteMethodReference(module, operandMethod, typeMap);
                    }
                    else if (inst.Operand is FieldReference operandField)
                    {
                        inst.Operand = RewriteFieldReference(module, operandField, typeMap);
                    }
                }

                RewriteCustomAttributes(module, method.CustomAttributes, typeMap);
            }

            RewriteCustomAttributes(module, type.CustomAttributes, typeMap);
            foreach (var field in type.Fields)
                RewriteCustomAttributes(module, field.CustomAttributes, typeMap);
            foreach (var property in type.Properties)
                RewriteCustomAttributes(module, property.CustomAttributes, typeMap);
            foreach (var evt in type.Events)
                RewriteCustomAttributes(module, evt.CustomAttributes, typeMap);
        }

        RewriteCustomAttributes(module, module.Assembly.CustomAttributes, typeMap);
    }

    private static TypeReference RewriteTypeReference(ModuleDefinition module, TypeReference type, Dictionary<string, TypeDefinition> typeMap)
    {
        if (type == null)
            return null;

        // Resolve TypeSpecs first (generic instance, arrays, byref, pointers, etc.)
        if (type is GenericInstanceType git)
        {
            var element = RewriteTypeReference(module, git.ElementType, typeMap);
            var newGit = new GenericInstanceType(element);
            foreach (var ga in git.GenericArguments)
                newGit.GenericArguments.Add(RewriteTypeReference(module, ga, typeMap));
            return module.ImportReference(newGit);
        }

        if (type is ArrayType at)
            return new ArrayType(RewriteTypeReference(module, at.ElementType, typeMap), at.Rank);

        if (type is ByReferenceType br)
            return new ByReferenceType(RewriteTypeReference(module, br.ElementType, typeMap));

        if (type is PointerType pt)
            return new PointerType(RewriteTypeReference(module, pt.ElementType, typeMap));

        if (type is PinnedType pin)
            return new PinnedType(RewriteTypeReference(module, pin.ElementType, typeMap));

        if (type is RequiredModifierType rmt)
            return new RequiredModifierType(module.ImportReference(rmt.ModifierType), RewriteTypeReference(module, rmt.ElementType, typeMap));

        if (type is OptionalModifierType omt)
            return new OptionalModifierType(module.ImportReference(omt.ModifierType), RewriteTypeReference(module, omt.ElementType, typeMap));

        if (type is SentinelType st)
            return new SentinelType(RewriteTypeReference(module, st.ElementType, typeMap));

        if (type is FunctionPointerType)
            return type;

        // Direct mapping by full name (covers nested types with /)
        TypeDefinition mapped;
        if (typeMap.TryGetValue(type.FullName, out mapped))
            return module.ImportReference(mapped);

        return module.ImportReference(type);
    }

    private static MethodReference RewriteMethodReference(ModuleDefinition module, MethodReference method, Dictionary<string, TypeDefinition> typeMap)
    {
        var declaringType = RewriteTypeReference(module, method.DeclaringType, typeMap);

        if (method is GenericInstanceMethod gim)
        {
            var element = RewriteMethodReference(module, gim.ElementMethod, typeMap);
            var newGim = new GenericInstanceMethod(element);
            foreach (var ga in gim.GenericArguments)
                newGim.GenericArguments.Add(RewriteTypeReference(module, ga, typeMap));
            return module.ImportReference(newGim);
        }

        var rewritten = new MethodReference(method.Name, RewriteTypeReference(module, method.ReturnType, typeMap), declaringType)
        {
            HasThis = method.HasThis,
            ExplicitThis = method.ExplicitThis,
            CallingConvention = method.CallingConvention
        };

        foreach (var p in method.Parameters)
            rewritten.Parameters.Add(new ParameterDefinition(RewriteTypeReference(module, p.ParameterType, typeMap)));

        foreach (var gp in method.GenericParameters)
            rewritten.GenericParameters.Add(new GenericParameter(gp.Name, rewritten));

        return module.ImportReference(rewritten);
    }

    private static FieldReference RewriteFieldReference(ModuleDefinition module, FieldReference field, Dictionary<string, TypeDefinition> typeMap)
    {
        var declaringType = RewriteTypeReference(module, field.DeclaringType, typeMap);
        var rewritten = new FieldReference(field.Name, RewriteTypeReference(module, field.FieldType, typeMap), declaringType);
        return module.ImportReference(rewritten);
    }

    private static void CloneCustomAttributes(ModuleDefinition module, ICollection<CustomAttribute> source, ICollection<CustomAttribute> target)
    {
        foreach (var ca in source)
        {
            var ctor = module.ImportReference(ca.Constructor);
            var newAttr = new CustomAttribute(ctor);

            foreach (var arg in ca.ConstructorArguments)
                newAttr.ConstructorArguments.Add(CloneCustomAttributeArgument(module, arg));

            foreach (var na in ca.Fields)
                newAttr.Fields.Add(new Mono.Cecil.CustomAttributeNamedArgument(na.Name, CloneCustomAttributeArgument(module, na.Argument)));

            foreach (var na in ca.Properties)
                newAttr.Properties.Add(new Mono.Cecil.CustomAttributeNamedArgument(na.Name, CloneCustomAttributeArgument(module, na.Argument)));

            target.Add(newAttr);
        }
    }

    private static void RewriteCustomAttributes(ModuleDefinition module, ICollection<CustomAttribute> attributes, Dictionary<string, TypeDefinition> typeMap)
    {
        foreach (var ca in attributes)
        {
            for (var i = 0; i < ca.ConstructorArguments.Count; i++)
                ca.ConstructorArguments[i] = RewriteCustomAttributeArgument(module, ca.ConstructorArguments[i], typeMap);

            for (var i = 0; i < ca.Fields.Count; i++)
            {
                var item = ca.Fields[i];
                ca.Fields[i] = new Mono.Cecil.CustomAttributeNamedArgument(item.Name, RewriteCustomAttributeArgument(module, item.Argument, typeMap));
            }

            for (var i = 0; i < ca.Properties.Count; i++)
            {
                var item = ca.Properties[i];
                ca.Properties[i] = new Mono.Cecil.CustomAttributeNamedArgument(item.Name, RewriteCustomAttributeArgument(module, item.Argument, typeMap));
            }
        }
    }

    private static CustomAttributeArgument CloneCustomAttributeArgument(ModuleDefinition module, CustomAttributeArgument arg)
    {
        var type = module.ImportReference(arg.Type);
        var value = arg.Value;

        if (value is TypeReference tr)
            value = module.ImportReference(tr);

        if (value is CustomAttributeArgument[] arr)
        {
            var newArr = new CustomAttributeArgument[arr.Length];
            for (var i = 0; i < arr.Length; i++)
                newArr[i] = CloneCustomAttributeArgument(module, arr[i]);
            value = newArr;
        }

        return new CustomAttributeArgument(type, value);
    }

    private static CustomAttributeArgument RewriteCustomAttributeArgument(ModuleDefinition module, CustomAttributeArgument arg, Dictionary<string, TypeDefinition> typeMap)
    {
        var type = RewriteTypeReference(module, arg.Type, typeMap);
        var value = arg.Value;

        if (value is TypeReference tr)
            value = RewriteTypeReference(module, tr, typeMap);

        if (value is CustomAttributeArgument[] arr)
        {
            var newArr = new CustomAttributeArgument[arr.Length];
            for (var i = 0; i < arr.Length; i++)
                newArr[i] = RewriteCustomAttributeArgument(module, arr[i], typeMap);
            value = newArr;
        }

        return new CustomAttributeArgument(type, value);
    }
}
