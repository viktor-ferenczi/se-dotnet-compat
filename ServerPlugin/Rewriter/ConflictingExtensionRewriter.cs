using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ServerPlugin.Rewriter;

internal class ConflictingExtensionRewriter(SemanticModel _semanticModel, Dictionary<IMethodSymbol, List<IMethodSymbol>> _conflicts) : CSharpSyntaxRewriter
{
    // FIXME: Move VisitUsingDirective to another class
    // It's been put here for now so we have access to _semanticModel (trees change after each write pass)

    public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
    {
        // Remove broken usings
        // FIXME: This fixes it but why are they even here? Whitelist issues? Types moving?
        if (_semanticModel.GetSymbolInfo(node.Name!).Symbol is null)
            return null;

        return base.VisitUsingDirective(node);
    }

    // FIXME: Either move this to another class or rename ConflictingExtensionRewriter
    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {

        var info = _semanticModel.GetSymbolInfo(node);

        // Special Case:
        // 'Vector3' is an ambiguous reference between 'VRageMath.Vector3' and 'System.Numerics.Vector3'
        // FIXME: Investigate why this happens only on core. 'System.Numerics.Vector3' is present on framework too.

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
            // Special Case:
            // Net Framework Has Extension: FirstOrDefault<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
            // Net Core Added Extension: FirstOrDefault<TSource>(IEnumerable<TSource> source, TSource defaultValue)
            // Calling IEnumerable<TSource>.FirstOrDefault(null) is now ambiguous so we must explicitly call the Framework one
            // Whilst this is still invalid code at runtime, some mods do it anyway (either a bug or due to flow control with exceptions)

            if (info.CandidateReason == CandidateReason.OverloadResolutionFailure)
            {
                // We only care about information that is shared between all possible enumerable candidates so pick the first
                // We assume this will ONLY fire in this special case so we know some information beforehand
                var enumerableMethod = info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == "FirstOrDefault" && x.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Linq.Enumerable");
                if (enumerableMethod is not null)
                {
                    if (node.ArgumentList.Arguments.Count == 1 && node.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.NullLiteralExpression))
                    {
                        var tSource = enumerableMethod.TypeArguments[0];

                        var funcType =
                            _semanticModel.Compilation.GetTypeByMetadataName("System.Func`2")!
                                .Construct(tSource, _semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean));

                        // The cast allows the compiler to pick the correct method
                        var castNull = SyntaxFactory.CastExpression(
                            SyntaxFactory.ParseTypeName(funcType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

                        return node.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(castNull))));
                    }
                }
            }

            return base.VisitInvocationExpression(node);
        }

        // Lookup conflicting extensions as normal
        // FIXME: Might not work for null coallessing access
        var method = symbol.ReducedFrom ?? symbol.OriginalDefinition;

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
