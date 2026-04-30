using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class SemanticParserTests
{
    [Test]
    public async Task ParseSemantic()
    {
        var tokens = HLSLTokenizer.Parse(": SV_TARGET0");
        var state = new ParseState(tokens);

        var semantic = SemanticParser.ParseSemantic(state);

        await Assert.That(semantic).IsEqualTo("SV_TARGET0");
    }

    [Test]
    public async Task EmptyStringWhenNoColon()
    {
        var tokens = HLSLTokenizer.Parse("foo");
        var state = new ParseState(tokens);

        var semantic = SemanticParser.ParseSemantic(state);

        await Assert.That(semantic).IsEqualTo(string.Empty);
    }
}
