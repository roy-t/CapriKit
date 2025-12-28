using CapriKit.Generators.PrecisionVariants;
using CapriKit.Tests.TestUtilities;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace CapriKit.Tests.Generators.PrecisionVariants;

internal class VariantGeneratorTests
{
    private static readonly string AttributeSource = """
            using System;
            using System.Collections.Generic;            
            
            namespace CapriKit.PrecisionVariants
            {
                [Embedded]
                [AttributeUsage(AttributeTargets.Method)]
                internal sealed class GenerateFloatVariant : Attribute { }            
            }                     
            """;    

    [Test]
    public async Task Execute_AllTypeVariants_RewritesAllTypesToFullyQualifiedNames()
    {
        // Covers all type syntaxes:

        // Simple	IdentifierNameSyntax, PredefinedTypeSyntax
        // Generic  GenericNameSyntax
        // Nullable NullableTypeSyntax
        // Array    ArrayTypeSyntax
        // Tuple    TupleTypeSyntax
        // Pointer  PointerTypeSyntax


        var variantGenerator = new VariantGenerator();
        var source = AttributeSource + """            
            namespace Test.Namespace
            {
                using CapriKit.PrecisionVariants;

                internal static partial class TestClass
                {
                    [GenerateFloatVariant]
                    internal static Array? TestMethod(object obj, List<Array> generic, Array[] array, (Array a, Array b) tuple)
                    {
                        unsafe
                        {
                            Array* pointer = null;
                        }
                        return null;
                    }
                }
            }                     
            """;
        var result = variantGenerator.Execute(source);

        await Assert.That(result.Diagnostics.Length).IsZero();
        await Assert.That(result.GeneratedFiles.Length).IsEqualTo(1);

        var expected = """
            namespace Test.Namespace
            {
                partial class TestClass
                {
                    [global::CapriKit.PrecisionVariants.GenerateFloatVariant]
                    internal static global::System.Array? TestMethod(object obj, global::System.Collections.Generic.List<global::System.Array> generic, global::System.Array[] array, (global::System.Array a, global::System.Array b) tuple)
                    {
                        unsafe
                        {
                            global::System.Array* pointer = null;
                        }

                        return null;
                    }
                }
            }
            """;

        var generatedFile = result.GeneratedFiles[0].Source.ToString();
        await Assert.That(generatedFile).IsEqualTo(expected).IgnoringWhitespace();
    }

    [Test]    
    public async Task Execute_IsRewritten()
    {
        var variantGenerator = new VariantGenerator();
        var source = AttributeSource + """            
            namespace Test.Namespace
            {
                using CapriKit.PrecisionVariants;

                internal static partial class TestClass
                {
                    [GenerateFloatVariant]
                    public static double TestMethod(List<double> argument)
                    {
                        return Math.Sin(1.0);                        
                    }
                }
            }                     
            """;
        var result = variantGenerator.Execute(source);
        
        await Assert.That(result.Diagnostics.Length).IsZero();
        await Assert.That(result.GeneratedFiles.Length).IsEqualTo(1);

        var expected = """
            namespace Test.Namespace
            {
                partial class TestClass
                {
                    [global::CapriKit.PrecisionVariants.GenerateFloatVariant]
                    public static float TestMethod(global::System.Collections.Generic.List<float> argument)
                    {
                        return global::System.MathF.Sin(1f);
                    }
                }
            }
            """;

        var generatedFile = result.GeneratedFiles[0].Source.ToString();
        await Assert.That(generatedFile).IsEqualTo(expected).IgnoringWhitespace();
    }

    // TODO: Make testing the output of a single method easier and come up with a better method name to describe things
    [Test]
    public async Task Execute_MethodWithDoubleArgument_IsRewrittenToFloatArgument()
    {
        var variantGenerator = new VariantGenerator();
        var source = AttributeSource + """            
            namespace Test.Namespace
            {                
                internal static partial class TestClass
                {
                    [CapriKit.PrecisionVariants.GenerateFloatVariant]
                    public static double TestMethod(double argument)
                    {
                        return argument;           
                    }
                }
            }                     
            """;
        var result = variantGenerator.Execute(source);

        await Assert.That(result.Diagnostics.Length).IsZero();
        await Assert.That(result.GeneratedFiles.Length).IsEqualTo(1);

        var expected = """
            namespace Test.Namespace
            {
                partial class TestClass
                {
                    [global::CapriKit.PrecisionVariants.GenerateFloatVariant]
                    public static float TestMethod(float argument)
                    {
                        return argument;
                    }
                }
            }
            """;

        var generatedFile = result.GeneratedFiles[0].Source.ToString();
        await Assert.That(generatedFile).IsEqualTo(expected).IgnoringWhitespace();
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
