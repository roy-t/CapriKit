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

        var pipelineWithComp = pipeline.Combine(context.CompilationProvider);

        context.RegisterSourceOutput(pipelineWithComp, static (producer, pair) =>
        {
            var (model, compilation) = pair;
            if (model != null)
            {
                try
                {
                    var semanticModel = compilation.GetSemanticModel(model.MethodDeclaration.SyntaxTree);
                    var (hintName, source) = EmitVariant(semanticModel, model);
                    producer.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
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

                    producer.ReportDiagnostic(diagnostic);
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

    private static (string hintName, string source) EmitVariant(SemanticModel semanticModel, MethodTemplate template)
    {
        var formatRewriter = new TypeQualificationRewriter(semanticModel);
        var typeRewriter = new FloatingPointVariantRewriter([SyntaxKind.DoubleKeyword], SyntaxKind.FloatKeyword);

        var fullyQualifiedMethod = formatRewriter.Visit(template.MethodDeclaration) ?? throw new Exception("Invalid rewrite");
        var variantMethod = typeRewriter.Visit(fullyQualifiedMethod) ?? throw new Exception("Invalid rewrite");
        var methodText = variantMethod.NormalizeWhitespace().ToFullString();

        var fileText = $$"""            
            namespace {{template.Namespace ?? "CapriKit.Generated"}}
            {
                partial class {{template.ClassName}}
                {
            {{IndentString("        ", methodText)}}
                }
            }
            """;

        return ($"{template.ClassName}_{template.Method.Name}_{template.Variant}.g.cs", fileText);
    }

    private static string? GetNamespace(INamespaceSymbol? symbol)
    {
        return symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
    }

    private static string IndentString(string indent, string text)
    {
        var lines = text.Split('\n');
        return indent + String.Join("\n" + indent, lines);
    }
}



