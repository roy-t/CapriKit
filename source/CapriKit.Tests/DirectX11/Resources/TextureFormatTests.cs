using CapriKit.DirectX11.Resources.Surfaces;

namespace CapriKit.Tests.DirectX11.Resources;

internal class TextureFormatTests
{
    [Test]
    public async Task GetRowPitch()
    {
        var format = TextureFormat.BC7_UNORM;

        // 10 pixels need 3 blocks of 4 pixels, the last block is partially covered
        var rowPitch = format.GetRowPitch(10);

        await Assert.That(rowPitch).IsEqualTo(3 * 16);
    }
}
