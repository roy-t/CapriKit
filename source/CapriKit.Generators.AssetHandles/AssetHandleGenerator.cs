using CapriKit.Generators.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using static CapriKit.Generators.Shared.ConfigUtils;
namespace CapriKit.Generators.AssetHandles;

[Generator]
internal sealed class AssetHandleGenerator : IIncrementalGenerator
{
    private record AssetHandle(string Path, string Directory, string FileName, string Extension, string Comment);

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
                Reporters.ReportConfigDiagnostic(context, input.Right, ConfigTypeGenerator.GeneratorConfigurationFile);
                return;
            }

            if (input.Left.Length == 0)
            {
                Reporters.ReportNoOp(context, "Please double check that your asset files are added to your project: `<ItemGroup><AdditionalFiles Include=\"asset.xyz\"/></ItemGroup>`");
                return;
            }

            var config = PatchConfig(input.Right);
            var extensions = new HashSet<string>(config.IncludedExtensions, StringComparer.OrdinalIgnoreCase);

            var handles = new List<AssetHandle>(input.Left.Length);
            foreach (var file in input.Left)
            {
                var path = file.Path;
                var extension = Path.GetExtension(path);
                if (extensions.Contains(extension))
                {
                    var relativePath = SourceCodeUtils.GetRelativePath(config.AbsoluteContentRoot, path);
                    var directory = Path.GetDirectoryName(relativePath);
                    var fileName = Path.GetFileNameWithoutExtension(relativePath);
                    var comment = $"Path to {relativePath} in the {config.ContentRoot} directory";

                    handles.Add(new AssetHandle(relativePath, directory, fileName, extension, comment));
                }
            }

            var groups = handles.GroupBy(h => h.Directory);
            foreach (var group in groups)
            {
                var example = group.First();
                var builder = new SourceCodeBuilder();
                var ns = $"{config.TargetNamespace}.{SourceCodeUtils.CreateValidNamespace(example.Directory)}";
                builder.WriteNamespace(ns);

                foreach (var item in group)
                {
                    builder.OpenClass(Modifiers.Public | Modifiers.Static, SourceCodeUtils.CreateValidTypeIdentifier(item.FileName));
                    builder.WriteSummaryComment(item.Comment);
                    builder.WriteField(Modifiers.Public | Modifiers.Const, "string", SourceCodeUtils.CreateValidVariableIdentifier(item.Extension), SourceCodeUtils.ToLiteral(item.Path));
                    builder.CloseBlock();
                }

                var hintName = $"{SourceCodeUtils.CreateValidNamespace(ns)}.g.cs";
                var source = SourceText.From(builder.Build(), Encoding.UTF8);
                context.AddSource(hintName, source);
            }
        });
    }

    private static GeneratorConfiguration PatchConfig(ConfigResult<GeneratorConfiguration> result)
    {
        var config = result.Configuration ?? throw new NullReferenceException(nameof(result.Configuration));
        var configDirectory = Path.GetDirectoryName(result.ConfigPath);
        var contentRoot = Path.Combine(configDirectory, config.ContentRoot);
        return config with { AbsoluteContentRoot = contentRoot };
    }
}
