using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants.Visitors
{
    /// <summary>
    /// Rewrites literals (e.g. 1.0) to literals of a different type (e.g. 1.0f)
    /// </summary>
    internal class LiteralRewriter : ATreeVisitor
    {
        private readonly string Id;
        private readonly RewriteRule Rule;

        public LiteralRewriter(RewriteRule rule) : base("LITERAL_VARIANT_TARGET")
        {
            // Use a guid to uniquely identify the rewriter with this rule
            // so that we do not accidentaly rewrite nodes that another instance,
            // with a different rule, would like to change.
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
                RewriteRule.DoubleToFloat => value is double,
                _ => throw new ArgumentOutOfRangeException($"Unexpected rule {Rule}"),
            };
        }

        public override SyntaxNode Rewrite(SyntaxNode node)
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
