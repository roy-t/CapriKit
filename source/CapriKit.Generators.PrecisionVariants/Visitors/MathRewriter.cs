using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

/// <summary>
/// Rewrites usage of the static Math class with MathF, if the target variant is a float
/// </summary>
internal sealed class MathRewriter(RewriteRule RewriteRule) : ATreeVisitor("VARIANT_MATH")
{
    private readonly IdentifierNameSyntax MathFSyntax = SyntaxFactory.IdentifierName("MathF");

    public override SyntaxNode Annotate(SyntaxNode node, ISymbol symbol)
    {
        // Only for floats we need to use MathF instead of Math
        if (RewriteRule == RewriteRule.DoubleToFloat &&
            node is IdentifierNameSyntax identifierNameSyntax &&
            identifierNameSyntax.Identifier.ValueText == "Math")
        {
            return Annotate(node, "MathF");
        }

        return node;
    }

    public override SyntaxNode Rewrite(SyntaxNode node)
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
