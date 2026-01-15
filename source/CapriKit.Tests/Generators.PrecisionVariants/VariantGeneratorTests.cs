using CapriKit.Generators.PrecisionVariants;
using CapriKit.Tests.TestUtilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CapriKit.Tests.Generators.PrecisionVariants;

internal class VariantGeneratorTests
{
    [Test]
    public async Task Execute_ComplexTypeVariants_RewritesAllTypesToFullyQualifiedNames()
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

        string inputSource = """
            using System;
            using System.Collections.Generic;

            namespace Test.Namespace
            {
                using CapriKit.PrecisionVariants;

                internal static partial class TestClass
                {
                    [GenerateFloatVariant]
                    internal static Array? TestMethod(double init, object obj, List<Array> generic, Array[] array, (Array a, Array b) tuple)
                    {
                        double sum = 0.0;
                        double? x = Math.Sin(1.0);
                        unsafe
                        {
                            Array* pointer = null;
                        }
                        return null;
                    }
                }
            }
            """;

        var expectedGeneratedSource = """
                namespace Test.Namespace
                {
                    partial class TestClass
                    {
                        [global::CapriKit.PrecisionVariants.GenerateFloatVariant]
                        internal static global::System.Array? TestMethod(float init, object obj, global::System.Collections.Generic.List<global::System.Array> generic, global::System.Array[] array, (global::System.Array a, global::System.Array b) tuple)
                        {
                            float sum = 0F;
                            float? x = global::System.MathF.Sin(1F);
                            unsafe
                            {
                                global::System.Array* pointer = null;
                            }

                            return null;
                        }
                    }
                }
                """;

        IEnumerable<(string fileName, SourceText content)> inputFiles =
        [
            new (@"PrecisionVariants.cs", SourceText.From(attributeSource, Encoding.UTF8)),
            new (@"Input.cs", SourceText.From(inputSource, Encoding.UTF8))
        ];

        IEnumerable<(string fileName, SourceText content)> generatedFiles =
        [
            new (@"TestClass_TestMethod_DoubleToFloat.g.cs", SourceText.From(expectedGeneratedSource, Encoding.UTF8))
        ];

        await Assert.That(GeneratorSubject.OfType<VariantGenerator>())
            .WithSources(inputFiles, true)
            .Generates(generatedFiles);
    }
}
