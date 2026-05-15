using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class EntryPointParserTests
{
    [Test]
    public async Task ParseEntryPoint()
    {
        var source = """
            #pragma VertexShader
            PS_INPUT VS(VS_INPUT input) : SV_POSITION
            {
                return input;
            }
            """;
        var state = new ParseState(HLSLTokenizer.Parse(source));

        var success = EntryPointParser.TryParse(state, out var entry);

        await Assert.That(success).IsTrue();
        await Assert.That(entry!.Kind).IsEqualTo(EntryPointKind.VertexShader);
        await Assert.That(entry.Name).IsEqualTo("VS");
        await Assert.That(entry.Semantic).IsEqualTo("SV_POSITION");
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task ParseEntryPointWithoutSemantic()
    {
        var source = """
            #pragma ComputeShader
            void Main()
            {
                return;
            }
            """;
        var state = new ParseState(HLSLTokenizer.Parse(source));

        var success = EntryPointParser.TryParse(state, out var entry);

        await Assert.That(success).IsTrue();
        await Assert.That(entry!.Kind).IsEqualTo(EntryPointKind.ComputeShader);
        await Assert.That(entry.Name).IsEqualTo("Main");
        await Assert.That(entry.Semantic).IsEqualTo(string.Empty);
        await Assert.That(state.IsAtEnd).IsTrue();
    }

    [Test]
    public async Task RejectsUnknownPragmaAndRewinds()
    {
        var state = new ParseState(HLSLTokenizer.Parse("#pragma SomethingElse"));

        var success = EntryPointParser.TryParse(state, out _);

        await Assert.That(success).IsFalse();
        await Assert.That(state.Peek().Value).IsEqualTo("#pragma SomethingElse");
    }

    [Test]
    public async Task RejectsIncludeDirectiveAndRewinds()
    {
        var state = new ParseState(HLSLTokenizer.Parse("#include <std.io>"));

        var success = EntryPointParser.TryParse(state, out _);

        await Assert.That(success).IsFalse();
        await Assert.That(state.Peek().Value).IsEqualTo("#include <std.io>");
    }
}
