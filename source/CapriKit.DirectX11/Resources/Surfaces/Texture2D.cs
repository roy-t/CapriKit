using CapriKit.DirectX11.Debug;
using CapriKit.DirectX11.Resources.Views;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Surfaces;

/// <summary>
/// The texel data of one mip level.
/// </summary>
/// <param name="Data">The texel data, for block-compressed formats rows of 4x4 blocks.</param>
/// <param name="RowPitch">Distance in bytes between rows of pixels, or rows of blocks for block-compressed formats.</param>
public readonly record struct MipLevel(ReadOnlyMemory<byte> Data, uint RowPitch);

public sealed class Texture2D : IShaderResourceView
{
    private ID3D11ShaderResourceView shaderResourceView;
    private ID3D11Texture2D texture;

    /// <summary>
    /// Creates an immutable 2D texture, <paramref name="mipLevels"/> holds every mip level from largest to smallest.
    /// </summary>
    public Texture2D(Device device, ReadOnlySpan<MipLevel> mipLevels, TextureFormat format, int width, int height,
        [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(mipLevels.Length, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mipLevels.Length, BitOperations.Log2((uint)Math.Max(width, height)) + 1);

        Width = width;
        Height = height;
        MipLevels = mipLevels.Length;

        var description = new Texture2DDescription(format.InternalFormat, (uint)width, (uint)height, 1u, (uint)mipLevels.Length,
            BindFlags.ShaderResource, ResourceUsage.Immutable);

        var pins = new MemoryHandle[mipLevels.Length];
        var initialData = new SubresourceData[mipLevels.Length];
        try
        {
            for (var i = 0; i < mipLevels.Length; i++)
            {
                var mipLevel = mipLevels[i];
                ValidateMipLevel(format, width, height, i, mipLevel);

                var index = D3D11.CalculateSubResourceIndex((uint)i, 0, (uint)mipLevels.Length);

                pins[index] = mipLevel.Data.Pin();
                unsafe { initialData[index] = new SubresourceData(pins[index].Pointer, mipLevel.RowPitch); }
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

    private static void ValidateMipLevel(TextureFormat format, int width, int height, int level, MipLevel data)
    {
        var levelWidth = Math.Max(1, width >> level);
        var levelHeight = Math.Max(1, height >> level);
        var rows = (levelHeight + format.BlockHeight - 1) / format.BlockHeight;
        if (data.RowPitch < format.GetRowPitch(levelWidth) || data.Data.Length < data.RowPitch * rows)
        {
            throw new Exception($"Mip level {level} ({levelWidth}x{levelHeight} {format}) needs a row pitch of at least " +
                $"{format.GetRowPitch(levelWidth)} and {data.RowPitch * rows} bytes, got {data.RowPitch} and {data.Data.Length}");
        }
    }

    ID3D11ShaderResourceView IShaderResourceView.ID3D11ShaderResourceView => shaderResourceView;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int MipLevels { get; private set; }

    public void HotSwap(Texture2D newParts)
    {
        Dispose();
        shaderResourceView = newParts.shaderResourceView;
        texture = newParts.texture;
        Width = newParts.Width;
        Height = newParts.Height;
        MipLevels = newParts.MipLevels;
    }

    public void Dispose()
    {
        shaderResourceView.Dispose();
        texture.Dispose();
    }
}
