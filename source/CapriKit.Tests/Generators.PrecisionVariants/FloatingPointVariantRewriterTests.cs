using CapriKit.Generators.PrecisionVariants;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using CapriKit.Tests.TestUtilities;

namespace CapriKit.Tests.Generators.PrecisionVariants;

internal class FloatingPointVariantRewriterTests
{
    [Test]
    public async Task Visit_MethodDeclaration_ReplacesDoublesWithFloats()
    {
        var sut = new FloatingPointVariantRewriter([SyntaxKind.DoubleKeyword], SyntaxKind.FloatKeyword);
        var source = """
            public static double StandardError(double standardDeviation, double count)
            {
                return standardDeviation / Math.Sqrt(count);
            }
            """;

        var rewritten = sut.Execute(source);
        var expected = """
            public static float StandardError(float standardDeviation, float count)
            {
                return standardDeviation / Math.Sqrt(count);
            }
            """;

        await Assert.That(rewritten)
            .IsEqualTo(expected)
            .IgnoringWhitespace();
    }
}
