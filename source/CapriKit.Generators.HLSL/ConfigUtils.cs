using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CapriKit.Generators.HLSL;

internal enum ConfigError
{
    None,
    Missing,
    Malformed
}

internal sealed record ConfigResult(GeneratorConfiguration? Configuration, ConfigError Error, string? Message);

internal static class ConfigUtils
{
    // We use a caprikit.generators.hlsl.json file in to tell us about the root namespace
    // and the folder paths
    public const string ConfigurationFile = "CapriKit.Generators.HLSL.json";

    public static IncrementalValueProvider<ConfigResult> CreateConfigurationProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.AdditionalTextsProvider
                    .Where(static f => Path.GetFileName(f.Path).Equals(ConfigurationFile, StringComparison.OrdinalIgnoreCase))
                    .Select(static (text, cancellationToken) => (text.Path, Text: text.GetText(cancellationToken)))
                    .Collect()
                    .Select(static (files, _) => Parse(files));
    }

    public static void ReportConfigDiagnostic(SourceProductionContext context, ConfigResult result)
    {
        var descriptor = result.Error switch
        {
            ConfigError.Missing => new DiagnosticDescriptor
            (
                "STG001",
                $"Missing configuration file '{ConfigurationFile}'",
                $"To be able to use this generator you need to add exactly one configuration file to your project: `<ItemGroup><AdditionalFiles Include=\"{ConfigurationFile}\"/></ItemGroup> " +
                "to describe the namespace to generate files in and which folder to use as your asset root folder",
                "SourceGeneration",
                DiagnosticSeverity.Error,
                true
            ),
            ConfigError.Malformed => new DiagnosticDescriptor
            (
                "STG002",
                $"Configuration file '{ConfigurationFile}' is malformed",
                "Exception: {0}",
                "SourceGeneration",
                DiagnosticSeverity.Error,
                true
            ),
            _ => null
        };

        if (descriptor is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, null, result.Message));
        }
    }

    private static ConfigResult Parse(ImmutableArray<(string Path, SourceText? Text)> files)
    {
        if (files.Length != 1)
        {
            return new ConfigResult(null, ConfigError.Missing, null);
        }

        try
        {
            var config = ReadConfiguration(files[0].Path, files[0].Text);
            return new ConfigResult(config, ConfigError.None, null);
        }
        catch (SerializationException ex)
        {
            return new ConfigResult(null, ConfigError.Malformed, ex.ToString());
        }
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
        var absoluteConfigRoot = Path.Combine(configDirectory, dto.ContentRoot);
        return new GeneratorConfiguration(dto.TargetNamespace, dto.ContentRoot, absoluteConfigRoot);
    }
}
