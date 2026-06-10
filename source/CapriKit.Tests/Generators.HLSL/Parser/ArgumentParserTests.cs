using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class ArgumentParserTests
{
    [Test]
    public async Task ParseArgumentWithSemantic()
    {
        var tokens = HLSLTokenizer.Parse("float4 color : SV_TARGET0,");
        var state = new ParseState(tokens);

        var argument = ArgumentParser.Parse(state);

        await Assert.That(argument.Type).IsEqualTo("float4");
        await Assert.That(argument.Name).IsEqualTo("color");
        await Assert.That(argument.Semantic).IsEqualTo("SV_TARGET0");
    }

    [Test]
    public async Task ParseArgumentWithoutSemantic()
    {
        var tokens = HLSLTokenizer.Parse("float a,");
        var state = new ParseState(tokens);

        var argument = ArgumentParser.Parse(state);

        await Assert.That(argument.Type).IsEqualTo("float");
        await Assert.That(argument.Name).IsEqualTo("a");
        await Assert.That(argument.Semantic).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ParseArrayArgument()
    {
        var tokens = HLSLTokenizer.Parse("float4 mat[3][2],");
        var state = new ParseState(tokens);

        var argument = ArgumentParser.Parse(state);

        await Assert.That(argument.Type).IsEqualTo("float4");
        await Assert.That(argument.Name).IsEqualTo("mat");
        await Assert.That(argument.Semantic).IsEqualTo(string.Empty);
        await Assert.That(argument.Dimensions).IsEquivalentTo(new uint[] { 3, 2 });
    }

    [Test]
    public async Task SkipInputModifier()
    {
        var tokens = HLSLTokenizer.Parse("inout float4 color : SV_TARGET0;");
        var state = new ParseState(tokens);

        var argument = ArgumentParser.Parse(state);

        await Assert.That(argument.Type).IsEqualTo("float4");
        await Assert.That(argument.Name).IsEqualTo("color");
        await Assert.That(argument.Modifiers).Count().IsEqualTo(1);
        await Assert.That(argument.Modifiers[0]).IsEqualTo("inout");
        await Assert.That(argument.Semantic).IsEqualTo("SV_TARGET0");
    }

    [Test]
    public async Task ParseListStopsAtClosingParentheses()
    {
        var tokens = HLSLTokenizer.Parse("float a, float4 b : COLOR)");
        var state = new ParseState(tokens);

        var parser = ArgumentParser.CreateListParser();

        var arguments = new List<Argument>();
        var result = parser.TryParse(state, ref arguments);

        await Assert.That(result).IsTrue();
        await Assert.That(arguments).Count().IsEqualTo(2);
        await Assert.That(arguments[0].Name).IsEqualTo("a");
        await Assert.That(arguments[1].Name).IsEqualTo("b");
        await Assert.That(arguments[1].Semantic).IsEqualTo("COLOR");
        await Assert.That(state.Peek(TokenKind.Operator, ")")).IsTrue();
    }
}
