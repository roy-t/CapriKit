using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

internal sealed class MathRewriter() : ATreeVisitor("VARIANT_MATH")
{
    private readonly IdentifierNameSyntax MathFSyntax = SyntaxFactory.IdentifierName("MathF");

    public override SyntaxNode Annotate(SyntaxNode node, ISymbol symbol)
    {
        if (node is QualifiedNameSyntax qualifiedNameSyntax &&
            qualifiedNameSyntax.Right is IdentifierNameSyntax identifierNameSyntax &&
            identifierNameSyntax.Identifier.ValueText == "Math")
        {
            return Annotate(node, "MathF");
        }

        return node;
    }

    public override SyntaxNode Execute(SyntaxNode node)
    {
        if (TryGetAnnotation(node, out string _) && node is QualifiedNameSyntax name)
        {
            var rewritten = name.WithRight(MathFSyntax);
            rewritten = node.CopyAnnotationsTo(rewritten);
            return rewritten.WithTriviaFrom(node);
        }

        return node;
    }
}
