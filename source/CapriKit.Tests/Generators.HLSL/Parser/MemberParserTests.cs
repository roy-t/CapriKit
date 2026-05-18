using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class MemberParserTests
{
    [Test]
    public async Task ParseMemberWithSemantic()
    {
        var tokens = HLSLTokenizer.Parse("float4 color : SV_TARGET0;");
        var state = new ParseState(tokens);

        var member = MemberParser.Parse(state);

        await Assert.That(member.Type).IsEqualTo("float4");
        await Assert.That(member.Name).IsEqualTo("color");
        await Assert.That(member.Semantic).IsEqualTo("SV_TARGET0");
    }

    [Test]
    public async Task ParseMemberWithoutSemantic()
    {
        var tokens = HLSLTokenizer.Parse("float a;");
        var state = new ParseState(tokens);

        var member = MemberParser.Parse(state);

        await Assert.That(member.Type).IsEqualTo("float");
        await Assert.That(member.Name).IsEqualTo("a");
        await Assert.That(member.Semantic).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ParseArrayMember()
    {
        var tokens = HLSLTokenizer.Parse("float4 mat[3][2];");
        var state = new ParseState(tokens);

        var member = MemberParser.Parse(state);

        await Assert.That(member.Type).IsEqualTo("float4");
        await Assert.That(member.Name).IsEqualTo("mat");
        await Assert.That(member.Semantic).IsEqualTo(string.Empty);
        await Assert.That(member.Dimensions).IsEquivalentTo(new uint[] { 3, 2 });
    }

    [Test]
    public async Task SkipInterpolationModifier()
    {
        var tokens = HLSLTokenizer.Parse("centroid float4 color : SV_TARGET0;");
        var state = new ParseState(tokens);

        var member = MemberParser.Parse(state);

        await Assert.That(member.Type).IsEqualTo("float4");
        await Assert.That(member.Name).IsEqualTo("color");
        await Assert.That(member.Modifiers).Count().IsEqualTo(1);
        await Assert.That(member.Modifiers[0]).IsEqualTo("centroid");
        await Assert.That(member.Semantic).IsEqualTo("SV_TARGET0");
    }
}
