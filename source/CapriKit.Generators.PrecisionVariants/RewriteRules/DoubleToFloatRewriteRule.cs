using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants.RewriteRules;

internal sealed class DoubleToFloatRewriteRule : ITypeRewriterRule
{
    private readonly ITypeSymbol FloatTypeSymbol;

    public DoubleToFloatRewriteRule(Compilation compilation)
    {
        FloatTypeSymbol = compilation.GetTypeByMetadataName("System.Single") ?? throw new Exception("Cannot find type float");
    }

    public (TypeSyntax, ITypeSymbol) Rewrite(TypeSyntax syntax, ITypeSymbol symbol)
    {
        if (syntax is not PredefinedTypeSyntax predefinedSyntax ||
            !predefinedSyntax.Keyword.IsKind(SyntaxKind.DoubleKeyword))
        {
            return (syntax, symbol);
        }

        var floatSyntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.FloatKeyword))
            .WithTriviaFrom(syntax);

        return (floatSyntax, FloatTypeSymbol);
    }
}
