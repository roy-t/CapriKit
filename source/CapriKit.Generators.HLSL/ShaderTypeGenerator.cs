using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;
using Microsoft.CodeAnalysis;
using static CapriKit.Generators.HLSL.ConfigUtils;

namespace CapriKit.Generators.HLSL;

/// <summary>
/// Generates metadata that describe the shader, its entrypoints and slots and generates struct for types used.
/// </summary>
[Generator]
internal sealed class ShaderTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configurationProvider = CreateConfigurationProvider(context);

        var shadersProvider = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase))
            .Select(static (text, cancellationToken) => BuildMetaData(text, cancellationToken))
            .Collect();

        var provider = shadersProvider.Combine(configurationProvider);
        context.RegisterSourceOutput(provider, static (context, input) =>
        {
            if (input.Right.Configuration == null || input.Right.Error != ConfigError.None)
            {
                ReportConfigDiagnostic(context, input.Right);
                return;
            }

            if (input.Left.Length == 0)
            {
                ReportNoOp(context);
                return;
            }

            var config = input.Right.Configuration;
            var shaderIndex = new ShaderIndex(input.Left);

            foreach (var (path, shader) in input.Left)
            {
                if (ShaderClassBuilder.TryGenerateShader(path, shader, shaderIndex, config, out var result))
                {
                    var relativePath = SourceCodeUtils.GetRelativePath(config.AbsoluteContentRoot, path);
                    var hintName = $"{SourceCodeUtils.CreateValidNamespace(relativePath)}.g.cs";
                    context.AddSource(hintName, result);
                }
                else
                {
                    ReportFailure(context, path);
                }
            }
        });
    }

    private static (string path, ShaderMetadata? shader) BuildMetaData(AdditionalText text, CancellationToken cancellationToken)
    {
        var shaderText = text.GetText(cancellationToken);
        if (shaderText == null)
        {
            return (text.Path, null);
        }
        var tokens = HLSLTokenizer.Parse(shaderText.ToString());
        var metadata = HLSLParser.Parse(tokens);
        return (text.Path, metadata);
    }

    private static void ReportNoOp(SourceProductionContext context)
    {
        var description = new DiagnosticDescriptor
                            (
                                "STG004",
                                $"No input files found",
                                $"Please double check that your .hlsl files are added to your project: `<ItemGroup><AdditionalFiles Include=\"shader.hlsl\"/></ItemGroup>`",
                                "SourceGeneration",
                                DiagnosticSeverity.Warning,
                                true
                            );
        var diagnostic = Diagnostic.Create(description, null);
        context.ReportDiagnostic(diagnostic);
    }

    private static void ReportFailure(SourceProductionContext context, string path)
    {
        var description = new DiagnosticDescriptor
                            (
                                "STG004",
                                $"Failed to Generate Shader Type",
                                $"Could not generate shader type for: {path}",
                                "SourceGeneration",
                                DiagnosticSeverity.Warning,
                                true
                            );
        var diagnostic = Diagnostic.Create(description, null);
        context.ReportDiagnostic(diagnostic);
    }
}
