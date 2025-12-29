using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants.Visitors
{
    public enum LiteralRewriteRule
    {
        DoubleToFloat
    };

    internal class LiteralRewriter : ATreeVisitor
    {
        private readonly string Id;
        private readonly LiteralRewriteRule Rule;

        public LiteralRewriter(LiteralRewriteRule rule) : base("LITERAL_VARIANT_TARGET")
        {
            Id = Guid.NewGuid().ToString();
            Rule = rule;
        }

        public override SyntaxNode Annotate(SyntaxNode node, ISymbol symbol)
        {
            if (node.IsKind(SyntaxKind.NumericLiteralExpression)
                && node is LiteralExpressionSyntax literal
                && ShouldAnnotate(literal.Token.Value))
            {
                return Annotate(node, Id);
            }

            return node;
        }

        private bool ShouldAnnotate(object? value)
        {
            return Rule switch
            {
                LiteralRewriteRule.DoubleToFloat => value is double,
                _ => throw new ArgumentOutOfRangeException($"Unexpected rule {Rule}"),
            };
        }

        public override SyntaxNode Execute(SyntaxNode node)
        {
            if (TryGetAnnotation(node, out string data) && data.Equals(Id)
                && node is LiteralExpressionSyntax literalNode)
            {
                var value = literalNode.Token.Value;
                var targetValue = Convert.ToSingle(value);
                var literal = SyntaxFactory.Literal(targetValue);

                var expression = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, literal);
                expression = node.CopyAnnotationsTo(expression);
                return expression.WithTriviaFrom(node);
            }

            return node;
        }
    }
}
