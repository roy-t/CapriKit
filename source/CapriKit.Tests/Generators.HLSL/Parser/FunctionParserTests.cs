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
        await Assert.That(entry.Arguments).Count().IsEqualTo(1);
        await Assert.That(entry.Arguments[0].Type).IsEqualTo("VS_INPUT");
        await Assert.That(entry.Arguments[0].Name).IsEqualTo("input");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task ParseFunctionWithComplexArguments()
    { 
        var source = """
            PS_INPUT VS(VS_INPUT input, in float4x4 boneMatrices[128], uint instanceId : SV_InstanceId)
            {
                return input;
            }
            """;
        var state = new ParseState(HLSLTokenizer.Parse(source));

        var success = FunctionParser.TryParse(state, out var entry);

        await Assert.That(success).IsTrue();
        await Assert.That(entry!.Name).IsEqualTo("VS");
        await Assert.That(entry.Arguments).Count().IsEqualTo(3);
        await Assert.That(entry.Arguments[0].Type).IsEqualTo("VS_INPUT");
        await Assert.That(entry.Arguments[0].Name).IsEqualTo("input");
        await Assert.That(entry.Arguments[1].Modifiers).Count().IsEqualTo(1);
        await Assert.That(entry.Arguments[1].Modifiers[0]).IsEqualTo("in");
        await Assert.That(entry.Arguments[1].Type).IsEqualTo("float4x4");
        await Assert.That(entry.Arguments[1].Name).IsEqualTo("boneMatrices");
        await Assert.That(entry.Arguments[1].Dimensions).Count().IsEqualTo(1);
        await Assert.That(entry.Arguments[1].Dimensions[0]).IsEqualTo(128u);
        await Assert.That(entry.Arguments[2].Type).IsEqualTo("uint");
        await Assert.That(entry.Arguments[2].Name).IsEqualTo("instanceId");
        await Assert.That(entry.Arguments[2].Semantic).IsEqualTo("SV_InstanceId");
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
