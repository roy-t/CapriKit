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
            .Select(static (text, cancellationToken) => (text.Path, text.GetText(cancellationToken)));

        var provider = shadersProvider.Combine(configurationProvider);
        context.RegisterSourceOutput(provider, static (context, input) =>
        {
            var (shaderPath, shaderText) = input.Left;
            if (input.Right.Configuration is not { } config)
            {
                return;
            }

            if (ShaderClassBuilder.TryGenerateShader(shaderPath, shaderText, config, out var result))
            {
                var relativePath = SourceCodeUtils.GetRelativePath(config.AbsoluteContentRoot, shaderPath);
                var hintName = $"{SourceCodeUtils.CreateValidNamespace(relativePath)}.g.cs";
                context.AddSource(hintName, result);
            }
        });

        // Project-wide diagnostics, warn when there are no shaders at all,
        // otherwise require a valid configuration to generate against.
        var hasShadersProvider = shadersProvider.Collect().Select(static (shaders, _) => !shaders.IsDefaultOrEmpty);
        context.RegisterSourceOutput(hasShadersProvider.Combine(configurationProvider), static (context, input) =>
        {
            var (hasShaders, result) = input;
            if (!hasShaders)
            {
                ReportNoOp(context);
                return;
            }

            ReportConfigDiagnostic(context, result);
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
