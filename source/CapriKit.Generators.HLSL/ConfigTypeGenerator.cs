using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
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
        context.RegisterSourceOutput(configurationProvider, static (context, result) =>
        {
            if (result.Configuration is { } config)
            {
                var type = GenerateConfigType(config);
                context.AddSource($"{ConfigurationFile}.cs", type);
            }
            else
            {
                ReportConfigDiagnostic(context, result);
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
