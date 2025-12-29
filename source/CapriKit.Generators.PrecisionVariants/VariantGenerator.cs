using CapriKit.Generators.PrecisionVariants.Visitors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CapriKit.Generators.PrecisionVariants;

/// <summary>
/// Generates new variants for methods attributed with a "CapriKit.PrecisionVariants.GenerateXYZVariant" attribute.
/// For example, for the method `double Sum(double a, double b){ return a + b; }` we can generate a variant that
/// works with floats, `float Sum(float a, float b){ return a + b; }`
/// </summary>
[Generator]
public class VariantGenerator : IIncrementalGenerator
{
    private record RewriteTemplate(string? Namespace, string ClassName, IMethodSymbol Symbol, MethodDeclarationSyntax Syntax, RewriteRule RewriteRule);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            "CapriKit.PrecisionVariants.GenerateFloatVariant",
            static (syntaxNode, cancellationToken) => syntaxNode is BaseMethodDeclarationSyntax,
            static (context, cancellationToken) => TransformToTemplate(context, cancellationToken, RewriteRule.DoubleToFloat));

        var pipelineWithComp = pipeline.Combine(context.CompilationProvider);

        context.RegisterSourceOutput(pipelineWithComp, static (producer, pair) =>
        {
            var (model, compilation) = pair;
            if (model != null)
            {
                try
                {
                    var semanticModel = compilation.GetSemanticModel(model.Syntax.SyntaxTree);
                    var (hintName, source) = EmitVariant(semanticModel, model, compilation);
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
                    var location = model.Symbol.Locations.FirstOrDefault();
                    var diagnostic = Diagnostic.Create(description, location, ex.Message);

                    producer.ReportDiagnostic(diagnostic);
                }
            }
        });
    }

    private static RewriteTemplate? TransformToTemplate(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken, RewriteRule rewriteRule)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(context.TargetNode, cancellationToken);
        if (symbol is IMethodSymbol method && context.TargetNode is MethodDeclarationSyntax methodDeclaration)
        {
            var @class = context.TargetSymbol.ContainingType;
            var @namespace = GetNamespace(@class.ContainingNamespace);

            return new RewriteTemplate(@namespace, @class.Name, method, methodDeclaration, rewriteRule);
        }

        return null;
    }

    private static (string hintName, string source) EmitVariant(SemanticModel semanticModel, RewriteTemplate template, Compilation compilation)
    {
        var visitors = new ATreeVisitor[]
        {
            new TypeNameRewriter(),
            new PredefinedTypeRewriter(template.RewriteRule),
            new LiteralRewriter(template.RewriteRule),
            new MathRewriter(template.RewriteRule)
        };

        var syntaxRoot = template.Syntax;
        var annotatedRoot = TreeAnnotator.Annotate(syntaxRoot, semanticModel, visitors);
        var rewrittenRoot = TreeRewriter.Rewrite(annotatedRoot, visitors);

        var methodText = rewrittenRoot.NormalizeWhitespace().ToFullString();

        var fileText = $$"""
            namespace {{template.Namespace ?? "CapriKit.Generated"}}
            {
                partial class {{template.ClassName}}
                {
            {{IndentString("        ", methodText)}}
                }
            }
            """;

        return ($"{template.ClassName}_{template.Symbol.Name}_{template.RewriteRule}.g.cs", fileText);
    }

    private static string? GetNamespace(INamespaceSymbol? symbol)
    {
        return symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
    }

    private static string IndentString(string indent, string text)
    {
        var lines = text.Split('\n');
        return indent + string.Join("\n" + indent, lines);
    }
}



