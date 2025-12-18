using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CapriKit.Generators.PrecisionVariants;

[Generator]
public class VariantGenerator : IIncrementalGenerator
{
    private record MethodTemplate(string? Namespace, string ClassName, IMethodSymbol Method, MethodDeclarationSyntax MethodDeclaration, string Variant);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            "CapriKit.PrecisionVariants.GenerateFloatVariant",
            static (syntaxNode, cancellationToken) => syntaxNode is BaseMethodDeclarationSyntax,
            static (context, cancellationToken) => TransformToTemplate(context, cancellationToken, "float"));            

        context.RegisterSourceOutput(pipeline, static (context, model) =>
        {
            if (model != null)
            {
                try
                {
                    var (hintName, source) = EmitVariant(model);
                    context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
                }
                catch (Exception ex)
                {
                    var description = new DiagnosticDescriptor
                    (
                        "PV000",
                        "Precision variant generator error",
                        $"Exception: {ex}",
                        "SourceGeneration",
                        DiagnosticSeverity.Error,
                        true
                    );
                    var location = model.Method.Locations.FirstOrDefault();
                    var diagnostic = Diagnostic.Create(description, location, ex.Message);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        });
    }

    private static MethodTemplate? TransformToTemplate(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken, string variant)
    {        
        var symbol = context.SemanticModel.GetDeclaredSymbol(context.TargetNode, cancellationToken);
        if (symbol is IMethodSymbol method && context.TargetNode is MethodDeclarationSyntax methodDeclaration)
        {
            var @class = context.TargetSymbol.ContainingType;
            var @namespace = GetNamespace(@class.ContainingNamespace);
            
            return new MethodTemplate(@namespace, @class.Name, method, methodDeclaration, variant);
        }
        
        return null;
    }

    private static (string hintName, string source) EmitVariant(MethodTemplate template)
    {
        var rewriter = new FloatingPointVariantRewriter([SyntaxKind.DoubleKeyword], SyntaxKind.FloatKeyword);
        var rewritten = rewriter.Visit(template.MethodDeclaration);
        var methodText = rewritten.NormalizeWhitespace().ToFullString();

        throw new Exception("Eek");
    }

    private static string? GetNamespace(INamespaceSymbol? symbol)
    {
        return symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
    }
}



