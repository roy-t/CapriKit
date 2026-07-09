using CapriKit.SuperCompressed;

namespace CapriKit.Tests.SuperCompressed;

internal class Ktx2TranscoderTests
{
    private static byte[] EncodeGradientWithMips()
    {
        var image = TestImages.CreateGradient();
        return Encoder.Encode(image, BasisTexFormat.UastcLdr4x4, effort: 2,
            flags: CompressionFlags.Srgb | CompressionFlags.GenMipsClamp);
    }

    [Test]
    public async Task OpenReadsTheTextureMetadata()
    {
        using var ktx2File = Ktx2Transcoder.Open(EncodeGradientWithMips());

        await Assert.That(Ktx2Transcoder.GetWidth(ktx2File)).IsEqualTo(16);
        await Assert.That(Ktx2Transcoder.GetHeight(ktx2File)).IsEqualTo(16);
        await Assert.That(Ktx2Transcoder.GetLevels(ktx2File)).IsEqualTo(5); // 16, 8, 4, 2, 1
        await Assert.That(Ktx2Transcoder.GetFaces(ktx2File)).IsEqualTo(1);
        await Assert.That(Ktx2Transcoder.GetFormat(ktx2File)).IsEqualTo(BasisTexFormat.UastcLdr4x4);
        await Assert.That(Ktx2Transcoder.IsSrgb(ktx2File)).IsTrue();
    }

    [Test]
    public async Task TranscodeToUncompressedRgba32()
    {
        using var ktx2File = Ktx2Transcoder.Open(EncodeGradientWithMips());

        var level0 = Ktx2Transcoder.Transcode(ktx2File, TranscodeFormat.Rgba32);

        await Assert.That(level0.Width).IsEqualTo(16);
        await Assert.That(level0.Height).IsEqualTo(16);
        await Assert.That(level0.RowPitch).IsEqualTo(16 * 4);
        await Assert.That(level0.Data.Length).IsEqualTo(16 * 16 * 4);

        // The gradient's top-right pixel is fully red; UASTC at the lower effort levels
        // is quite lossy, so only check that the pixel is still clearly red
        var topRightRed = (int)level0.Data[(16 - 1) * 4 + 0];
        var topRightAlpha = (int)level0.Data[(16 - 1) * 4 + 3];
        await Assert.That(topRightRed).IsGreaterThan(248);
        await Assert.That(topRightAlpha).IsEqualTo(255);

        var level4 = Ktx2Transcoder.Transcode(ktx2File, TranscodeFormat.Rgba32, level: 4);
        await Assert.That(level4.Width).IsEqualTo(1);
        await Assert.That(level4.Height).IsEqualTo(1);
        await Assert.That(level4.Data.Length).IsEqualTo(4);
    }

    [Test]
    public async Task TranscodeToBlockCompressedBc7()
    {
        using var ktx2File = Ktx2Transcoder.Open(EncodeGradientWithMips());

        var level0 = Ktx2Transcoder.Transcode(ktx2File, TranscodeFormat.Bc7Rgba);

        // 16x16 pixels = 4x4 blocks of 16 bytes each
        await Assert.That(level0.Width).IsEqualTo(16);
        await Assert.That(level0.Height).IsEqualTo(16);
        await Assert.That(level0.RowPitch).IsEqualTo(4 * 16);
        await Assert.That(level0.Data.Length).IsEqualTo(4 * 4 * 16);
    }
}
