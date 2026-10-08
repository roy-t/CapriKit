using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using static CapriKit.Generators.Shared.ConfigUtils;

namespace CapriKit.Generators.Shared;

/// <summary>
/// Base class for a generator that converts the configuration used for the generator to a type
/// so that users can read back the configuration without serialization.
/// Do not forget to mark the implementation with <code>[Generator]</code>.
/// </summary>
public abstract class ConfigTypeGenerator<T>(string ConfigurationFile) : IIncrementalGenerator
    where T : class
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configurationProvider = CreateConfigurationProvider<T>(context, ConfigurationFile);
        context.RegisterSourceOutput(configurationProvider, (context, result) =>
        {
            if (result.Configuration is { } config)
            {
                var type = GenerateConfigType(config);
                context.AddSource($"{ConfigurationFile}.cs", type);
            }
            else
            {
                ReportConfigDiagnostic(context, result, ConfigurationFile);
            }
        });
    }

    protected abstract SourceText GenerateConfigType(T config);
}
