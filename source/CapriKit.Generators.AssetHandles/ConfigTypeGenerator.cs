using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using CapriKit.Generators.Shared;
using static CapriKit.Generators.Shared.SourceCodeUtils;

namespace CapriKit.Generators.AssetHandles;

/// <inheritdoc cref="ConfigTypeGenerator{T}"/>
[Generator]
internal sealed class ConfigTypeGenerator()
    : ConfigTypeGenerator<GeneratorConfiguration>(GeneratorConfigurationFile)
{
    public const string GeneratorConfigurationFile = "CapriKit.Generators.AssetHandles.json";

    protected override SourceText GenerateConfigType(GeneratorConfiguration config)
    {
        var builder = new SourceCodeBuilder();
        builder.WriteNamespace("CapriKit.Generators.HLSL");
        builder.OpenClass(Modifiers.Internal | Modifiers.Static, "Configuration");
        builder.WriteField(Modifiers.Public | Modifiers.Const, "string", "TargetNamespace", ToLiteral(config.TargetNamespace));
        builder.WriteField(Modifiers.Public | Modifiers.Const, "string", "ContentRoot", ToLiteral(config.ContentRoot));
        builder.WriteField(Modifiers.Public | Modifiers.Static | Modifiers.ReadOnly, "IReadOnlyList<string>", "IncludedExtensions", ToLiteralCollection(config.IncludedExtensions));
        builder.CloseBlock();
        return SourceText.From(builder.Build(), Encoding.UTF8);
    }
}
