using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ClientPlugin.Tools;

// Useful methods for preloader patches using Mono.Cecil.
// For usage examples, see the various *Prepatch.cs files.
public static class PreloaderHelpers
{
    public static string Hash(this IEnumerable<Instruction> instructions, MethodDefinition method)
    {
        return instructions.HashInstructions(method).CombineHashCodes().ToString("x8");
    }

    private static string FormatCode(this IEnumerable<Instruction> instructions, MethodDefinition method)
    {
        var sb = new StringBuilder();

        var instructionsList = instructions.ToList();
        var hash = instructionsList.Hash(method);
        sb.Append($"// {hash}\r\n");

        foreach (var instr in instructionsList)
        {
            sb.Append(instr.ToCodeLine());
            sb.Append("\r\n");
        }

        return sb.ToString();
    }

    private static string ToCodeLine(this Instruction instr)
    {
        var sb = new StringBuilder();

        sb.Append(instr.OpCode);

        var arg = FormatOperand(instr.Operand);
        if (arg.Length > 0)
        {
            sb.Append(' ');
            sb.Append(arg);
        }

        return sb.ToString();
    }

    private static string FormatOperand(object operand)
    {
        switch (operand)
        {
            case null:
                return "";

            case MethodReference methodRef:
                return FormatMethodReference(methodRef);

            case TypeReference typeRef:
                return FormatTypeReference(typeRef);

            case FieldReference fieldRef:
                return FormatFieldReference(fieldRef);

            case Instruction targetInstr:
                return $"IL_{targetInstr.Offset:x4}";

            case Instruction[] instructions:
                return string.Join(", ", instructions.Select(i => $"IL_{i.Offset:x4}"));

            case VariableDefinition varDef:
                return $"{varDef.Index} ({FormatTypeReference(varDef.VariableType)})";

            case ParameterDefinition paramDef:
                return $"{paramDef.Name} ({FormatTypeReference(paramDef.ParameterType)})";

            case string s:
                return s.ToLiteral();

            case float f:
                return f.ToString(CultureInfo.InvariantCulture);

            case double d:
                return d.ToString(CultureInfo.InvariantCulture);

            case int i:
                return i.ToString(CultureInfo.InvariantCulture);

            case long l:
                return l.ToString(CultureInfo.InvariantCulture);

            case sbyte sb:
                return sb.ToString(CultureInfo.InvariantCulture);

            case byte b:
                return b.ToString(CultureInfo.InvariantCulture);

            default:
                return operand.ToString()?.Trim() ?? "null";
        }
    }

    private static string FormatMethodReference(MethodReference methodRef)
    {
        var sb = new StringBuilder();

        // Return type
        sb.Append(FormatTypeReference(methodRef.ReturnType));
        sb.Append(' ');

        // Declaring type
        if (methodRef.DeclaringType != null)
        {
            sb.Append(FormatTypeReference(methodRef.DeclaringType));
            sb.Append("::");
        }

        // Method name
        sb.Append(methodRef.Name);

        // Generic parameters
        if (methodRef is GenericInstanceMethod genericMethod && genericMethod.GenericArguments.Count > 0)
        {
            sb.Append('<');
            sb.Append(string.Join(", ", genericMethod.GenericArguments.Select(FormatTypeReference)));
            sb.Append('>');
        }

        // Parameters
        sb.Append('(');
        if (methodRef.HasParameters)
        {
            sb.Append(string.Join(", ", methodRef.Parameters.Select(p => FormatTypeReference(p.ParameterType))));
        }
        sb.Append(')');

        return sb.ToString();
    }

    private static string FormatTypeReference(TypeReference typeRef)
    {
        if (typeRef == null)
            return "void";

        // Handle generic instances
        if (typeRef is GenericInstanceType genericType)
        {
            var baseName = genericType.ElementType.Name;
            // Remove `1, `2 etc from generic type names
            var tickIndex = baseName.IndexOf('`');
            if (tickIndex > 0)
                baseName = baseName.Substring(0, tickIndex);

            return $"{genericType.Namespace}.{baseName}<{string.Join(", ", genericType.GenericArguments.Select(FormatTypeReference))}>";
        }

        // Handle arrays
        if (typeRef is ArrayType arrayType)
        {
            return FormatTypeReference(arrayType.ElementType) + "[]";
        }

        // Handle by reference
        if (typeRef is ByReferenceType byRefType)
        {
            return FormatTypeReference(byRefType.ElementType) + "&";
        }

        // Handle pointers
        if (typeRef is PointerType pointerType)
        {
            return FormatTypeReference(pointerType.ElementType) + "*";
        }

        // Simple type
        if (!string.IsNullOrEmpty(typeRef.Namespace))
            return $"{typeRef.Namespace}.{typeRef.Name}";

        return typeRef.Name;
    }

    private static string FormatFieldReference(FieldReference fieldRef)
    {
        var sb = new StringBuilder();
        sb.Append(FormatTypeReference(fieldRef.FieldType));
        sb.Append(' ');
        if (fieldRef.DeclaringType != null)
        {
            sb.Append(FormatTypeReference(fieldRef.DeclaringType));
            sb.Append("::");
        }
        sb.Append(fieldRef.Name);
        return sb.ToString();
    }

    public static void RecordOriginalCode(this IEnumerable<Instruction> instructions, MethodDefinition method, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "")
    {
        RecordCode(instructions, method, callerFilePath, callerMemberName, "original");
    }

    public static void RecordPatchedCode(this IEnumerable<Instruction> instructions, MethodDefinition method, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "")
    {
        RecordCode(instructions, method, callerFilePath, callerMemberName, "patched");
    }

    public static void RecordCustomCode(this IEnumerable<Instruction> instructions, MethodDefinition method, string suffix, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "")
    {
        RecordCode(instructions, method, callerFilePath, callerMemberName, suffix);
    }

    private static void RecordCode(IEnumerable<Instruction> instructions, MethodDefinition method, string callerFilePath, string callerMemberName, string suffix)
    {
#if DEBUG
        Debug.Assert(callerFilePath.Length > 0);

        if (!File.Exists(callerFilePath))
            return;

        var dir = Path.GetDirectoryName(callerFilePath);
        if (dir == null)
            return;

        var name = method == null
            ? callerMemberName.EndsWith("Prepatch")
                ? callerMemberName.Substring(0, callerMemberName.Length - "Prepatch".Length)
                : callerMemberName
            : method.Name.Replace(".ctor", "Constructor").Replace(".cctor", "StaticConstructor");

        var path = Path.Combine(dir, $"{name}.{suffix}.il");

        var text = instructions.FormatCode(method);

        if (File.Exists(path) && File.ReadAllText(path) == text)
            return;

        File.WriteAllText(path, text);
#endif
    }

    // Note: The following helper methods from TranspilerHelpers cannot be directly ported to Cecil
    // because Cecil's Instruction collection works differently from Harmony's CodeInstruction list.
    // Cecil instructions are in a linked list structure and don't support the same query operations.
    // If needed, these would need to be reimplemented with different logic specific to each use case.

    // - FindAllIndex (Cecil uses linked list, not indexed list)
    // - GetField (would need to iterate through Collection<Instruction>)
    // - FindPropertyGetter (would need to iterate through Collection<Instruction>)
    // - FindPropertySetter (would need to iterate through Collection<Instruction>)
    // - GetLabel (Cecil uses Instruction references instead of Label structs)
    // - RemoveFieldInitialization (Cecil has different remove semantics)
    // - VerifyCodeHash (can be added if needed)
    // - DeepClone (Cecil has its own cloning mechanism)
}
