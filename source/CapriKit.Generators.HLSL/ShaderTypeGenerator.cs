using CapriKit.Generators.HLSL.Builder;
using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;
using CapriKit.Generators.Shared;
using Microsoft.CodeAnalysis;
using static CapriKit.Generators.Shared.ConfigUtils;

namespace CapriKit.Generators.HLSL;

/// <summary>
/// Generates metadata that describe the shader, its entry points and slots and generates struct for types used.
/// </summary>
[Generator]
internal sealed class ShaderTypeGenerator : IIncrementalGenerator
{

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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configurationProvider = CreateConfigurationProvider<GeneratorConfiguration>(context, ConfigTypeGenerator.GeneratorConfigurationFile);

        var shadersProvider = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase))
            .Select(static (text, cancellationToken) => BuildMetaData(text, cancellationToken))
            .Collect();

        var provider = shadersProvider.Combine(configurationProvider);
        context.RegisterSourceOutput(provider, static (context, input) =>
        {
            if (input.Right.Configuration == null || input.Right.Error != ConfigError.None)
            {
                Reporters.ReportConfigDiagnostic(context, input.Right, ConfigTypeGenerator.GeneratorConfigurationFile);
                return;
            }

            if (input.Left.Length == 0)
            {
                Reporters.ReportNoOp(context, "Please double check that your .hlsl files are added to your project: `<ItemGroup><AdditionalFiles Include=\"shader.hlsl\"/></ItemGroup>`");
                return;
            }

            var config = PatchConfig(input.Right);
            var includeResolver = new IncludeResolver(input.Left);


            foreach (var (path, shader) in input.Left)
            {
                if (ShaderClassBuilder.TryGenerateShader(path, shader, includeResolver, config, out var result))
                {
                    var relativePath = SourceCodeUtils.GetRelativePath(config.AbsoluteContentRoot, path);
                    var hintName = $"{SourceCodeUtils.CreateValidNamespace(relativePath)}.g.cs";
                    context.AddSource(hintName, result);
                }
                else
                {
                    Reporters.ReportFailure(context, "STG500",
                                $"Failed to Generate Shader Type",
                                $"Could not generate shader type for: {path}");
                }
            }
        });
    }

    private static GeneratorConfiguration PatchConfig(ConfigResult<GeneratorConfiguration> result)
    {
        var config = result.Configuration ?? throw new NullReferenceException(nameof(result.Configuration));
        var configDirectory = Path.GetDirectoryName(result.ConfigPath);
        var contentRoot = Path.Combine(configDirectory, config.ContentRoot);
        return config with { AbsoluteContentRoot = contentRoot };
    }
}
