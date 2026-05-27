using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.Serialization;
using System.Text;
using static CapriKit.Generators.HLSL.ConfigUtils;
using static CapriKit.Generators.HLSL.SourceCodeUtils;

namespace CapriKit.Generators.HLSL;

/// <summary>
/// Converts the configuration used for the generators in this project to a type
/// so that users can read back the configuration without serialization.
/// </summary>
[Generator]
internal sealed class ConfigTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configurationProvider = CreateConfigurationProvider(context);
        context.RegisterSourceOutput(configurationProvider, static (context, input) =>
        {
            if (input == null || input.Length != 1)
            {
                ReportMissingConfigurationFile(context);
                return;
            }

            var (configPath, configText) = input[0];
            GeneratorConfiguration config;
            try
            {
                config = ReadConfiguration(configPath, configText);
                var type = GenerateConfigType(config);
                context.AddSource($"{ConfigurationFile}.cs", type);

            }
            catch (SerializationException ex)
            {
                ReportMalformedConfigurationFile(context, ex);
                return;
            }
        });
    }

    private static SourceText GenerateConfigType(GeneratorConfiguration config)
    {
        var builder = new SourceCodeBuilder();
        builder.WriteNamespace("CapriKit.Generators.HLSL");
        builder.OpenClass(Modifiers.Internal | Modifiers.Static, "Configuration");
        builder.WriteField(Modifiers.Public | Modifiers.Const, "string", "TargetNamespace", ToLiteral(config.TargetNamespace));
        builder.WriteField(Modifiers.Public | Modifiers.Const, "string", "ContentRoot", ToLiteral(config.ContentRoot));
        return SourceText.From(builder.Build(), Encoding.UTF8);
    }


}
