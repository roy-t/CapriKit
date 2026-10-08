using CapriKit.Generators.Shared;
using Microsoft.CodeAnalysis;
using static CapriKit.Generators.Shared.ConfigUtils;
namespace CapriKit.Generators.AssetHandles;

[Generator]
internal sealed class AssetHandleGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configurationProvider = CreateConfigurationProvider<GeneratorConfiguration>(context, ConfigTypeGenerator.GeneratorConfigurationFile);
        var filesProvider = context.AdditionalTextsProvider
            .Collect();

        var provider = filesProvider.Combine(configurationProvider);
        context.RegisterSourceOutput(provider, static (context, input) =>
        {
            if (input.Right.Configuration == null || input.Right.Error != ConfigError.None)
            {
                ReportConfigDiagnostic(context, input.Right, ConfigTypeGenerator.GeneratorConfigurationFile);
                return;
            }

            if (input.Left.Length == 0)
            {
                ReportNoOp(context);
                return;
            }

            foreach (var file in input.Left)
            {
                var path = file.Path;
                var extensions = input.Right.Configuration.IncludedExtensions;
                if (extensions.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                {
                    // TODO: generate a source file with the path to the asset!
                }
            }
        });
    }

    private static void ReportNoOp(SourceProductionContext context)
    {
        var description = new DiagnosticDescriptor
                            (
                                "STG004",
                                $"No input files found",
                                $"Please double check that your asset files are added to your project: `<ItemGroup><AdditionalFiles Include=\"asset.xyz\"/></ItemGroup>`",
                                "SourceGeneration",
                                DiagnosticSeverity.Warning,
                                true
                            );
        var diagnostic = Diagnostic.Create(description, null);
        context.ReportDiagnostic(diagnostic);
    }
}
