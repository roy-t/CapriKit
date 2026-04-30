using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class IncludeParserTests
{
    [Test]
    public async Task ParseSystemInclude()
    {
        var source = """
            #include <std.io>
            """;

        var tokens = HLSLTokenizer.Parse(source);
        var state = new ParseState(tokens);

        var include = IncludeParser.Parse(state);

        await Assert.That(include.Path).IsEqualTo("std.io");
        await Assert.That(include.Kind).IsEqualTo(IncludeKind.System);
    }

    [Test]
    public async Task ParseLocalInclude()
    {
        var source = """
            #include "defines.hlsl"
            """;

        var tokens = HLSLTokenizer.Parse(source);
        var state = new ParseState(tokens);

        var include = IncludeParser.Parse(state);

        await Assert.That(include.Path).IsEqualTo("defines.hlsl");
        await Assert.That(include.Kind).IsEqualTo(IncludeKind.Local);
    }
}
