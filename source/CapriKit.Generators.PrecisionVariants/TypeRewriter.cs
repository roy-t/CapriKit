using CapriKit.Generators.PrecisionVariants.RewriteRules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants;

/// <summary>
/// Analyzes each type in the syntax tree and rewrites it according to the given type rewrite rules.
/// </summary>
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
        var position = node?.SpanStart ?? 0;
        node = base.Visit(node);

        if (node is AttributeListSyntax attributeList)
        {
            node = RewriteAttributeList(attributeList);
        }


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
            typeSymbol.TypeKind == TypeKind.Array ||
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

    private SyntaxNode RewriteAttributeList(AttributeListSyntax attributeList)
    {
        var list = new List<AttributeSyntax>();
        foreach (var attribute in attributeList.Attributes)
        {
            var position = attribute.SpanStart;
            var attributeSymbol = attribute.SyntaxTree.HasCompilationUnitRoot
                ? SemanticModel.GetSymbolInfo(attribute)
                : SemanticModel.GetSpeculativeSymbolInfo(position, attribute, SpeculativeBindingOption.BindAsExpression);

            if (attributeSymbol.Symbol != null && attributeSymbol.Symbol.ContainingType != null)
            {
                ITypeSymbol typeSymbol = attributeSymbol.Symbol.ContainingType;
                TypeSyntax typeSyntax = attribute.Name;

                foreach (var rule in Rules)
                {
                    (typeSyntax, typeSymbol) = rule.Rewrite(typeSyntax, typeSymbol);
                }

                if (typeSyntax is NameSyntax name)
                {
                    list.Add(attribute.WithName(name));
                }
                else
                {
                    var nameBestGuess = SyntaxFactory.ParseName(typeSyntax.ToFullString()).WithTriviaFrom(attribute.Name);
                    list.Add(attribute.WithName(nameBestGuess));
                }
            }
            else
            {
                list.Add(attribute);
            }
        }

        return attributeList.WithAttributes(SyntaxFactory.SeparatedList(list));
    }
}
