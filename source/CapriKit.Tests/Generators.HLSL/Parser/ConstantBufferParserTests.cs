using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class ConstantBufferParserTests
{
    [Test]
    public async Task ParseConstantBufferWithRegister()
    {
        var source = """
            cbuffer Constants : register(b0)
            {
                float4x4 WorldViewProjection;
                float4 Color;
            };
            """;
        var tokens = HLSLTokenizer.Parse(source);
        var state = new ParseState(tokens);

        var buffer = ConstantBufferParser.Parse(state);

        await Assert.That(buffer.Name).IsEqualTo("Constants");
        await Assert.That(buffer.Register).IsEqualTo("b0");
        await Assert.That(buffer.Fields).Count().IsEqualTo(2);
        await Assert.That(buffer.Fields[0].Type).IsEqualTo("float4x4");
        await Assert.That(buffer.Fields[0].Name).IsEqualTo("WorldViewProjection");
    }

    [Test]
    public async Task ParseConstantBufferWithoutRegister()
    {
        var tokens = HLSLTokenizer.Parse("cbuffer Globals { float4 tint; };");
        var state = new ParseState(tokens);

        var buffer = ConstantBufferParser.Parse(state);

        await Assert.That(buffer.Name).IsEqualTo("Globals");
        await Assert.That(buffer.Register).IsEqualTo(string.Empty);
        await Assert.That(buffer.Fields).Count().IsEqualTo(1);
    }
}
