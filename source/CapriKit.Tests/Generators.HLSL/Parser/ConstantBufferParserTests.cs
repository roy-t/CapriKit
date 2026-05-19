using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class ConstantBufferParserTests
{
    [Test]
    public async Task ParseConstantBufferWithRegister()
    {
        var source = """
            cbuffer Constants : register(b4)
            {
                float4x4 WorldViewProjection;
                float4 Color;
            };
            """;
        var state = new ParseState(HLSLTokenizer.Parse(source));

        var success = ConstantBufferParser.TryParse(state, out var buffer);

        await Assert.That(success).IsTrue();
        await Assert.That(buffer!.Name).IsEqualTo("Constants");
        await Assert.That(buffer.Register).IsEqualTo(4u);
        await Assert.That(buffer.Members).Count().IsEqualTo(2);
        await Assert.That(buffer.Members[0].Type).IsEqualTo("float4x4");
        await Assert.That(buffer.Members[0].Name).IsEqualTo("WorldViewProjection");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task ParseConstantBufferWithoutRegister()
    {
        var state = new ParseState(HLSLTokenizer.Parse("cbuffer Globals { float4 tint; };"));

        var success = ConstantBufferParser.TryParse(state, out var buffer);

        await Assert.That(success).IsTrue();
        await Assert.That(buffer!.Name).IsEqualTo("Globals");
        await Assert.That(buffer.Register).IsEqualTo(0u);
        await Assert.That(buffer.Members).Count().IsEqualTo(1);
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task RejectsNonCBufferAndRewinds()
    {
        var state = new ParseState(HLSLTokenizer.Parse("struct Foo { float a; };"));

        var success = ConstantBufferParser.TryParse(state, out _);

        await Assert.That(success).IsFalse();
        await Assert.That(state.Peek().Value).IsEqualTo("struct");
    }
}
