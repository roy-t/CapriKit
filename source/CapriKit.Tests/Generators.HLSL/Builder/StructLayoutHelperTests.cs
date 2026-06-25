using CapriKit.Generators.HLSL.Builder;
using CapriKit.Generators.HLSL.Parser;

namespace CapriKit.Tests.Generators.HLSL.Builder;

internal class StructLayoutHelperTests
{
    [Test]
    public async Task Align16RoundsUpToMultipleOf16()
    {
        await Assert.That(StructLayoutHelper.Align16(0u)).IsEqualTo(0u);
        await Assert.That(StructLayoutHelper.Align16(12u)).IsEqualTo(16u);
        await Assert.That(StructLayoutHelper.Align16(16u)).IsEqualTo(16u);
        await Assert.That(StructLayoutHelper.Align16(17u)).IsEqualTo(32u);
    }

    [Test]
    public async Task FlattenMultipliesDimensionsAndDefaultsToOne()
    {
        await Assert.That(StructLayoutHelper.Flatten([3u, 2u])).IsEqualTo(6u);
        await Assert.That(StructLayoutHelper.Flatten([])).IsEqualTo(1u);
    }

    [Test]
    public async Task SplitSemanticInTextAndIndexSeparatesTrailingDigits()
    {
        await Assert.That(StructLayoutHelper.SplitSemanticInTextAndIndex("POSITION4")).IsEqualTo(("POSITION", 4u));
        await Assert.That(StructLayoutHelper.SplitSemanticInTextAndIndex("")).IsEqualTo(("", 0u));
    }

    [Test]
    public async Task DocumentMemberDescribesKnownFacts()
    {
        var member = new Member("float3", "A", "SV_SEMANTIC", ["nointerpolation"], [3u, 1u]);

        var expected =
            "Original Name: A\n" +
            "Semantic: SV_SEMANTIC\n" +
            "Dimensions: [3][1]\n" +
            "Modifiers: nointerpolation\n";

        await Assert.That(StructLayoutHelper.DocumentMember(member).ReplaceLineEndings("\n")).IsEqualTo(expected);
    }

    [Test]
    public async Task StructNamesFollowNamingConvention()
    {
        await Assert.That(StructLayoutHelper.ElementStructName("Constants", "E")).IsEqualTo("ConstantsEElement");
        await Assert.That(StructLayoutHelper.ArrayStructName("Constants", "E")).IsEqualTo("ConstantsEArray");
    }
}
