using CapriKit.Generators.HLSL;
using CapriKit.Tests.TestUtilities;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CapriKit.Tests.Generators.HLSL;

internal class ConfigTypeGeneratorTests
{
    [Test]
    public async Task Execute()
    {
        IEnumerable<(string fileName, SourceText content)> additionalFiles =
       [
           new (@"C:/project/CapriKit.Generators.HLSL.json", SourceText.From(ConfigJson, Encoding.UTF8))
       ];

        IEnumerable<(string fileName, SourceText content)> generatedFiles =
        [
           new (@"CapriKit.Generators.HLSL.json.cs", SourceText.From(Expected, Encoding.UTF8))
        ];

        await Assert.That(GeneratorSubject.OfType<ConfigTypeGenerator>())
            .WithAdditionalFiles(additionalFiles)
            .Generates(generatedFiles);
    }

    private const string ConfigJson = """
        {
          "targetNamespace": "MyGame.Shaders",
          "contentRoot": "Assets/Shaders"
        }
        """;

    private const string Expected = """
        namespace CapriKit.Generators.HLSL;
        internal static class Configuration
        {
            public const string TargetNamespace = "MyGame.Shaders";
            public const string ContentRoot = "Assets/Shaders";
        }
        
        """;
}
