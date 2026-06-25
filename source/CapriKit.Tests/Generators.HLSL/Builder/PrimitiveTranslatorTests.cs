using CapriKit.Generators.HLSL.Builder;

namespace CapriKit.Tests.Generators.HLSL.Builder;

internal class PrimitiveTranslatorTests
{
    [Test]
    public async Task GetSizeInBytes()
    {
        var result = PrimitiveTranslator.GetSizeInBytes("int2");
        await Assert.That(result).IsEqualTo(8u);
    }

    [Test]
    public async Task GetFormat()
    {
        var result = PrimitiveTranslator.GetFormat("dword");
        await Assert.That(result).IsEqualTo("Vortice.DXGI.Format.R32_UInt");
    }

    [Test]
    public async Task TranslateKnownPrimitive()
    {
        var result = PrimitiveTranslator.Translate("float3");
        await Assert.That(result).IsEqualTo("System.Numerics.Vector3");
    }

    [Test]
    public async Task TranslateUnknownTypeFallsBackToValidIdentifier()
    {
        var result = PrimitiveTranslator.Translate("MyStruct");
        await Assert.That(result).IsEqualTo("MyStruct");
    }
}
