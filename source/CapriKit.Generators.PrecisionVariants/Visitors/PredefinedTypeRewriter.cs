using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

/// <summary>
/// Rewrites syntax nodes from one predefiend type (e.g. double) to another (e.g. float)
/// </summary>
internal sealed class PredefinedTypeRewriter : ATreeVisitor
{
    private readonly SyntaxKind FromKind;
    private readonly SyntaxKind ToKind;
    private readonly string Id;

    private PredefinedTypeRewriter(SyntaxKind fromKind, SyntaxKind toKind) : base("NUMERIC_VARIANT_TARGET")
    {
        FromKind = fromKind;
        ToKind = toKind;
        Id = Guid.NewGuid().ToString();
    }

    public static PredefinedTypeRewriter DoubleToFloat() => new(SyntaxKind.DoubleKeyword, SyntaxKind.FloatKeyword);

    public override SyntaxNode Annotate(SyntaxNode node, ISymbol symbol)
    {
        if (node is PredefinedTypeSyntax predefinedSyntax &&
           predefinedSyntax.Keyword.IsKind(FromKind))
        {
            return Annotate(node, Id);
        }

        return node;
    }

    public override SyntaxNode Execute(SyntaxNode node)
    {
        if (TryGetAnnotation(node, out string data) && data.Equals(Id))
        {
            var typeNode = SyntaxFactory.PredefinedType(SyntaxFactory.Token(ToKind));
            typeNode = node.CopyAnnotationsTo(typeNode);
            return typeNode.WithTriviaFrom(node);
        }

        return node;
    }
}
