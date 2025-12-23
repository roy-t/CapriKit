using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants;

internal interface ITypeRewriterRule
{
    public (TypeSyntax, ITypeSymbol) Rewrite(TypeSyntax syntax, ITypeSymbol symbol);
}

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
        if (SyntaxFacts.IsPredefinedType(syntax.Kind()))
        {
            return (syntax, symbol);
        }

        var qualifiedName = symbol.ToDisplayString(FullyQualifiedTypeFormat);
        var qualifiedTypeSyntax = SyntaxFactory.ParseTypeName(qualifiedName)
                                      .WithTriviaFrom(syntax);

        return (qualifiedTypeSyntax, symbol);
    }
}

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

internal sealed class MathRewriteRule : ITypeRewriterRule
{
    private readonly ITypeSymbol MathFTypeSymbol;


    public MathRewriteRule(Compilation compilation)
    {
        MathFTypeSymbol = compilation.GetTypeByMetadataName("System.MathF") ?? throw new Exception("Cannot find System.MathF");
    }

    public (TypeSyntax, ITypeSymbol) Rewrite(TypeSyntax syntax, ITypeSymbol symbol)
    {
        // TODO: doesn't work after rerwite to GLOBAL
        if (syntax is not IdentifierNameSyntax identifierNameSyntax)
        {
            return (syntax, symbol);
        }

        var mathFSyntax = SyntaxFactory.IdentifierName("global::Systam::MathF")
            .WithTriviaFrom(syntax);

        return (mathFSyntax, MathFTypeSymbol);
    }
}

internal sealed class TypeRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel SemanticModel;

    private readonly IReadOnlyList<ITypeRewriterRule> Rules;

    public TypeRewriter(SemanticModel semanticModel, params IReadOnlyList<ITypeRewriterRule> rules)
    {
        SemanticModel = semanticModel;
        Rules = rules;
    }

    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        var position = node?.FullSpan.Start ?? 0;
        node = base.Visit(node);

        if (node == null || node is not TypeSyntax typeSyntax)
        {
            return node;
        }

        // An IdentifierName can represent where the name of a type is used and
        // where the name of something that has a type is used, like a property or variable.
        if (typeSyntax is IdentifierNameSyntax identifierNameSyntax)
        {
            // Rewritten notes are not part of the original compilation's syntax tree
            // therefore we need to guess at their exact symbol and type
            var symbol = node.SyntaxTree.HasCompilationUnitRoot
            ? SemanticModel.GetSymbolInfo(node)
            : SemanticModel.GetSpeculativeSymbolInfo(position, node, SpeculativeBindingOption.BindAsTypeOrNamespace);

            if (symbol.Symbol is not INamedTypeSymbol)
            {
                return node;
            }
        }

        // Rewritten notes are not part of the original compilation's syntax tree
        // therefore we need to guess at their exact symbol and type
        var typeInfo = node.SyntaxTree.HasCompilationUnitRoot
            ? SemanticModel.GetTypeInfo(node)
            : SemanticModel.GetSpeculativeTypeInfo(position, node, SpeculativeBindingOption.BindAsTypeOrNamespace);

        var typeSymbol = typeInfo.Type;
        if (typeSymbol == null ||
            typeSymbol.TypeKind == TypeKind.TypeParameter ||
            typeSymbol.TypeKind == TypeKind.Dynamic)
        {
            return node;
        }

        foreach (var rule in Rules)
        {
            (typeSyntax, typeSymbol) = rule.Rewrite(typeSyntax, typeSymbol);
        }

        return typeSyntax;
    }
}
