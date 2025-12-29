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
    {
        public SyntaxNode Visit(SyntaxNode originalNode)
        {
            var symbolInfo = SemanticModel.GetSymbolInfo(originalNode);
            var typeInfo = SemanticModel.GetTypeInfo(originalNode);

            // Handles literals that do not have their own symbol
            var symbol = symbolInfo.Symbol ?? typeInfo.Type;

            var rewrittenNode = originalNode
                .ReplaceNodes(originalNode.ChildNodes(), (originalChild, _) => Visit(originalChild));

            if (symbol != null)
            {
                foreach (var annotator in Annotators)
                {
                    rewrittenNode = annotator.Annotate(rewrittenNode, symbol);
                }
            }

            return rewrittenNode;
        }
    }
}
