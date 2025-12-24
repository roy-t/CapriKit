using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;

namespace CapriKit.Generators.PrecisionVariants;

/// <summary>
/// Analyzes all literals in the syntax tree and rewrites them to literals of a different type
/// </summary>
internal class LiteralRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel SemanticModel;

    public LiteralRewriter(SemanticModel semanticModel)
    {
        SemanticModel = semanticModel;
    }

    public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        if (!node.IsKind(SyntaxKind.NumericLiteralExpression))
        {
            return base.VisitLiteralExpression(node);
        }

        var position = node.SpanStart;
        var typeInfo = node.SyntaxTree.HasCompilationUnitRoot
            ? SemanticModel.GetTypeInfo(node)
            : SemanticModel.GetSpeculativeTypeInfo(position, node, SpeculativeBindingOption.BindAsExpression);

        if (typeInfo.Type?.SpecialType != SpecialType.System_Double)
        {
            return base.VisitLiteralExpression(node);
        }

        if (node.Token.Value is double dValue)
        {
            var fValue = (float)dValue;
            var literal = SyntaxFactory.Literal(node.Token.LeadingTrivia,
                fValue.ToString("R", CultureInfo.InvariantCulture) + "f",
                fValue, node.Token.TrailingTrivia);

            return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, literal);
        }

        return base.VisitLiteralExpression(node);
    }
}
