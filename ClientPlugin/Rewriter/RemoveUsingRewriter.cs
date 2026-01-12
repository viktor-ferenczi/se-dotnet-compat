using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClientPlugin.Rewriter;

internal sealed class RemoveUsingRewriter : CSharpSyntaxRewriter
{
    internal readonly string[] blacklist = ["System.Deployment"];

    public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
    {
        foreach (var item in blacklist)
            if (node.Name?.ToString().Contains(item) ?? false)
                return null;

        return base.VisitUsingDirective(node);
    }
}
