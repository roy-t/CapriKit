using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class EntryPointParserTests
{
    [Test]
    public async Task ParseEntryPoint()
    {
        var source = """
            PS_INPUT VS(VS_INPUT input) : SV_POSITION
            {
                return input;
            }
            """;
        var tokens = HLSLTokenizer.Parse(source);
        var state = new ParseState(tokens);

        var entry = EntryPointParser.Parse(state, EntryPointKind.VertexShader);

        await Assert.That(entry.Kind).IsEqualTo(EntryPointKind.VertexShader);
        await Assert.That(entry.Name).IsEqualTo("VS");
        await Assert.That(entry.Semantic).IsEqualTo("SV_POSITION");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task ParseEntryPointWithoutSemantic()
    {
        var source = """
            void Main()
            {
                return;
            }
            """;
        var tokens = HLSLTokenizer.Parse(source);
        var state = new ParseState(tokens);

        var entry = EntryPointParser.Parse(state, EntryPointKind.ComputeShader);

        await Assert.That(entry.Kind).IsEqualTo(EntryPointKind.ComputeShader);
        await Assert.That(entry.Name).IsEqualTo("Main");
        await Assert.That(entry.Semantic).IsEqualTo(string.Empty);
        await Assert.That(state.IsAtEnd).IsTrue();
    }
}
