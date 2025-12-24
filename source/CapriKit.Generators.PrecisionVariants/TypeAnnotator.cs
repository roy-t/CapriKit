using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CapriKit.Generators.PrecisionVariants;

internal class TypeAnnotator : CSharpSyntaxRewriter
{
    private readonly SemanticModel SemanticModel;

    public TypeAnnotator(SemanticModel semanticModel)
    {
        SemanticModel = semanticModel;
    }

    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        if (node != null)
        {
            var symbolInfo = SemanticModel.GetSymbolInfo(node);

            if (symbolInfo.Symbol is ITypeSymbol typeSymbol)
            {
                var typeInfo = SemanticModel.GetTypeInfo(node);
                var name = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                node = node.WithAdditionalAnnotations(new SyntaxAnnotation("TypeName", name));

                // TODO: USAGE
                // node.GetAnnotations("TypeName") // To get the annotation back
                // Be sure to pass on annotations when rewriting stuff by always adding
                // .WithAdditionalAnnotations(node.GetAnnotations());
            }
        }

        return base.Visit(node);
    }
}
