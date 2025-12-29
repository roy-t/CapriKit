using Microsoft.CodeAnalysis;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

/// <summary>
/// An ITreeRewriter should use previously set annotations to determine if and how
/// to rewrite the node. Whenever a node is rewritten, care should be taken to
/// include the trivia and annotations of the original node so that later rewriters
/// can use that information to perform their operations.
/// </summary>
internal interface ITreeRewriter
{
    public SyntaxNode Rewrite(SyntaxNode node);
}
