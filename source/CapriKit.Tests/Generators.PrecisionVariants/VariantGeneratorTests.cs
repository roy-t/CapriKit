using CapriKit.Generators.PrecisionVariants;
using CapriKit.Tests.TestUtilities;
using Microsoft.CodeAnalysis;

namespace CapriKit.Tests.Generators.PrecisionVariants;

internal class VariantGeneratorTests
{
    [Test]
    public async Task Execute_MethodWithAttribute_IsRewritten()
    {
        var variantGenerator = new VariantGenerator();
        var source = """
            using System;
            using Microsoft.CodeAnalysis;
            
            namespace CapriKit.PrecisionVariants
            {
                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateFloatVariant : Attribute { }

                internal static partial class Statistics
                {
                    [CapriKit.PrecisionVariants.GenerateFloatVariant]
                    public static double StandardError(double standardDeviation, double count)
                    {
                        return standardDeviation / Math.Sqrt(count);
                    }
                }
            }                     
            """;

        var result = variantGenerator.Execute(source);       
        // The source generator can add other files, like the embedded attribute definition
        await Assert.That(result.GeneratedFiles.Length).IsGreaterThanOrEqualTo(1);
    }
}
