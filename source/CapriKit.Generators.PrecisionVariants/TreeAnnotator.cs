using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants;

internal interface ITreeAnnotator
{
    public SyntaxNode Annotate(SyntaxNode node, ISymbol symbol);
}

internal interface ITreeRewriter
{
    public SyntaxNode Execute(SyntaxNode node);
}

internal abstract class ATreeVisitor(string annotationKey) : ITreeAnnotator, ITreeRewriter
{
    public abstract SyntaxNode Annotate(SyntaxNode node, ISymbol symbol);

    public abstract SyntaxNode Execute(SyntaxNode node);

    protected string AnnotationKey { get; } = annotationKey;

    protected SyntaxNode Annotate(SyntaxNode node, string data)
    {
        return node.WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationKey, data));
    }

    protected bool TryGetAnnotation(SyntaxNode node, out string data)
    {
        var annotation = node.GetAnnotations(AnnotationKey).SingleOrDefault();
        if (annotation != null)
        {
            data = annotation.Data ?? string.Empty;
            return true;
        }

        data = string.Empty;
        return false;
    }
}

internal sealed class FullyQualifiedTypeNameAnnotator() : ATreeVisitor("FULLY_QUALIFIED_TYPE_NAME")
{
    private static readonly SymbolDisplayFormat Format = new(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    genericsOptions:
        SymbolDisplayGenericsOptions.IncludeTypeParameters |
        SymbolDisplayGenericsOptions.IncludeVariance,
    miscellaneousOptions:
        SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
        SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);


    public override SyntaxNode Annotate(SyntaxNode node, ISymbol symbol)
    {
        // TODO: this might require special care for Attributes/AttributeLists

        if (!SyntaxFacts.IsPredefinedType(node.Kind()) && symbol is ITypeSymbol typeSymbol)
        {
            var name = typeSymbol.ToDisplayString(Format);
            return Annotate(node, name);
        }

        return node;
    }

    public override SyntaxNode Execute(SyntaxNode node)
    {
        if (TryGetAnnotation(node, out string data))
        {
            var typeNode = SyntaxFactory.ParseTypeName(data);
            typeNode = node.CopyAnnotationsTo(typeNode);
            return typeNode.WithTriviaFrom(node);
        }

        return node;
    }
}

internal sealed class TypePrecisionAnnotator() : ATreeVisitor("NUMERIC_VARIANT_TARGET")
{
    public override SyntaxNode Annotate(SyntaxNode node, ISymbol symbol)
    {
        if (node is PredefinedTypeSyntax predefinedSyntax &&
           predefinedSyntax.Keyword.IsKind(SyntaxKind.DoubleKeyword))
        {
            return Annotate(node, "float");
        }

        return node;
    }

    public override SyntaxNode Execute(SyntaxNode node)
    {
        if (TryGetAnnotation(node, out string data))
        {
            var kind = data switch
            {
                "float" => SyntaxKind.FloatKeyword,
                _ => throw new NotSupportedException($"Unexpected precision annotation: {data}")
            };

            var typeNode = SyntaxFactory.PredefinedType(SyntaxFactory.Token(kind));
            typeNode = node.CopyAnnotationsTo(typeNode);
            return typeNode.WithTriviaFrom(node);
        }

        return node;
    }
}

internal sealed class TreeAnnotator : CSharpSyntaxRewriter
{
    private readonly SemanticModel semanticModel;
    private readonly IReadOnlyList<ITreeAnnotator> annotators;

    public TreeAnnotator(SemanticModel SemanticModel, params IReadOnlyList<ITreeAnnotator> Annotators)
    {
        semanticModel = SemanticModel;
        annotators = Annotators;
    }

    public override SyntaxNode? Visit(SyntaxNode? originalNode)
    {
        if (originalNode == null)
        {
            return null;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(originalNode);

        var rewrittenNode = base.Visit(originalNode);

        if (rewrittenNode == null)
        {
            return null;
        }

        if (symbolInfo.Symbol != null)
        {
            foreach (var annotator in annotators)
            {
                rewrittenNode = annotator.Annotate(rewrittenNode, symbolInfo.Symbol);
            }
        }

        return rewrittenNode;
    }
}

internal sealed class TreeRewriter(params IReadOnlyList<ITreeRewriter> Rewriters)
    : CSharpSyntaxRewriter
{
    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        if (node == null)
        {
            return null;
        }

        // TODO: there is a bug somewhere that causes a float that is a generic type arugment to not be
        // picked up

        foreach (var rewriter in Rewriters)
        {
            node = rewriter.Execute(node);
        }

        return base.Visit(node);
    }
}
