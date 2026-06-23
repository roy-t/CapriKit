using CapriKit.Generators.HLSL;

namespace CapriKit.Tests.Generators.HLSL;

internal class TypeTranslatorTests
{
    [Test]
    public async Task GetSizeInBytes()
    {
        var result = TypeTranslator.GetSizeInBytes("int2");
        await Assert.That(result).IsEqualTo(8);
    }

    [Test]
    public async Task GetFormat()
    {
        var result = TypeTranslator.GetFormat("dword");
        await Assert.That(result).IsEqualTo("Vortice.DXGI.Format.R32_UInt");
    }

    [Test]
    public async Task Translate()
    {
        var result = TypeTranslator.Translate("float");
        await Assert.That(result).IsEqualTo("float");
    }

    [Test]
    public async Task TranslateUnknownTypeFallsBackToValidIdentifier()
    {
        var result = TypeTranslator.Translate("MyStruct");
        await Assert.That(result).IsEqualTo("MyStruct");
    }
}
