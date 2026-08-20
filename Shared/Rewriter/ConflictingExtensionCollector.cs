using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#if MAGNETAR
namespace ServerPlugin.Rewriter;

#else
namespace ClientPlugin.Rewriter;

#endif

internal sealed class ConflictingExtensionCollector(
    SemanticModel _semanticModel,
    Dictionary<IMethodSymbol, List<IMethodSymbol>> conflicts
) : CSharpSyntaxWalker
{
    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol is not { IsExtensionMethod: true })
            return;

        var extendedType = symbol.Parameters[0].Type as INamedTypeSymbol;
        if (extendedType == null)
            return;

        var conflict = extendedType
            .GetMembers(symbol.Name)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.IsStatic && IsValidOverload(m, symbol));

        if (conflict is null)
            return;

        if (conflicts.TryGetValue(conflict, out var possibleExtensions))
            possibleExtensions.Add(symbol);
        else
            conflicts.Add(conflict, [symbol]);
    }

    private bool IsValidOverload(IMethodSymbol instanceMethod, IMethodSymbol extension)
    {
        // Extra parameters are allowed only when they are optional.

        var instanceParams = instanceMethod.Parameters;
        var extensionParams = extension.Parameters.Skip(1).ToArray();

        for (int i = 0; i < Math.Max(instanceParams.Length, extensionParams.Length); i++)
        {
            if (i >= instanceParams.Length && !extensionParams[i].IsOptional)
                return false;

            if (i >= extensionParams.Length && !instanceParams[i].IsOptional)
                return false;

            var extType = extensionParams[i].Type;
            var instanceType = instanceParams[i].Type;

            var conversion = _semanticModel.Compilation.ClassifyConversion(extType, instanceType);

            if (!conversion.IsImplicit)
                return false;
        }

        return true;
    }
}
