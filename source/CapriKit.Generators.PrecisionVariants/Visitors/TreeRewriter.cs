using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

internal sealed class TreeRewriter
{
    public static SyntaxNode? Rewrite(SyntaxNode? tree, params IReadOnlyList<ITreeRewriter> rewriters)
    {
        if (tree == null)
        {
            return null;
        }

        var rewrittenNode = tree;
        foreach (var rewriteRule in rewriters)
        {
            var treeRewriter = new InternalTreeWriter(rewriteRule);
            rewrittenNode = treeRewriter.Visit(rewrittenNode);
        }

        return rewrittenNode;
    }

    private sealed class InternalTreeWriter(ITreeRewriter Rewriter) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? Visit(SyntaxNode? originalNode)
        {
            if (originalNode == null)
            {
                return null;
            }

            var rewrittenNode = base.Visit(originalNode);
            if (rewrittenNode == null)
            {
                return null;
            }

            rewrittenNode = Rewriter.Execute(rewrittenNode);

            return rewrittenNode;
        }
    }
}
