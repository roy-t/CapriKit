using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class StructureParserTests
{
    [Test]
    public async Task ParseStructure()
    {
        var source = """
            struct VS_INPUT
            {
                float3 position : POSITION;
                int id;
            };
            """;
        var state = new ParseState(HLSLTokenizer.Parse(source));

        var success = StructureParser.TryParse(state, out var structure);

        await Assert.That(success).IsTrue();
        await Assert.That(structure.Name).IsEqualTo("VS_INPUT");
        await Assert.That(structure.Members).Count().IsEqualTo(2);
        await Assert.That(structure.Members[0].Type).IsEqualTo("float3");
        await Assert.That(structure.Members[0].Name).IsEqualTo("position");
        await Assert.That(structure.Members[0].Semantic).IsEqualTo("POSITION");
        await Assert.That(structure.Members[1].Name).IsEqualTo("id");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task RejectsNonStructAndRewinds()
    {
        var state = new ParseState(HLSLTokenizer.Parse("cbuffer Globals { float4 tint; };"));

        var success = StructureParser.TryParse(state, out _);

        await Assert.That(success).IsFalse();
        await Assert.That(state.Peek().Value).IsEqualTo("cbuffer");
    }
}
