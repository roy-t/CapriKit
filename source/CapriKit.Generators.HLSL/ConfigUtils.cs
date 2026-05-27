using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CapriKit.Generators.HLSL;

internal static class ConfigUtils
{
    // We use a caprikit.generators.hlsl.json file in to tell us about the root namespace
    // and the folder paths
    public const string ConfigurationFile = "CapriKit.Generators.HLSL.json";

    public static IncrementalValueProvider<ImmutableArray<(string Path, SourceText?)>> CreateConfigurationProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.AdditionalTextsProvider
                    .Where(static f => Path.GetFileName(f.Path).Equals(ConfigurationFile, StringComparison.OrdinalIgnoreCase))
                    .Select(static (text, cancellationToken) => (text.Path, text.GetText(cancellationToken)))
                    .Collect();
    }


    public static GeneratorConfiguration ReadConfiguration(string configPath, SourceText? configText)
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

    public static void ReportMissingConfigurationFile(SourceProductionContext context)
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

    public static void ReportMalformedConfigurationFile(SourceProductionContext context, SerializationException ex)
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
}
