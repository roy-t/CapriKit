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
            using System.Collections.Generic;
            using Microsoft.CodeAnalysis;

            namespace CapriKit.PrecisionVariants
            {
                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateFloatVariant : Attribute { }
                internal static partial class TestClass
                {
                    [CapriKit.PrecisionVariants.GenerateFloatVariant]
                    public static double TestMethod(List<double> argument)
                    {
                        return Math.Sin(1.0);                        
                    }
                }
            }                     
            """;

        var result = variantGenerator.Execute(source);
        // The source generator can add other files, like the embedded attribute definition
        await Assert.That(result.GeneratedFiles.Length).IsGreaterThanOrEqualTo(1);
    }

    // TODO: it would be easier to test source generators using Microsoft.CodeAnalysis.Testing
    // but that doesn't work with the latest source generators
    // see: https://github.com/dotnet/roslyn-sdk/issues/1241
    // and: https://github.com/dotnet/roslyn-sdk/blob/main/src/Microsoft.CodeAnalysis.Testing/README.md

    //[Test]
    //public async Task Execute_MethodWithAttribute_IsRewritten2()
    //{
    //    var source = """
    //            using System;
    //            using Microsoft.CodeAnalysis;

    //            namespace CapriKit.PrecisionVariants
    //            {
    //                [Embedded]
    //                [AttributeUsage(AttributeTargets.Method)]
    //                internal sealed class GenerateFloatVariant : Attribute { }

    //                internal static partial class Statistics
    //                {
    //                    [CapriKit.PrecisionVariants.GenerateFloatVariant]
    //                    public static double StandardError(double standardDeviation, double count)
    //                    {
    //                        return standardDeviation / Math.Sqrt(count);
    //                    }
    //                }
    //            }                     
    //            """;

    //    var test = new CSharpSourceGeneratorTest<VariantGenerator, FooVerifier>
    //    {
    //        TestState =
    //        {
    //            Sources = { source },
    //            GeneratedSources =
    //            {
    //                (typeof(VariantGenerator), "Hello.g.cs", "")
    //            }
    //        }
    //    };

    //    await test.RunAsync();
    //}
}
