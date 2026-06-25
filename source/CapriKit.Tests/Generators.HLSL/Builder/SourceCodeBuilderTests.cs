using CapriKit.Generators.HLSL.Builder;

namespace CapriKit.Tests.Generators.HLSL.Builder;

internal class SourceCodeBuilderTests
{
    [Test]
    public async Task BuildIndentsNestsAndClosesOpenBlocks()
    {
        var builder = new SourceCodeBuilder();
        builder.OpenClass(Modifiers.Public, "Foo");
        builder.WriteAttribute("A", "1");
        builder.WriteField(Modifiers.Public | Modifiers.Static | Modifiers.ReadOnly, "int", "X", "5");

        var expected = """
            public class Foo
            {
                [A(1)]
                public static readonly int X = 5;
            }

            """;

        await Assert.That(Normalize(builder.Build())).IsEqualTo(Normalize(expected));
    }

    [Test]
    public async Task WriteSummaryCommentSplitsLinesIntoDocComment()
    {
        var builder = new SourceCodeBuilder();
        builder.WriteSummaryComment("Original Name: A\r\nSemantic: S");

        var expected = """
            /// <summary>
            /// Original Name: A
            /// Semantic: S
            /// </summary>

            """;

        await Assert.That(Normalize(builder.Build())).IsEqualTo(Normalize(expected));
    }

    private static string Normalize(string text) => text.ReplaceLineEndings("\n");
}
