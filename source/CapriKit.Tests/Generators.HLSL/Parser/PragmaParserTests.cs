using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class PragmaParserTests
{
    [Test]
    public async Task RecognizeEntryPointPragma()
    {
        var tokens = HLSLTokenizer.Parse("#pragma VertexShader");
        var directive = tokens.Single(t => t.Kind == TokenKind.Directive);

        var matched = PragmaParser.TryParseEntryPoint(directive, out var kind);

        await Assert.That(matched).IsTrue();
        await Assert.That(kind).IsEqualTo(EntryPointKind.VertexShader);
    }

    [Test]
    public async Task IgnoreUnknownPragma()
    {
        var tokens = HLSLTokenizer.Parse("#pragma SomethingElse");
        var directive = tokens.Single(t => t.Kind == TokenKind.Directive);

        var matched = PragmaParser.TryParseEntryPoint(directive, out _);

        await Assert.That(matched).IsFalse();
    }
}
