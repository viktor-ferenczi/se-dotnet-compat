using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

#if MAGNETAR
namespace ServerPlugin.Rewriter;
#else
namespace ClientPlugin.Rewriter;
#endif

internal class ConflictingExtensionRewriter(SemanticModel _semanticModel, Dictionary<IMethodSymbol, List<IMethodSymbol>> _conflicts) : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
    {
        if (_semanticModel.GetSymbolInfo(node.Name!).Symbol is null)
            return null;

        return base.VisitUsingDirective(node);
    }

    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {

        var info = _semanticModel.GetSymbolInfo(node);

        // Mods written for .NET Framework expect VRageMath.Vector3 here.
        if (info.Symbol is null && info.CandidateSymbols.Length > 1)
        {
            ITypeSymbol[] candidates = [.. info.CandidateSymbols.OfType<ITypeSymbol>()];
            bool hasNumerics = candidates.Any(s => s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Numerics.Vector3");
            ITypeSymbol[] remaining = [.. candidates.Where(s => s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != "global::System.Numerics.Vector3")];

            if (remaining.Length == 1)
            {
                var replacement = SyntaxFactory.ParseName(remaining[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                return replacement.WithTriviaFrom(node);
            }
        }

        return base.VisitIdentifierName(node);
    }

    public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is not MemberAccessExpressionSyntax member)
            return base.VisitInvocationExpression(node);

        var info = _semanticModel.GetSymbolInfo(node);
        if (info.Symbol is not IMethodSymbol symbol)
        {
            // The newer FirstOrDefault(defaultValue) overload makes a null argument ambiguous.
            if (info.CandidateReason == CandidateReason.OverloadResolutionFailure)
            {
                var enumerableMethod = info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == "FirstOrDefault" && x.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Linq.Enumerable");
                if (enumerableMethod is not null)
                {
                    if (node.ArgumentList.Arguments.Count == 1 && node.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.NullLiteralExpression))
                    {
                        var tSource = enumerableMethod.TypeArguments[0];

                        var funcType =
                            _semanticModel.Compilation.GetTypeByMetadataName("System.Func`2")!
                                .Construct(tSource, _semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean));

                        var castNull = SyntaxFactory.CastExpression(
                            SyntaxFactory.ParseTypeName(funcType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

                        return node.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(castNull))));
                    }
                }
            }

            return base.VisitInvocationExpression(node);
        }

        var method = symbol.ReducedFrom ?? symbol.OriginalDefinition;

        if (!_conflicts.TryGetValue(method, out var possibleExtensions))
            return base.VisitInvocationExpression(node);

        foreach (var possibleExtension in possibleExtensions)
        {
            if (!IsVisible(_semanticModel, node.SpanStart, possibleExtension))
                continue;

            // x.Round(a,b) => global::Full.Type.Name.Of.ExtensionClass.Round(x,a,b)
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
        if (!model.IsAccessible(position, extension))
            return false;

        if (!model.IsAccessible(position, extension.ContainingType))
            return false;

        return model
            .LookupNamespacesAndTypes(position)
            .OfType<INamedTypeSymbol>()
            .Any(t =>
                t.Name == extension.ContainingType.Name &&
                t.ContainingNamespace.ToDisplayString() ==
                extension.ContainingType.ContainingNamespace.ToDisplayString());
    }
}
