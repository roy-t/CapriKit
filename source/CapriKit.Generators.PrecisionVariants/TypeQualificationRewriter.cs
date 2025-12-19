using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants;

/// <summary>
/// Converts the type qualification style of the given syntax node to their fully qualified types to ensure that we
/// do not have to define using statements or have to worry about clashes with them.
/// For example: `Span` is converted to `global::System.Span`
/// </summary>
internal sealed class TypeQualificationRewriter : CSharpSyntaxRewriter
{    
    public static SymbolDisplayFormat FullyQualifiedTypeFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions:
                SymbolDisplayGenericsOptions.IncludeTypeParameters |
                SymbolDisplayGenericsOptions.IncludeVariance,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private readonly SemanticModel SemanticModel;

    public TypeQualificationRewriter(SemanticModel semanticModel)
    {
        SemanticModel = semanticModel;
    }    

    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        if (node != null &&
            // Skip items we've changed and thus do not come from the original compilation
            node.SyntaxTree.HasCompilationUnitRoot &&
            node is TypeSyntax typeSyntax)
        {
            // An IdentifierName can represent where the name of a type is used and
            // where the name of something that has a type is used, like a property or variable.
            if (typeSyntax is IdentifierNameSyntax identifierName)
            {
                var symbol = SemanticModel.GetSymbolInfo(identifierName);
                if (symbol.Symbol is not INamedTypeSymbol)
                {
                    return base.Visit(node);
                }
            }

            var info = SemanticModel.GetTypeInfo(typeSyntax);
            var typeSymbol = info.Type;

            if (typeSymbol == null ||
                typeSymbol.TypeKind == TypeKind.TypeParameter ||
                typeSymbol.TypeKind == TypeKind.Dynamic)
            {
                return base.Visit(node);
            }

            var qualifiedType = typeSymbol.ToDisplayString(FullyQualifiedTypeFormat);
            var parsed = SyntaxFactory.ParseTypeName(qualifiedType)
                                      .WithTriviaFrom(typeSyntax);

            return base.Visit(parsed);
        }

        return base.Visit(node);
    }

    public override SyntaxNode? VisitAttribute(AttributeSyntax node)
    {
        var ctor = SemanticModel.GetSymbolInfo(node).Symbol as IMethodSymbol;
        var attrType = ctor?.ContainingType;

        if (attrType != null)
        {
            var fq = attrType.ToDisplayString(FullyQualifiedTypeFormat);
            var newName = SyntaxFactory.ParseName(fq).WithTriviaFrom(node.Name);
            node = node.WithName(newName);
        }

        return base.VisitAttribute(node);
    }
}
