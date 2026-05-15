using CapriKit.Generators.HLSL;

namespace CapriKit.Tests.Generators.HLSL;

internal class TypeTranslatorTests
{
    [Test]
    public async Task TranslatesScalar()
    {
        var result = TypeTranslator.Translate("float", []);

        await Assert.That(result.DotNetType).IsEqualTo("float");
        await Assert.That(result.IsFixed).IsFalse();
        await Assert.That(result.OriginalDimensions).IsEmpty();
    }

    [Test]
    public async Task TranslatesVectorToSystemNumerics()
    {
        var result = TypeTranslator.Translate("float4", []);

        await Assert.That(result.DotNetType).IsEqualTo("System.Numerics.Vector4");
        await Assert.That(result.IsFixed).IsFalse();
    }

    [Test]
    public async Task TranslatesMatrixToSystemNumerics()
    {
        var result = TypeTranslator.Translate("float4x4", []);

        await Assert.That(result.DotNetType).IsEqualTo("System.Numerics.Matrix4x4");
    }

    [Test]
    public async Task TranslatesDwordAliasToUint()
    {
        var result = TypeTranslator.Translate("dword", []);

        await Assert.That(result.DotNetType).IsEqualTo("uint");
    }

    [Test]
    public async Task FixedSizeArrayBecomesFixedBuffer()
    {
        var result = TypeTranslator.Translate("float", [4]);

        await Assert.That(result.DotNetType).IsEqualTo("float");
        await Assert.That(result.IsFixed).IsTrue();
        await Assert.That(result.FixedSize).IsEqualTo(4u);
        await Assert.That(result.OriginalDimensions).IsEquivalentTo(new uint[] { 4 });
    }

    [Test]
    public async Task MultidimensionalArrayIsFlattened()
    {
        var result = TypeTranslator.Translate("float", [4, 2]);

        await Assert.That(result.IsFixed).IsTrue();
        await Assert.That(result.FixedSize).IsEqualTo(8u);
        await Assert.That(result.OriginalDimensions).IsEquivalentTo(new uint[] { 4, 2 });
    }

    [Test]
    public async Task UnknownTypeFallsBackToValidIdentifier()
    {
        var result = TypeTranslator.Translate("MyStruct", []);

        await Assert.That(result.DotNetType).IsEqualTo("MyStruct");
        await Assert.That(result.IsFixed).IsFalse();
    }
}
