using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants
{
    internal class FloatingPointVariantRewriter(IReadOnlyList<SyntaxKind> from, SyntaxKind to) : CSharpSyntaxRewriter
    {
        public IReadOnlyList<SyntaxKind> From { get; } = from;
        public SyntaxKind To { get; } = to;

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (IsReplacableKeyword(node.ReturnType))
            {
                node = node.WithReturnType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(To)));
            }

            var oldParameters = node.ParameterList.Parameters;
            var newParameters = new List<ParameterSyntax>();
            foreach (var parameter in oldParameters)
            {
                // TODO: add support for arrays and generic type parameters
                if (parameter.Type != null && IsReplacableKeyword(parameter.Type))
                {
                    var targetType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(To));
                    newParameters.Add(parameter.WithType(targetType));
                }
                else
                {
                    newParameters.Add(parameter);
                }
            }
            node = node.WithParameterList(node.ParameterList.WithParameters(SyntaxFactory.SeparatedList(newParameters)));

            return base.VisitMethodDeclaration(node);
        }


        private bool IsReplacableKeyword(TypeSyntax syntax)
        {
            if (syntax.IsKind(SyntaxKind.PredefinedType) && syntax is PredefinedTypeSyntax predefined)
            {
                return from.Contains(predefined.Keyword.Kind());
            }
            return false;
        }
    }
}
