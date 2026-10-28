using CapriKit.Generators.AssetHandles;
using CapriKit.Tests.TestUtilities;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CapriKit.Tests.Generators.AssetHandles;

internal class AssetHandleGeneratorTests
{
    [Test]
    public async Task Execute()
    {
        IEnumerable<(string fileName, SourceText content)> additionalFiles =
       [
           new (@"C:/project/CapriKit.Generators.AssetHandles.json", SourceText.From(AssetsJson, Encoding.UTF8)),
           new (@"C:/project/Assets/Model.blend", SourceText.From(string.Empty, Encoding.UTF8)),
           new (@"C:/project/Assets/Strange.file", SourceText.From(string.Empty, Encoding.UTF8)),
       ];

        IEnumerable<(string fileName, SourceText content)> generatedFiles =
        [
           new (@"CapriKit.AssetHandles.g.cs", SourceText.From(Expected, Encoding.UTF8))
        ];

        await Assert.That(GeneratorSubject.OfType<AssetHandleGenerator>())
            .WithAdditionalFiles(additionalFiles)
            .Generates(generatedFiles);
    }

    private const string AssetsJson = """
        {
          "targetNamespace": "CapriKit.AssetHandles",
          "contentRoot": "Assets",
          "includedExtensions": [".blend"]
        }
        """;

    private const string Expected = """
        namespace CapriKit.AssetHandles;
        public static class Model
        {
            /// <summary>
            /// Path to Model.blend in the Assets directory
            /// </summary>
            public const string blend = "Model.blend";
        }

        """;
}
