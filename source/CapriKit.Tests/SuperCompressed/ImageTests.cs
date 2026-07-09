using CapriKit.SuperCompressed;

namespace CapriKit.Tests.SuperCompressed;

internal class ImageTests
{
    [Test]
    public async Task LoadDecodesImageFilesToRgba32()
    {
        var image = Image.Load(TestImages.CreateTgaFile());

        await Assert.That(image.Width).IsEqualTo(2);
        await Assert.That(image.Height).IsEqualTo(2);
        await Assert.That(image.Pixels.Length).IsEqualTo(2 * 2 * 4);

        // The first pixel is red; TGA stores BGRA but Image is always RGBA
        await Assert.That(image.Pixels[0]).IsEqualTo((byte)255);
        await Assert.That(image.Pixels[1]).IsEqualTo((byte)0);
        await Assert.That(image.Pixels[2]).IsEqualTo((byte)0);
        await Assert.That(image.Pixels[3]).IsEqualTo((byte)255);
    }

    [Test]
    public async Task ConstructorAcceptsRawPixels()
    {
        var image = TestImages.CreateGradient(4, 2);

        await Assert.That(image.Width).IsEqualTo(4);
        await Assert.That(image.Height).IsEqualTo(2);
        await Assert.That(image.Pixels.Length).IsEqualTo(4 * 2 * 4);
    }
}
