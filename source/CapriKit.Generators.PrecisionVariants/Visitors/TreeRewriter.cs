using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

/// <summary>
/// Allows ITreeRewriters to rewrite nodes in the tree. This is usually done after adding annotations
/// with rewrite instructions to the tree/syntax nodes, using the TreeAnnotator. As rewriters do not have access
/// to any other information than the syntax node themselves.
/// </summary>
internal sealed class TreeRewriter
{
    public static SyntaxNode Rewrite(SyntaxNode tree, params IReadOnlyList<ITreeRewriter> rewriters)
    {
        var rewrittenNode = tree;
        foreach (var rewriteRule in rewriters)
        {
            var treeRewriter = new InternalTreeWriter(rewriteRule);
            rewrittenNode = treeRewriter.Visit(rewrittenNode);
        }

        return rewrittenNode;
    }

    private sealed class InternalTreeWriter(ITreeRewriter Rewriter)
    {
        public SyntaxNode Visit(SyntaxNode originalNode)
        {
            var rewrittenNode = originalNode
                .ReplaceNodes(originalNode.ChildNodes(), (originalChild, _) => Visit(originalChild));

            return Rewriter.Rewrite(rewrittenNode);
        }
    }
}
