using Microsoft.CodeAnalysis;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

/// <summary>
/// An ITreeAnnotator can add anotations to syntax node, which an ITreeRewriter can later
/// use to determine if and how to rewrite the syntax node. 
/// </summary>
internal interface ITreeAnnotator
{
    public SyntaxNode Annotate(SyntaxNode node, ISymbol symbol);
}
