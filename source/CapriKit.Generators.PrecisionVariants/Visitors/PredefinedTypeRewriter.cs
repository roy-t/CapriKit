using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

/// <summary>
/// Rewrites syntax nodes from one predefined type (e.g. double) to another (e.g. float).
/// Handles array with predefined element types as well (e.g. double[] to float[])
/// </summary>
internal sealed class PredefinedTypeRewriter : ATreeVisitor
{
    private readonly SyntaxKind FromKind;
    private readonly SyntaxKind ToKind;
    private readonly string Id;

    public PredefinedTypeRewriter(RewriteRule rewriteRule)
        : base("PREDEFINED_TYPE_VARIANT_TARGET")
    {
        // Use a guid to uniquely identify the rewriter with this rule
        // so that we do not accidentaly rewrite nodes that another instance,
        // with a different rule, would like to change.
        Id = Guid.NewGuid().ToString();
        switch (rewriteRule)
        {
            case RewriteRule.DoubleToFloat:
                FromKind = SyntaxKind.DoubleKeyword;
                ToKind = SyntaxKind.FloatKeyword;
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unexpected rewrite rule {rewriteRule}");
        }
    }

    public override SyntaxNode Annotate(SyntaxNode node, ISymbol symbol)
    {
        if (node is PredefinedTypeSyntax predefinedSyntax &&
           predefinedSyntax.Keyword.IsKind(FromKind))
        {
            return Annotate(node, Id);
        }

        if (node is ArrayTypeSyntax arrayType &&
            arrayType.ElementType is PredefinedTypeSyntax predefinedTypeSyntax &&
            predefinedTypeSyntax.Keyword.IsKind(FromKind))
        {
            return Annotate(node, Id);
        }

        return node;
    }

    public override SyntaxNode Rewrite(SyntaxNode node)
    {
        if (TryGetAnnotation(node, out string data) && data.Equals(Id))
        {
            if (node is PredefinedTypeSyntax predefinedType)
            {
                var typeNode = SyntaxFactory.PredefinedType(SyntaxFactory.Token(ToKind));
                typeNode = predefinedType.CopyAnnotationsTo(typeNode);
                return typeNode.WithTriviaFrom(predefinedType);
            }

            if (node is ArrayTypeSyntax arrayType)
            {
                var arrayTypeNode = SyntaxFactory.PredefinedType(SyntaxFactory.Token(ToKind));
                arrayTypeNode = arrayType.ElementType.CopyAnnotationsTo(arrayTypeNode)
                    .WithTriviaFrom(arrayType.ElementType);

                return arrayType.WithElementType(arrayTypeNode);
            }
        }

        return node;
    }
}
