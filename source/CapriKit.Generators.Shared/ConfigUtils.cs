using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CapriKit.Generators.Shared;

public enum ConfigError
{
    None,
    Missing,
    Malformed
}

public sealed record ConfigResult<T>(T? Configuration, string ConfigPath, ConfigError Error, string? Message);

public static class ConfigUtils
{
    public static IncrementalValueProvider<ConfigResult<T>> CreateConfigurationProvider<T>(IncrementalGeneratorInitializationContext context, string configurationFile)
        where T : class
    {
        return context.AdditionalTextsProvider
                    .Where(f => Path.GetFileName(f.Path).Equals(configurationFile, StringComparison.OrdinalIgnoreCase))
                    .Select((text, cancellationToken) => (text.Path, Text: text.GetText(cancellationToken)))
                    .Collect()
                    .Select((files, _) => Parse<T>(files));
    }

    public static void ReportConfigDiagnostic<T>(SourceProductionContext context, ConfigResult<T> result, string configurationFile)
        where T : class
    {
        var descriptor = result.Error switch
        {
            ConfigError.Missing => new DiagnosticDescriptor
            (
                "STG001",
                $"Missing configuration file '{configurationFile}'",
                $"To be able to use this generator you need to add exactly one configuration file to your project: `<ItemGroup><AdditionalFiles Include=\"{configurationFile}\"/></ItemGroup> " +
                "to describe the namespace to generate files in and which folder to use as your asset root folder",
                "SourceGeneration",
                DiagnosticSeverity.Error,
                true
            ),
            ConfigError.Malformed => new DiagnosticDescriptor
            (
                "STG002",
                $"Configuration file '{configurationFile}' is malformed",
                "Exception: {0}",
                "SourceGeneration",
                DiagnosticSeverity.Error,
                true
            ),
            _ => new DiagnosticDescriptor
            (
                "STG003",
                $"Unexpected error parsing configuration file '{configurationFile}'",
                "Exception: {0}",
                "SourceGeneration",
                DiagnosticSeverity.Error,
                true
            ),
        };

        if (descriptor is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, null, result.Message));
        }
    }

    private static ConfigResult<T> Parse<T>(ImmutableArray<(string Path, SourceText? Text)> files)
        where T : class
    {
        if (files.Length != 1)
        {
            return new ConfigResult<T>(null, string.Empty, ConfigError.Missing, null);
        }

        try
        {
            var config = ReadConfiguration<T>(files[0].Path, files[0].Text);
            return new ConfigResult<T>(config, files[0].Path, ConfigError.None, null);
        }
        catch (SerializationException ex)
        {
            return new ConfigResult<T>(null, string.Empty, ConfigError.Malformed, ex.ToString());
        }
    }

    private static T ReadConfiguration<T>(string configPath, SourceText? configText)
        where T : class
    {
        if (configText == null)
        {
            throw new SerializationException($"Cannot deserialize `null`, check {configPath} is a valid configuration file");
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(configText.ToString()));
        var serializer = new DataContractJsonSerializer(typeof(T));
        return (T)serializer.ReadObject(stream);
    }
}
