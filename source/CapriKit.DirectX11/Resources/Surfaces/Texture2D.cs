using CapriKit.DirectX11.Debug;
using System.Buffers;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Surfaces;

public enum TextureFormat : uint
{
    // Values Match Vortice.DXGI.Format

    BC7_UNORM = 98u,
    BC7_UNORM_SRGB = 99u,
    BC5_UNORM = 83u,
}

/// <summary>
/// The texel data of one mip level.
/// </summary>
/// <param name="Data">The texel data, for block-compressed formats rows of 4x4 blocks.</param>
/// <param name="RowPitch">Distance in bytes between rows of pixels, or rows of blocks for block-compressed formats.</param>
public readonly record struct MipLevel(ReadOnlyMemory<byte> Data, uint RowPitch);

public sealed class Texture2D : IDisposable
{
    private ID3D11ShaderResourceView shaderResourceView;
    private ID3D11Texture2D texture;

    /// <summary>
    /// Creates an immutable texture, <paramref name="mipLevels"/> holds every mip level from largest to smallest.
    /// </summary>
    public Texture2D(Device device, ReadOnlySpan<MipLevel> mipLevels, TextureFormat format, uint width, uint height,
        [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        var description = new Texture2DDescription((Vortice.DXGI.Format)format, width, height, 1, (uint)mipLevels.Length,
            BindFlags.ShaderResource, ResourceUsage.Immutable);

        
        var pins = new MemoryHandle[mipLevels.Length];
        var initialData = new SubresourceData[mipLevels.Length];
        try
        {
            for (var i = 0; i < mipLevels.Length; i++)
            {
                pins[i] = mipLevels[i].Data.Pin();
                unsafe { initialData[i] = new SubresourceData(pins[i].Pointer, mipLevels[i].RowPitch); }
            }

            texture = device.ID3D11Device.CreateTexture2D(description, initialData);
        }
        finally
        {
            foreach (var pin in pins)
            {
                pin.Dispose();
            }
        }

        texture.DebugName = DebugName.For(texture, caller: caller, callerFile: callerFile, callerLine: callerLine);

        shaderResourceView = device.ID3D11Device.CreateShaderResourceView(texture);
        shaderResourceView.DebugName = DebugName.For(shaderResourceView, caller: caller, callerFile: callerFile, callerLine: callerLine);
    }

    public void HotSwap(Texture2D newParts)
    {
        Dispose();
        shaderResourceView = newParts.shaderResourceView;
        texture = newParts.texture;
    }

    public void Dispose()
    {
        shaderResourceView.Dispose();
        texture.Dispose();
    }
}
