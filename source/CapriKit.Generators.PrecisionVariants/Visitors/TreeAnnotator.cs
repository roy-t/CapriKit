using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

/// <summary>
/// Allows ITreeAnnotators to make annotations to the tree, while having access to the full semantic model.
/// This annotation step is usually the precursor for a rewrite step. In which information from the annotations
/// is used to determin if and how to rewrite a node. This is nccessary because the semantic model is invalidated
/// as soon as a node or its child nodes are replaced.
/// </summary>
internal sealed class TreeAnnotator
{
    public static SyntaxNode Annotate(SyntaxNode tree, SemanticModel semanticModel, params IReadOnlyList<ITreeAnnotator> annotators)
    {
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
