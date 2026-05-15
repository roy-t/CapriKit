using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class IncludeParserTests
{
    [Test]
    public async Task ParseSystemInclude()
    {
        var state = new ParseState(HLSLTokenizer.Parse("#include <std.io>"));

        var success = IncludeParser.TryParse(state, out var include);

        await Assert.That(success).IsTrue();
        await Assert.That(include!.Path).IsEqualTo("std.io");
        await Assert.That(include.Kind).IsEqualTo(IncludeKind.System);
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task ParseLocalInclude()
    {
        var state = new ParseState(HLSLTokenizer.Parse("#include \"defines.hlsl\""));

        var success = IncludeParser.TryParse(state, out var include);

        await Assert.That(success).IsTrue();
        await Assert.That(include!.Path).IsEqualTo("defines.hlsl");
        await Assert.That(include.Kind).IsEqualTo(IncludeKind.Local);
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task RejectsNonIncludeAndRewinds()
    {
        var state = new ParseState(HLSLTokenizer.Parse("#pragma VertexShader"));

        var success = IncludeParser.TryParse(state, out _);

        await Assert.That(success).IsFalse();
        await Assert.That(state.Peek().Value).IsEqualTo("#pragma VertexShader");
    }
}
