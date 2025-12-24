using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;

namespace CapriKit.Generators.PrecisionVariants;

/// <summary>
/// Prevents our own attributes from ending up in the generated code
/// </summary>
internal sealed class AttributeListRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel SemanticModel;

    public AttributeListRewriter(SemanticModel semanticModel)
    {
        SemanticModel = semanticModel;
    }

    public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
    {
        var remainingAttributes = node.Attributes
            .Where(a => !IsVariantGeneratorAttribute(a));

        if (!remainingAttributes.Any())
        {
            return null;
        }

        var attributeList = SyntaxFactory.SeparatedList(remainingAttributes);
        return node.WithAttributes(attributeList);
    }

    private bool IsVariantGeneratorAttribute(AttributeSyntax attribute)
    {
        var position = attribute.SpanStart;
        var attributeSymbol = attribute.SyntaxTree.HasCompilationUnitRoot
            ? SemanticModel.GetSymbolInfo(attribute)
            : SemanticModel.GetSpeculativeSymbolInfo(position, attribute, SpeculativeBindingOption.BindAsExpression);
        return attributeSymbol.Symbol is not null &&
            attributeSymbol.Symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .StartsWith("global::CapriKit.PrecisionVariants");
    }
}
