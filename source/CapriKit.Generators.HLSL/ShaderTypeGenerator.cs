using Microsoft.CodeAnalysis;
using System.Runtime.Serialization;
using static CapriKit.Generators.HLSL.ConfigUtils;

namespace CapriKit.Generators.HLSL;

/// <summary>
/// Generates metdata that describe the shader, its entrypoints and slots and generates struct for types used.
/// </summary>
[Generator]
internal sealed class ShaderTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configurationProvider = CreateConfigurationProvider(context);

        var shadersProvider = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase))
            .Select(static (text, cancellationToken) => (text.Path, text.GetText(cancellationToken)));

        var provider = shadersProvider.Combine(configurationProvider);
        context.RegisterSourceOutput(provider, static (context, input) =>
        {
            var (shaderPath, shaderText) = input.Left;
            if (input.Right == null || input.Right.Length != 1)
            {
                ReportMissingConfigurationFile(context);
                return;
            }

            var (configPath, configText) = input.Right[0];
            GeneratorConfiguration config;
            try
            {
                config = ReadConfiguration(configPath, configText);
            }
            catch (SerializationException ex)
            {
                ReportMalformedConfigurationFile(context, ex);
                return;
            }

            if (ShaderClassBuilder.TryGenerateShader(shaderPath, shaderText, config, out var result))
            {
                var relativePath = SourceCodeUtils.GetRelativePath(config.AbsoluteContentRoot, shaderPath);
                var hintName = $"{SourceCodeUtils.CreateValidNamespace(relativePath)}.g.cs";
                context.AddSource(hintName, result);
            }
        });

        var shaderCountProvider = shadersProvider.Collect();
        context.RegisterSourceOutput(shaderCountProvider, static (context, shaders) =>
        {
            if (shaders.IsDefaultOrEmpty)
            {
                ReportNoOp(context);
            }
        });
    }





    private static void ReportNoOp(SourceProductionContext context)
    {
        var description = new DiagnosticDescriptor
                            (
                                "STG003",
                                $"No input files found",
                                $"Please double check that your .hlsl files are added to your project: `<ItemGroup><AdditionalFiles Include=\"shader.hlsl\"/></ItemGroup>`",
                                "SourceGeneration",
                                DiagnosticSeverity.Warning,
                                true
                            );
        var diagnostic = Diagnostic.Create(description, null);
        context.ReportDiagnostic(diagnostic);
    }
}
