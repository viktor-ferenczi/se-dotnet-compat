using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;


namespace ClientPlugin.Rewriter;


internal sealed class ConflictingExtensionCollector(SemanticModel _semanticModel, Dictionary<IMethodSymbol, List<IMethodSymbol>> conflicts) : CSharpSyntaxWalker
{
    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol is not { IsExtensionMethod: true })
            return;

        var extendedType = symbol.Parameters[0].Type as INamedTypeSymbol;
        if (extendedType == null)
            return;

        // FIXME: Use full method signature with types in this
        var conflict =
            extendedType.GetMembers(symbol.Name)
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(m => m.IsStatic);

        if (conflict is null)
            return;

        if (conflicts.TryGetValue(conflict, out var possibleExtensions))
            possibleExtensions.Add(symbol);
        else
            conflicts.Add(conflict, [symbol]);
    }
}
