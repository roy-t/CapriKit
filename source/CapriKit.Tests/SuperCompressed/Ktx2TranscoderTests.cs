using CapriKit.SuperCompressed;

namespace CapriKit.Tests.SuperCompressed;

internal class Ktx2TranscoderTests
{
    private static byte[] EncodeGradientWithMips()
    {
        var image = TestImages.CreateGradient();
        return Ktx2Encoder.Encode(image, BasisTexFormat.UastcLdr4x4, effort: 2,
            flags: CompressionFlags.Srgb | CompressionFlags.GenMipsClamp);
    }

    [Test]
    public async Task OpenReadsTheTextureMetadata()
    {
        using var ktx2File = Ktx2Transcoder.Open(EncodeGradientWithMips());

        await Assert.That(Ktx2Transcoder.GetWidth(ktx2File)).IsEqualTo(16u);
        await Assert.That(Ktx2Transcoder.GetHeight(ktx2File)).IsEqualTo(16u);
        await Assert.That(Ktx2Transcoder.GetLevels(ktx2File)).IsEqualTo(5u); // 16, 8, 4, 2, 1
        await Assert.That(Ktx2Transcoder.GetFaces(ktx2File)).IsEqualTo(1u);
        await Assert.That(Ktx2Transcoder.GetFormat(ktx2File)).IsEqualTo(BasisTexFormat.UastcLdr4x4);
        await Assert.That(Ktx2Transcoder.IsSrgb(ktx2File)).IsTrue();
    }

    [Test]
    public async Task TranscodeToUncompressedRgba32()
    {
        using var ktx2File = Ktx2Transcoder.Open(EncodeGradientWithMips());

        var level0 = Ktx2Transcoder.Transcode(ktx2File, TranscodeFormat.Rgba32);

        await Assert.That(level0.Width).IsEqualTo(16u);
        await Assert.That(level0.Height).IsEqualTo(16u);
        await Assert.That(level0.RowPitch).IsEqualTo(16u * 4);
        await Assert.That(level0.Data.Length).IsEqualTo(16 * 16 * 4);

        // The gradient's top-right pixel is fully red; UASTC at the lower effort levels
        // is quite lossy, so only check that the pixel is still clearly red
        var topRightRed = (int)level0.Data[(16 - 1) * 4 + 0];
        var topRightAlpha = (int)level0.Data[(16 - 1) * 4 + 3];
        await Assert.That(topRightRed).IsGreaterThan(248);
        await Assert.That(topRightAlpha).IsEqualTo(255);

        var level4 = Ktx2Transcoder.Transcode(ktx2File, TranscodeFormat.Rgba32, level: 4);
        await Assert.That(level4.Width).IsEqualTo(1u);
        await Assert.That(level4.Height).IsEqualTo(1u);
        await Assert.That(level4.Data.Length).IsEqualTo(4);
    }

    [Test]
    public async Task TranscodeAllTranscodesEveryLevelLayerAndFace()
    {
        using var ktx2File = Ktx2Transcoder.Open(EncodeGradientWithMips());

        var images = Ktx2Transcoder.TranscodeAll(ktx2File, TranscodeFormat.Rgba32);

        // 5 mip levels, not an array, not a cubemap
        await Assert.That(images.Length).IsEqualTo(5);
        for (var level = 0u; level < images.Length; level++)
        {
            var image = images[level];
            var expectedWidth = 16u >> (int)level;
            await Assert.That(image.Level).IsEqualTo(level);
            await Assert.That(image.Layer).IsEqualTo(0u);
            await Assert.That(image.Face).IsEqualTo(0u);
            await Assert.That(image.Width).IsEqualTo(expectedWidth);
            await Assert.That(image.Height).IsEqualTo(expectedWidth);
            await Assert.That(image.Data.Length).IsEqualTo((int)(expectedWidth * expectedWidth * 4));
        }
    }

    [Test]
    public async Task TranscodeToBlockCompressedBc7()
    {
        using var ktx2File = Ktx2Transcoder.Open(EncodeGradientWithMips());

        var level0 = Ktx2Transcoder.Transcode(ktx2File, TranscodeFormat.Bc7Rgba);

        // 16x16 pixels = 4x4 blocks of 16 bytes each
        await Assert.That(level0.Width).IsEqualTo(16u);
        await Assert.That(level0.Height).IsEqualTo(16u);
        await Assert.That(level0.RowPitch).IsEqualTo(4u * 16);
        await Assert.That(level0.Data.Length).IsEqualTo(4 * 4 * 16);
    }
}
