using CapriKit.Generators.HLSL;

namespace CapriKit.Tests.Generators.HLSL;

internal class TypeTranslatorTests
{
    [Test]
    public async Task TranslatesScalar()
    {
        var result = TypeTranslator.Translate("float");

        await Assert.That(result.DotNetType).IsEqualTo("float");
        await Assert.That(result.SizeInBytes).IsEqualTo(4u);
        await Assert.That(result.Format).IsEqualTo("Vortice.DXGI.Format.R32_Float");
    }

    [Test]
    public async Task TranslatesVectorToSystemNumerics()
    {
        var result = TypeTranslator.Translate("float4");

        await Assert.That(result.DotNetType).IsEqualTo("System.Numerics.Vector4");
        await Assert.That(result.SizeInBytes).IsEqualTo(16u);
        await Assert.That(result.Format).IsEqualTo("Vortice.DXGI.Format.R32G32B32A32_Float");
    }

    [Test]
    public async Task TranslatesMatrixToSystemNumerics()
    {
        var result = TypeTranslator.Translate("float4x4");

        await Assert.That(result.DotNetType).IsEqualTo("System.Numerics.Matrix4x4");
        await Assert.That(result.SizeInBytes).IsEqualTo(64u);
        await Assert.That(result.Format).IsEqualTo("Vortice.DXGI.Format.Unknown");
    }

    [Test]
    public async Task TranslatesDwordAliasToUint()
    {
        var result = TypeTranslator.Translate("dword");

        await Assert.That(result.DotNetType).IsEqualTo("uint");
        await Assert.That(result.SizeInBytes).IsEqualTo(4u);
        await Assert.That(result.Format).IsEqualTo("Vortice.DXGI.Format.R32_UInt");
    }

    [Test]
    public async Task TryTranslate_UnknownTypeReturnsFalse()
    {
        var translated = TypeTranslator.TryTranslate("MyStruct", out _);

        await Assert.That(translated).IsFalse();
    }

    [Test]
    public async Task GetFormat_UnknownTypeReturnsUnknown()
    {
        var result = TypeTranslator.GetFormat("MyStruct");

        await Assert.That(result).IsEqualTo("Vortice.DXGI.Format.Unknown");
    }
}
