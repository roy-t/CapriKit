using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants
{
    internal class FloatingPointVariantRewriter : CSharpSyntaxRewriter
    {
        private readonly IReadOnlyList<SyntaxKind> From;
        private readonly SyntaxKind To;

        public FloatingPointVariantRewriter(IReadOnlyList<SyntaxKind> from, SyntaxKind to)
        {
            this.From = from;
            this.To = to;
        }

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (node is TypeSyntax type && SyntaxFacts.IsInTypeOnlyContext(type))
            {
                node = RewriteType(type);
            }
            return base.Visit(node);
        }        

        private TypeSyntax RewriteType(TypeSyntax syntax)
        {
            switch (syntax)
            {
                case GenericNameSyntax genericName:
                    return RewriteGenericType(genericName);

                case ArrayTypeSyntax arrayType:
                    return RewriteArrayType(arrayType);
            }

            if (syntax.IsKind(SyntaxKind.PredefinedType) &&
                syntax is PredefinedTypeSyntax predefined &&
                From.Contains(predefined.Keyword.Kind()))
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(To));
            }

            return syntax;
        }

        private ArrayTypeSyntax RewriteArrayType(ArrayTypeSyntax arrayType)
        {
            return arrayType.WithElementType(RewriteType(arrayType.ElementType));
        }

        private GenericNameSyntax RewriteGenericType(GenericNameSyntax syntax)
        {
            return syntax.WithTypeArgumentList(RewriteTypeArgumentList(syntax.TypeArgumentList));
        }

        private TypeArgumentListSyntax RewriteTypeArgumentList(TypeArgumentListSyntax syntax)
        {
            var oldArguments = syntax.Arguments;
            var newArguments = new List<TypeSyntax>();
            foreach (var argument in oldArguments)
            {
                newArguments.Add(RewriteType(argument));
            }

            return syntax.WithArguments(SyntaxFactory.SeparatedList(newArguments));
        }
    }
}
