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
            node = node.WithReturnType(RewriteType(node.ReturnType));
            node = node.WithParameterList(RewriteParameterList(node.ParameterList));

            //var oldParameters = node.ParameterList.Parameters;
            //var newParameters = new List<ParameterSyntax>();

            //foreach (var parameter in oldParameters)
            //{
            //    // TODO: add support for arrays and generic type parameters

            //    // Plain argument
            //    if (parameter.Type != null)
            //    {
            //        newParameters.Add(parameter.WithType(RewriteType(parameter.Type)));
            //    }
            //    else
            //    {
            //        newParameters.Add(parameter);
            //    }

            //    //GenericNameSyntax
            //}
            //node = node.WithParameterList(node.ParameterList.WithParameters(SyntaxFactory.SeparatedList(newParameters)));

            return base.VisitMethodDeclaration(node);
        }

        private ParameterListSyntax RewriteParameterList(ParameterListSyntax syntax)
        {
            var oldParameters = syntax.Parameters;
            var newParameters = new List<ParameterSyntax>();

            foreach (var parameter in oldParameters)
            {
                newParameters.Add(RewriteParameter(parameter));
            }

            return syntax.WithParameters(SyntaxFactory.SeparatedList(newParameters));
        }

        private ParameterSyntax RewriteParameter(ParameterSyntax syntax)
        {
            if (syntax.Type != null)
            {
                return syntax.WithType(RewriteType(syntax.Type));
            }
            return syntax;
        }

        private TypeSyntax RewriteType(TypeSyntax syntax)
        {
            switch(syntax)
            {
                case GenericNameSyntax genericName:
                    return RewriteType(genericName);
                // TODO: arrays
            }
            
            if (syntax.IsKind(SyntaxKind.PredefinedType) &&
                syntax is PredefinedTypeSyntax predefined &&
                from.Contains(predefined.Keyword.Kind()))
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(To));
            }

            return syntax;
        }

        private GenericNameSyntax RewriteType(GenericNameSyntax syntax)
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
