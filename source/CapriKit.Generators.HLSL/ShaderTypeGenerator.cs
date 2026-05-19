using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CapriKit.Generators.HLSL;

[Generator]
public class ShaderTypeGenerator : IIncrementalGenerator
{
    private const string ConfigurationFile = "CapriKit.Generators.HLSL.json";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // We use a caprikit.generators.hlsl.json file in to tell us about the root namespace
        // and the folder paths

        var configurationProvider = context.AdditionalTextsProvider
            .Where(static f => Path.GetFileName(f.Path).Equals(ConfigurationFile, StringComparison.OrdinalIgnoreCase))
            .Select(static (text, cancellationToken) => (text.Path, text.GetText(cancellationToken)))
            .Collect();

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
                var relativePath = SourceCodeUtils.GetRelativePath(config.ContentRoot, shaderPath);
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

    private static GeneratorConfiguration ReadConfiguration(string configPath, SourceText? configText)
    {
        if (configText == null)
        {
            throw new SerializationException($"Cannot deserialize `null`, check {configPath} is a valid configuration file");
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(configText.ToString()));
        var serializer = new DataContractJsonSerializer(typeof(GeneratorConfiguration));
        var dto = (GeneratorConfiguration)serializer.ReadObject(stream);
        var configDirectory = Path.GetDirectoryName(configPath);
        return new GeneratorConfiguration(dto.TargetNamespace, Path.Combine(configDirectory, dto.ContentRoot));
    }

    private static void ReportMissingConfigurationFile(SourceProductionContext context)
    {
        var description = new DiagnosticDescriptor
                            (
                                "STG001",
                                $"Missing configuration file '{ConfigurationFile}'",
                                $"To be able to use this generator you need to add exactly one configuration file to your project: `<ItemGroup><AdditionalFiles Include=\"{ConfigurationFile}\"/></ItemGroup> " +
                                "to describe the namespace to generate files in and which folder to use as your asset root folder",
                                "SourceGeneration",
                                DiagnosticSeverity.Error,
                                true
                            );
        var diagnostic = Diagnostic.Create(description, null);
        context.ReportDiagnostic(diagnostic);
    }

    private static void ReportMalformedConfigurationFile(SourceProductionContext context, SerializationException ex)
    {
        var description = new DiagnosticDescriptor
                            (
                                "STG002",
                                $"Configuration file '{ConfigurationFile}' is malformed",
                                $"Exception: {ex}",
                                "SourceGeneration",
                                DiagnosticSeverity.Error,
                                true
                            );
        var diagnostic = Diagnostic.Create(description, null);
        context.ReportDiagnostic(diagnostic);
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
