using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

internal sealed class TreeAnnotator
{
    public static SyntaxNode? Annotate(SyntaxNode? tree, SemanticModel semanticModel, params IReadOnlyList<ITreeAnnotator> annotators)
    {
        if (tree == null)
        {
            return null;
        }

        var treeAnnotator = new InternalTreeAnnotator(semanticModel, annotators);
        return treeAnnotator.Visit(tree);
    }

    private sealed class InternalTreeAnnotator(SemanticModel SemanticModel, IReadOnlyList<ITreeAnnotator> Annotators)
        : CSharpSyntaxRewriter
    {
        public override SyntaxNode? Visit(SyntaxNode? originalNode)
        {
            if (originalNode == null)
            {
                return null;
            }

            var symbolInfo = SemanticModel.GetSymbolInfo(originalNode);

            var rewrittenNode = base.Visit(originalNode);

            if (rewrittenNode == null)
            {
                return null;
            }

            if (symbolInfo.Symbol != null)
            {
                foreach (var annotator in Annotators)
                {
                    rewrittenNode = annotator.Annotate(rewrittenNode, symbolInfo.Symbol);
                }
            }

            return rewrittenNode;
        }
    }
}
