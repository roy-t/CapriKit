using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class FunctionParserTests
{
    [Test]
    public async Task ParseFunction()
    {
        var source = """
            inline precise PS_INPUT VS(VS_INPUT input) : SV_POSITION
            {
                return input;
            }
            """;
        var state = new ParseState(HLSLTokenizer.Parse(source));

        var success = FunctionParser.TryParse(state, out var entry);

        await Assert.That(success).IsTrue();
        await Assert.That(entry!.Name).IsEqualTo("VS");
        await Assert.That(entry.Semantic).IsEqualTo("SV_POSITION");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task DoNotParseVariable()
    {
        var source = """
            float v = 4.0f;
            """;
        var state = new ParseState(HLSLTokenizer.Parse(source));

        var success = FunctionParser.TryParse(state, out var entry);

        await Assert.That(success).IsFalse();
        await Assert.That(state.Mark()).IsEqualTo(0);
    }
}
