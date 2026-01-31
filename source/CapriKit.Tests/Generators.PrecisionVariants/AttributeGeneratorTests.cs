using CapriKit.Generators.PrecisionVariants;
using CapriKit.Tests.TestUtilities;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CapriKit.Tests.Generators.PrecisionVariants;

internal class AttributeGeneratorTests
{
    [Test]
    public async Task Execute()
    {
        string attributeSource = """
            using System;
            using Microsoft.CodeAnalysis;
            
            namespace CapriKit.PrecisionVariants
            {
                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateFloatVariant : Attribute { }
            
            }
            """;

        IEnumerable<(string fileName, SourceText content)> generatedFiles =
        [
            new (@"GeneratePrecisionVariantAttributes.g.cs", SourceText.From(attributeSource, Encoding.UTF8))
        ];

        await Assert.That(GeneratorSubject.OfType<AttributeGenerator>())
            .Generates(generatedFiles, null, true);
    }
}
