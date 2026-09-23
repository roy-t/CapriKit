using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using Microsoft.Extensions.Logging.Abstractions;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace CapriKit.Tests.DirectX11.Buffers;

[NotInParallel("DirectX")]
internal class StagingBuffer2DTests
{
    /// <summary>
    /// A mapped texture is laid out row by row, but each row is padded to the row alignment the driver
    /// requires, so RowPitch is usually larger than width * sizeof(T). Reading the surface back therefore
    /// has to copy one row at a time; a flat copy drags that padding along and returns more elements than
    /// the texture has pixels.
    /// </summary>
    [Test]
    public async Task CopySurfaceDataToSpan()
    {
        using var device = new Device(NullLoggerFactory.Instance);
        var context = device.ImmediateDeviceContext;

        // 13 pixels of 4 bytes is 52 bytes per row, far below any row alignment a driver uses, so this
        // texture is guaranteed to be padded
        const uint width = 13;
        const uint height = 4;
        const uint red = 0xFF0000FF;

        var description = new Texture2DDescription
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.RenderTarget,
        };

        // Fill every pixel of the source with one known color
        using var source = device.ID3D11Device.CreateTexture2D(description);
        using var renderTargetView = device.ID3D11Device.CreateRenderTargetView(source);
        context.ID3D11DeviceContext.ClearRenderTargetView(renderTargetView, new Color4(1.0f, 0.0f, 0.0f, 1.0f));

        using var stagingBuffer = new StagingBuffer2D<uint>(device, description, "stagingBuffer");
        var pixels = stagingBuffer.CopySurfaceDataToSpan(context.ID3D11DeviceContext, source);

        await Assert.That(pixels.Length).IsEqualTo((int)(width * height));
        await Assert.That(pixels.All(pixel => pixel == red)).IsTrue();
    }
}
