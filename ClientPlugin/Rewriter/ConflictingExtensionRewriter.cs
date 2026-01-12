using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace ClientPlugin.Rewriter;

internal class ConflictingExtensionRewriter(SemanticModel _semanticModel, Dictionary<IMethodSymbol, List<IMethodSymbol>> _conflicts) : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is not MemberAccessExpressionSyntax member)
            return base.VisitInvocationExpression(node);

        var symbol = _semanticModel.GetSymbolInfo(node).Symbol as IMethodSymbol;
        if (symbol == null)
            return base.VisitInvocationExpression(node);

        // FIXME: Might not work for null coallessing access
        var method = symbol.ReducedFrom ?? symbol.OriginalDefinition;

        //if (method.ToString().Contains("Round"))
        //    Debugger.Break();

        if (!_conflicts.TryGetValue(method, out var possibleExtensions))
            return base.VisitInvocationExpression(node);

        foreach (var possibleExtension in possibleExtensions)
        {
            if (!IsVisible(_semanticModel, node.SpanStart, possibleExtension))
                continue;

            // x.Round(a,b) → global::Full.Type.Name.Of.ExtensionClass.Round(x,a,b)
            var args = node.ArgumentList.Arguments.Insert(
                0, SyntaxFactory.Argument(member.Expression));

            var typeName = SyntaxFactory.ParseName(
                possibleExtension.ContainingType.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat));

            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    typeName,
                    SyntaxFactory.IdentifierName(method.Name)),
                SyntaxFactory.ArgumentList(args));
        }

        return base.VisitInvocationExpression(node);
    }

    private static bool IsVisible(
        SemanticModel model,
        int position,
        IMethodSymbol extension)
    {
        // method itself
        if (!model.IsAccessible(position, extension))
            return false;

        // containing static class
        if (!model.IsAccessible(position, extension.ContainingType))
            return false;

        // namespace / type in scope
        return model
            .LookupNamespacesAndTypes(position)
            .OfType<INamedTypeSymbol>()
            .Any(t =>
                t.Name == extension.ContainingType.Name &&
                t.ContainingNamespace.ToDisplayString() ==
                extension.ContainingType.ContainingNamespace.ToDisplayString());
    }
}
