using CapriKit.DirectX11.Debug;
using CapriKit.DirectX11.Resources.Views;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Surfaces;

/// <summary>
/// The texel data of one texture sub resource, usually one mip level.
/// </summary>
public readonly record struct Subresource(ReadOnlyMemory<byte> Data, uint RowPitch);

public sealed class Texture2D : IShaderResourceView
{
    private ID3D11ShaderResourceView shaderResourceView;
    private ID3D11Texture2D texture;

    /// <summary>
    /// Creates an immutable 2D texture of 1 or more mipmaps.
    /// </summary>
    public Texture2D(Device device, ReadOnlySpan<Subresource> mipmap, TextureFormat format, int width, int height,
        [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(mipmap.Length, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mipmap.Length, BitOperations.Log2((uint)Math.Max(width, height)) + 1);

        Width = width;
        Height = height;
        MipLevels = mipmap.Length;

        var description = new Texture2DDescription(format.InternalFormat, (uint)width, (uint)height, 1u, (uint)mipmap.Length,
            BindFlags.ShaderResource, ResourceUsage.Immutable);

        var pins = new MemoryHandle[mipmap.Length];
        var initialData = new SubresourceData[mipmap.Length];
        try
        {
            for (var i = 0; i < mipmap.Length; i++)
            {
                var mipLevel = mipmap[i];
                ValidateMipLevel(format, width, height, i, mipLevel);

                var index = D3D11.CalculateSubResourceIndex((uint)i, 0, (uint)mipmap.Length);

                pins[index] = mipLevel.Data.Pin();
                unsafe { initialData[index] = new SubresourceData(pins[index].Pointer, mipLevel.RowPitch); }
            }

            texture = device.ID3D11Device.CreateTexture2D(description, initialData);
            texture.DebugName = DebugName.For(texture, caller: caller, callerFile: callerFile, callerLine: callerLine);
        }
        finally
        {
            foreach (var pin in pins)
            {
                pin.Dispose();
            }
        }

        shaderResourceView = device.ID3D11Device.CreateShaderResourceView(texture);
        shaderResourceView.DebugName = DebugName.For(shaderResourceView, caller: caller, callerFile: callerFile, callerLine: callerLine);
    }

    private static void ValidateMipLevel(TextureFormat format, int width, int height, int level, Subresource resource)
    {
        var levelWidth = Math.Max(1, width >> level);
        var levelHeight = Math.Max(1, height >> level);
        var rows = (levelHeight + format.BlockHeight - 1) / format.BlockHeight;
        if (resource.RowPitch < format.GetRowPitch(levelWidth) || resource.Data.Length < resource.RowPitch * rows)
        {
            throw new Exception($"Mip level {level} ({levelWidth}x{levelHeight} {format}) needs a row pitch of at least " +
                $"{format.GetRowPitch(levelWidth)} and {resource.RowPitch * rows} bytes, got {resource.RowPitch} and {resource.Data.Length}");
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
