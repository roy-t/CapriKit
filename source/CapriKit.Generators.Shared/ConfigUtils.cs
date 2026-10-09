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
