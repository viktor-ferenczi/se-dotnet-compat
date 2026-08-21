using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VRage.Scripting;

#if MAGNETAR
namespace ServerPlugin.Rewriter;

#else
namespace ClientPlugin.Rewriter;

#endif

internal static class CompilationRewriter
{
    public static CSharpCompilation Rewrite(CSharpCompilation compilation, MyApiTarget target)
    {
        if (
            target != MyApiTarget.Mod
            || !compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)
        )
            return compilation;

        Dictionary<IMethodSymbol, List<IMethodSymbol>> conflicts = [];

        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            new ConflictingExtensionCollector(model, conflicts).Visit(tree.GetRoot());
        }

        var replacements = new List<(SyntaxTree OldTree, SyntaxTree NewTree)>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            var model = compilation.GetSemanticModel(tree);
            var rewrittenRoot = new ConflictingExtensionRewriter(model, conflicts).Visit(root);
            if (ReferenceEquals(root, rewrittenRoot))
                continue;

            replacements.Add((tree, tree.WithRootAndOptions(rewrittenRoot, tree.Options)));
        }

        foreach (var (oldTree, newTree) in replacements)
            compilation = compilation.ReplaceSyntaxTree(oldTree, newTree);

        return compilation;
    }
}
