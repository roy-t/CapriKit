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
        var tokens = HLSLTokenizer.Parse(source);
        var state = new ParseState(tokens);

        var structure = StructureParser.Parse(state);

        await Assert.That(structure.Name).IsEqualTo("VS_INPUT");
        await Assert.That(structure.Fields).Count().IsEqualTo(2);
        await Assert.That(structure.Fields[0].Type).IsEqualTo("float3");
        await Assert.That(structure.Fields[0].Name).IsEqualTo("position");
        await Assert.That(structure.Fields[0].Semantic).IsEqualTo("POSITION");
        await Assert.That(structure.Fields[1].Name).IsEqualTo("id");
    }
}
