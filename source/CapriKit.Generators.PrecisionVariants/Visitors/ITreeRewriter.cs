using Microsoft.CodeAnalysis;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

internal interface ITreeRewriter
{
    public SyntaxNode Execute(SyntaxNode node);
}
