using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClientPlugin.Rewriter;

internal sealed class RemoveUsingRewriter : CSharpSyntaxRewriter
{
    internal readonly string[] blacklist = ["System.Deployment", "System.Runtime.Remoting.Lifetime", "System.Runtime.Remoting.Messaging", "Microsoft.VisualBasic"];

    public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
    {
        // FIXME: Try and do this without a hardcoded blacklist
        foreach (var item in blacklist)
            if (node.Name?.ToString().Contains(item) ?? false)
                return null;

        return base.VisitUsingDirective(node);
    }
}
