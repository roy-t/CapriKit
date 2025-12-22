using CapriKit.Generators.PrecisionVariants;
using CapriKit.Tests.TestUtilities;

namespace CapriKit.Tests.Generators.PrecisionVariants;

internal class AttributeGeneratorTests
{
    [Test]
    public async Task Execute_GeneratesAttributes()
    {
        var attributeGenerator = new AttributeGenerator();
        var result = attributeGenerator.Execute(string.Empty);

        // The source generator can add other files, like the embedded attribute definition
        await Assert.That(result.GeneratedFiles.Length).IsGreaterThanOrEqualTo(1);

        var expectedFile = result.GeneratedFiles.FirstOrDefault(f => f.FileName.EndsWith("GeneratePrecisionVariantAttributes.g.cs"));
        await Assert.That(expectedFile).IsNotDefault();

        string expectedFileContents = """
            using System;
            using Microsoft.CodeAnalysis;

            namespace CapriKit.PrecisionVariants
            {
                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateSByteVariant : Attribute { }

                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateByteVariant : Attribute { }

                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateShortVariant : Attribute { }

                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateUShortVariant : Attribute { }

                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateIntVariant : Attribute { }

                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateUIntVariant : Attribute { }

                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateLongVariant : Attribute { }

                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateULongVariant : Attribute { }

                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateNIntVariant : Attribute { }

                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateNUIntVariant : Attribute { }

                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateFloatVariant : Attribute { }

                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateDoubleVariant : Attribute { }

                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateDecimalVariant : Attribute { }
            }            
            """;

        await Assert.That(expectedFile!.Source.ToString())
            .IsEqualTo(expectedFileContents)
            .IgnoringWhitespace();
    }
}
