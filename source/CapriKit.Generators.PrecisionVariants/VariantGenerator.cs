using CapriKit.Generators.PrecisionVariants.RewriteRules;
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

    private static (string hintName, string source) EmitVariant(SemanticModel semanticModel, MethodTemplate template, Compilation compilation)
    {
        SyntaxNode? syntaxRoot = template.MethodDeclaration;

        var fqn = new FullyQualifiedTypeNameAnnotator();
        var prc = new TypePrecisionAnnotator();

        var annotator = new TreeAnnotator(semanticModel, fqn, prc);
        var annotatedRoot = annotator.Visit(syntaxRoot);

        var rewriter = new TreeRewriter(fqn, prc);
        var rewrittenRoot = rewriter.Visit(annotatedRoot);
        if (rewrittenRoot != null)
        { 
            var text = rewrittenRoot.NormalizeWhitespace().ToFullString();
            var f = "";
        }


        // TODO: Can the type annotator help to become better at guessing types?
        // test by adding a second annotations like [MethodImpl(..)]
        //var annotator = new TypeAnnotator(semanticModel);
        //syntaxRoot = annotator.Visit(syntaxRoot);

        // Rewrite doubles to floats
        var nameRule = new FullyQualifiedNameRewriterRule();
        var doubleToFloatRule = new DoubleToFloatRewriteRule(compilation);
        var mathToMathFRule = new MathRewriteRule(compilation);        

        var typeRewriter = new TypeRewriter(semanticModel, nameRule, doubleToFloatRule, mathToMathFRule);
        syntaxRoot = typeRewriter.Visit(syntaxRoot);

        // Rewrite double literals to floats
        var literalRewriter = new LiteralRewriter(semanticModel);
        syntaxRoot = literalRewriter.Visit(syntaxRoot);

        if (syntaxRoot == null)
        {
            throw new Exception("Invalid rewrite");
        }

        var rewrittenText = syntaxRoot.NormalizeWhitespace().ToFullString();

        var fileText = $$"""            
            namespace {{template.Namespace ?? "CapriKit.Generated"}}
            {
                partial class {{template.ClassName}}
                {
            {{IndentString("        ", rewrittenText)}}
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
        return indent + string.Join("\n" + indent, lines);
    }
}



