using Microsoft.CodeAnalysis;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

internal interface ITreeAnnotator
{
    public SyntaxNode Annotate(SyntaxNode node, ISymbol symbol);
}
