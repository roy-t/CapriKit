using CapriKit.DirectX11;
using CapriKit.DirectX11.Resources.Surfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CapriKit.Tests.DirectX11.Resources;

[NotInParallel("DirectX")]
internal class Texture2DTests
{
    [Test]
    public async Task CreateWithMipLevels()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        // An 8x8 BC7 texture is 2x2 blocks of 16 bytes, its 4x4 mip level is a single block
        MipLevel[] mipLevels =
        [
            new(new byte[2 * 2 * 16], 2 * 16),
            new(new byte[1 * 1 * 16], 1 * 16),
        ];

        await Assert.That(() => new Texture2D(device, mipLevels, TextureFormat.BC7_UNORM, 8, 8).Dispose()).ThrowsNothing();
    }
}
