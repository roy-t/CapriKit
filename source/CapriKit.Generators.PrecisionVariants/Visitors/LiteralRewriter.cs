using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;

namespace CapriKit.Generators.PrecisionVariants.Visitors
{
    internal class LiteralRewriter : ATreeVisitor
    {
        public LiteralRewriter() : base("LITERAL_VARIANT_TARGET")
        {

        }

        public override SyntaxNode Annotate(SyntaxNode node, ISymbol symbol)
        {
            if (node.IsKind(SyntaxKind.NumericLiteralExpression) && node is LiteralExpressionSyntax literal)
            {
                // TODO: assumes double to float
                var value = literal.Token.Value;
                var fValue = Convert.ToSingle(value);
                var data = fValue.ToString("R", CultureInfo.InvariantCulture) + "f";
                Annotate(node, data);
            }
        }

        public override SyntaxNode Execute(SyntaxNode node)
        {
            throw new NotImplementedException();
        }
    }
}
