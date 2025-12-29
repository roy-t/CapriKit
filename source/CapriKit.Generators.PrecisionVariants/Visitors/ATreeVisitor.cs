using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

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
