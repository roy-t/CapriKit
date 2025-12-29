using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants.Visitors;

/// <summary>
/// Rewrites the syntax nodes that reference types so that they reference them by their
/// fully qualified name (e.g. Array -> global::System.Array). This means that they can
/// be referenced without adding the required using statements.
/// </summary>
internal sealed class TypeNameRewriter() : ATreeVisitor("FULLY_QUALIFIED_TYPE_NAME")
{
    // Format for fully qualified type names like: global::System.Array
    // Ignores:
    // - Special types (object, float, int, ..), these are kept as they are always available
    // - Generic type arguments: These are visited and rewritten while traversing the syntax tree
    // - Nullability modifers (?, !): These are separate symbols in the syntax tree
    private static readonly SymbolDisplayFormat TypeNameFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    public override SyntaxNode Annotate(SyntaxNode node, ISymbol symbol)
    {
        // Note: a predefined nullable type, like double? is not a predefined type according
        // to SyntaxFacts but has the PredefinedType syntax kind.
        var kind = node.Kind();
        if (kind == SyntaxKind.PredefinedType || SyntaxFacts.IsPredefinedType(node.Kind()))
        {
            return node;
        }

        switch (node)
        {
            // Attributes do not implement ITypeSymbol and when walking            
            case AttributeSyntax attribute:
                return AnnotateAttribute(attribute, symbol);
            default:
                if (node is TypeSyntax && symbol is ITypeSymbol typeSymbol)
                {
                    if (IsNullableValueType(typeSymbol, out ITypeSymbol typeArgument))
                    {
                        var name = typeArgument.ToDisplayString(TypeNameFormat);
                        return Annotate(node, name);
                    }
                    else
                    {
                        var name = typeSymbol.ToDisplayString(TypeNameFormat);
                        return Annotate(node, name);
                    }
                }
                break;
        }

        return node;
    }

    private static bool IsNullableValueType(ITypeSymbol type, out ITypeSymbol typeArgument)
    {
        if (type is INamedTypeSymbol named &&
            named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            typeArgument = named.TypeArguments[0];
            return true;
        }

        typeArgument = type;
        return false;
    }

    private SyntaxNode AnnotateAttribute(AttributeSyntax attribute, ISymbol symbol)
    {
        var type = symbol.ContainingType;
        var name = type.ToDisplayString(TypeNameFormat);
        return Annotate(attribute, name);
    }

    public override SyntaxNode Execute(SyntaxNode node)
    {
        if (TryGetAnnotation(node, out string data))
        {
            switch (node)
            {
                // Take special care when rewriting syntax nodes that 'wrap' a type
                case AttributeSyntax attribute:
                    return RewriteAttribute(attribute, data);
                case GenericNameSyntax generic:
                    return RewriteGenericName(generic, data);
                case NullableTypeSyntax nullable:
                    return RewriteNullableType(nullable, data);

                default:
                    var typeNode = SyntaxFactory.ParseTypeName(data);
                    typeNode = node.CopyAnnotationsTo(typeNode);
                    return typeNode.WithTriviaFrom(node);
            }
        }

        return node;
    }

    private AttributeSyntax RewriteAttribute(AttributeSyntax attribute, string data)
    {
        var name = SyntaxFactory.ParseName(data)
                    .WithTriviaFrom(attribute.Name);
        name = attribute.Name.CopyAnnotationsTo(name);

        var rewritten = attribute.WithName(name);
        rewritten = attribute.CopyAnnotationsTo(rewritten);
        return rewritten.WithTriviaFrom(attribute);
    }
    private GenericNameSyntax RewriteGenericName(GenericNameSyntax generic, string data)
    {
        var identifier = SyntaxFactory.Identifier(data)
                        .WithTriviaFrom(generic.Identifier);
        identifier = generic.Identifier.CopyAnnotationsTo(identifier);

        var rewritten = generic.WithIdentifier(identifier);
        rewritten = generic.CopyAnnotationsTo(rewritten);
        return rewritten.WithTriviaFrom(generic);
    }

    private NullableTypeSyntax RewriteNullableType(NullableTypeSyntax nullable, string data)
    {
        var name = SyntaxFactory.ParseTypeName(data)
            .WithTriviaFrom(nullable.ElementType);
        name = nullable.ElementType.CopyAnnotationsTo(name);

        var rewritten = nullable.WithElementType(name);
        rewritten = nullable.CopyAnnotationsTo(rewritten);
        return rewritten.WithTriviaFrom(nullable);
    }
}
