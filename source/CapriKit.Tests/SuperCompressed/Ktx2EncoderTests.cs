using CapriKit.SuperCompressed;

namespace CapriKit.Tests.SuperCompressed;

internal class Ktx2EncoderTests
{
    private static readonly byte[] Ktx2Magic = [0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A];

    [Test]
    public async Task EncodeProducesAKtx2File()
    {
        var image = TestImages.CreateGradient();

        var ktx2 = Ktx2Encoder.Encode(image, BasisTexFormat.UastcLdr4x4, effort: 1);

        await Assert.That(ktx2.Length).IsGreaterThan(Ktx2Magic.Length);
        await Assert.That(ktx2.AsSpan(0, Ktx2Magic.Length).SequenceEqual(Ktx2Magic)).IsTrue();
    }
}
