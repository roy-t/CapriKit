using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class DirectiveParserTests
{
    [Test]
    public async Task RecognizeEntryPointPragma()
    {
        var tokens = HLSLTokenizer.Parse("#pragma VertexShader");
        var directive = tokens.Single(t => t.Kind == TokenKind.Directive);

        var matched = DirectiveParser.TryParseEntryPoint(directive, out var kind);

        await Assert.That(matched).IsTrue();
        await Assert.That(kind).IsEqualTo(EntryPointKind.VertexShader);
    }

    [Test]
    public async Task IgnoreUnknownPragma()
    {
        var tokens = HLSLTokenizer.Parse("#pragma SomethingElse");
        var directive = tokens.Single(t => t.Kind == TokenKind.Directive);

        var matched = DirectiveParser.TryParseEntryPoint(directive, out _);

        await Assert.That(matched).IsFalse();
    }

    [Test]
    public async Task RecognizeInclude()
    {
        var tokens = HLSLTokenizer.Parse("#include <std.io>");
        var directive = tokens.Single(t => t.Kind == TokenKind.Directive);

        var matched = DirectiveParser.TryParseInclude(directive);

        await Assert.That(matched).IsTrue();
    }
}
