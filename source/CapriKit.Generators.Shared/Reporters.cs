using Microsoft.CodeAnalysis;

namespace CapriKit.Generators.Shared;

public static class Reporters
{
    public static void ReportNoOp(SourceProductionContext context, string message)
    {
        var description = new DiagnosticDescriptor
                            (
                                "CGS404",
                                $"No input files found",
                                message,
                                "SourceGeneration",
                                DiagnosticSeverity.Warning,
                                true
                            );
        var diagnostic = Diagnostic.Create(description, null);
        context.ReportDiagnostic(diagnostic);
    }

    public static void ReportFailure(SourceProductionContext context, string id, string title, string message)
    {
        var description = new DiagnosticDescriptor
                            (
                                id, title, message,
                                "SourceGeneration",
                                DiagnosticSeverity.Error,
                                true
                            );
        var diagnostic = Diagnostic.Create(description, null);
        context.ReportDiagnostic(diagnostic);
    }

    public static void ReportConfigDiagnostic<T>(SourceProductionContext context, ConfigResult<T> result, string configurationFile)
        where T : class
    {
        var descriptor = result.Error switch
        {
            ConfigError.Missing => new DiagnosticDescriptor
            (
                "CGS405",
                $"Missing configuration file '{configurationFile}'",
                $"To be able to use this generator you need to add exactly one configuration file to your project: `<ItemGroup><AdditionalFiles Include=\"{configurationFile}\"/></ItemGroup> " +
                "to describe the namespace to generate files in and which folder to use as your asset root folder",
                "SourceGeneration",
                DiagnosticSeverity.Error,
                true
            ),
            ConfigError.Malformed => new DiagnosticDescriptor
            (
                "CGS406",
                $"Configuration file '{configurationFile}' is malformed",
                "Exception: {0}",
                "SourceGeneration",
                DiagnosticSeverity.Error,
                true
            ),
            _ => new DiagnosticDescriptor
            (
                "CGS500",
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
}
