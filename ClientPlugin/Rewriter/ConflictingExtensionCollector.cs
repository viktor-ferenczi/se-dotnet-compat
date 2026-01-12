using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;


namespace ClientPlugin.Rewriter;


internal sealed class ConflictingExtensionCollector(SemanticModel _semanticModel) : CSharpSyntaxWalker
{
    public readonly HashSet<IMethodSymbol> Conflicts = [];  

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        if (symbol is not { IsExtensionMethod: true })
            return;

        var extendedType = symbol.Parameters[0].Type as INamedTypeSymbol;
        if (extendedType == null)
            return;

        var conflict =
            extendedType.GetMembers(symbol.Name)
                        .OfType<IMethodSymbol>()
                        .Any(m => m.IsStatic);

        if (conflict)
            Conflicts.Add(symbol);
    }
}
