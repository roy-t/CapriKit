using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants.RewriteRules;

internal sealed class FullyQualifiedNameRewriterRule : ITypeRewriterRule
{
    private static readonly SymbolDisplayFormat FullyQualifiedTypeFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions:
            SymbolDisplayGenericsOptions.IncludeTypeParameters |
            SymbolDisplayGenericsOptions.IncludeVariance,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public (TypeSyntax, ITypeSymbol) Rewrite(TypeSyntax syntax, ITypeSymbol symbol)
    {
        if (SyntaxFacts.IsPredefinedType(syntax.Kind()) || syntax.IsVar)
        {
            return (syntax, symbol);
        }        

        var qualifiedName = symbol.ToDisplayString(FullyQualifiedTypeFormat);
        var qualifiedTypeSyntax = SyntaxFactory.ParseTypeName(qualifiedName)
                                      .WithTriviaFrom(syntax);

        return (qualifiedTypeSyntax, symbol);
    }
}
