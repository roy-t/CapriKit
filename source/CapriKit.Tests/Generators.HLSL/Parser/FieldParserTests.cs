using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class FieldParserTests
{
    [Test]
    public async Task ParseFieldWithSemantic()
    {
        var tokens = HLSLTokenizer.Parse("float4 color : SV_TARGET0;");
        var state = new ParseState(tokens);

        var field = FieldParser.Parse(state);

        await Assert.That(field.Type).IsEqualTo("float4");
        await Assert.That(field.Name).IsEqualTo("color");
        await Assert.That(field.Semantic).IsEqualTo("SV_TARGET0");
    }

    [Test]
    public async Task ParseFieldWithoutSemantic()
    {
        var tokens = HLSLTokenizer.Parse("float a;");
        var state = new ParseState(tokens);

        var field = FieldParser.Parse(state);

        await Assert.That(field.Type).IsEqualTo("float");
        await Assert.That(field.Name).IsEqualTo("a");
        await Assert.That(field.Semantic).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task SkipInterpolationModifier()
    {
        var tokens = HLSLTokenizer.Parse("centroid float4 color : SV_TARGET0;");
        var state = new ParseState(tokens);

        var field = FieldParser.Parse(state);

        await Assert.That(field.Type).IsEqualTo("float4");
        await Assert.That(field.Name).IsEqualTo("color");
        await Assert.That(field.Semantic).IsEqualTo("SV_TARGET0");
    }
}
