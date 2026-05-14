using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CapriKit.Generators.HLSL;

[Generator]
public class ShaderTypeGenerator : IIncrementalGenerator
{
    private const string SettingsFile = "CapriKit.Generators.HLSL.json";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // TODO: how to handle include files
        // every file leads to exactly one class. So /path/to/includes.hlsl leads to
        // class Includes in namespace ...path.to
        // A type defined in includes.hlsl will be defined inside the Include class
        // For every include in a file we add `using static path.to.Includes;` so
        // that references work.
        // We use a caprikit.generators.hlsl.json file in to tell us about the root namespace
        // and the folder paths

        var settingsProvider = context.AdditionalTextsProvider
            .Where(static f => Path.GetFileName(f.Path).Equals(SettingsFile, StringComparison.OrdinalIgnoreCase))
            .Select(static (text, ct) => text.GetText(ct)?.ToString())
            .Collect();

        var shadersProvider = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase))
            .Select(static (text, cancellationToken) => (text.Path, text.GetText(cancellationToken)));

        // TODO: use this provider and read the settings for each file, report diagnostics if there are no settings
        var provider = shadersProvider.Combine(settingsProvider);

        context.RegisterSourceOutput(shadersProvider, static (outputContext, file) =>
        {
            var (path, text) = file;
            if (ShaderClassGenerator.TryGenerateShader(text, out var result))
            {
                outputContext.AddSource(path, result);
            }
        });
    }
}

public record GeneratorSettings(string TargetNamespace, string ContentRoot);

[Generator]
public class SharedTypesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context =>
        {
            context.AddEmbeddedAttributeDefinition();
            var definitions = """
            using System;
            using Microsoft.CodeAnalysis;

            namespace CapriKit.Generators.HLSL;            
            public interface IShaderMetadata
            {
                
            }
            """;


            var source = SourceText.From(definitions, Encoding.UTF8);
            context.AddSource("Definitions.g.cs", source);
        });
    }
}
