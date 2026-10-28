using CapriKit.DirectX11;
using CapriKit.DirectX11.Resources.Surfaces;
using CapriKit.IO;
using CapriKit.IO.Streams;
using CapriKit.SuperCompressed;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;

namespace CapriKit.AssetPipeline.DirectX11.Textures;

public enum TextureUsage : byte
{
    /// <summary>
    /// A regular image optimized for perceptual quality.
    /// </summary>
    SRgb,
    /// <summary>
    /// A normal map, optimized for keeping the x value and y values precise
    /// even when block compressed. We assume the input puts the x and y values
    /// in the red and green channels. When we encode the data we store x in rgb
    /// and y in the alpha channel. When the data is transcoded to the GPU
    /// the BC5 format puts X in R and Y in G. The shader needs to reconstruct z.
    /// </summary>
    NormalMap,
    /// <summary>
    /// Either color data that is in linear color space or non-color data that is stored in a texture.
    /// Encoding and decoding does not make any optimizations based on what the data might mean.
    /// </summary>
    Linear,
}

public enum MipMapGeneration : byte
{
    /// <summary>
    /// Do not generate mip maps for this image.
    /// </summary>
    None,
    /// <summary>
    /// Generate mip maps and assume the image does not wrap.
    /// </summary>
    Clamp,
    /// <summary>
    /// Generate mip maps and assume the image wraps.
    /// </summary>
    Wrap
}

public sealed record Texture2DSettings(TextureUsage Usage, MipMapGeneration MipMaps);
public sealed class Texture2DTranscoder(Device device) : IAssetTranscoder<Texture2D, Texture2DSettings>
{
    public Guid Id { get; } = Guid.Parse("{12AC6151-6655-43E5-9898-4ADB80E08EE1}");
    public int Version => 1;

    public async Task Encode(AssetId id, Texture2DSettings settings, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        var source = await fileSystem.ReadAllBytes(id.Path);
        var image = Image.Load(source);
        ValidateDimensions(id, image, settings);

        if (settings.Usage == TextureUsage.NormalMap)
        {
            image = ConvertToNormalMap(image);
        }

        var (format, flags) = ToEncoderSettings(settings);
        var bytes = Ktx2Encoder.Encode(image, format, quality: 100, flags: flags);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static void ValidateDimensions(AssetId id, Image image, Texture2DSettings settings)
    {
        var (_, format, _) = ToDecoderSettings(settings);
        if (image.Width % format.BlockWidth != 0 || image.Height % format.BlockHeight != 0)
        {
            throw new InvalidOperationException($"The dimensions of texture {id.Path}: {image.Width}x{image.Height} " +
                $"need to be a multiple of {format.BlockWidth}x{format.BlockHeight}");
        }
    }

    private static (BasisTexFormat, CompressionFlags) ToEncoderSettings(Texture2DSettings settings)
    {
        var usage = settings.Usage;
        var mipMaps = settings.MipMaps;

        var flags = CompressionFlags.Threaded | CompressionFlags.Ktx2UastcZstd;
        flags |= mipMaps switch
        {
            MipMapGeneration.None => CompressionFlags.None,
            MipMapGeneration.Clamp => CompressionFlags.GenMipsClamp,
            MipMapGeneration.Wrap => CompressionFlags.GenMipsWrap,
            _ => throw new NotSupportedException()
        };

        return usage switch
        {
            TextureUsage.SRgb => (BasisTexFormat.UastcLdr4x4, flags | CompressionFlags.Srgb),
            TextureUsage.Linear => (BasisTexFormat.UastcLdr4x4, flags),
            TextureUsage.NormalMap => (BasisTexFormat.UastcLdr4x4, flags),
            _ => throw new NotSupportedException(),
        };
    }

    /// <summary>
    /// For optimal encoding in block-compressed texture formats we renormalize the normal
    /// encoded in the RGB channels and store X in RGB and Y in A. The normal is then
    /// recalculated in the shader using:
    /// <code>
    /// float2 xy = NormalMap.Sample(Sampler, uv).rg * 2.0f - 1.0f;
    /// float3 n = float3(xy, sqrt(saturate(1.0f - dot(xy, xy))));
    /// </code>
    /// </summary>
    private static Image ConvertToNormalMap(Image image)
    {
        var rgba = new byte[image.Pixels.Length];
        image.Pixels.CopyTo(rgba);
        for (var i = 0; i < rgba.Length; i += 4)
        {
            var vector = (new Vector3(rgba[i], rgba[i + 1], rgba[i + 2]) / 127.5f) - Vector3.One;
            var n = vector.LengthSquared() > 0.25f ? Vector3.Normalize(vector) : Vector3.UnitZ;
            var x = (byte)MathF.Round((n.X + 1.0f) * 127.5f);
            var y = (byte)MathF.Round((n.Y + 1.0f) * 127.5f);
            rgba[i] = rgba[i + 1] = rgba[i + 2] = x;
            rgba[i + 3] = y;
        }

        return new Image(rgba, image.Width, image.Height);
    }

    public Texture2D Decode(AssetId id, Texture2DSettings settings, ref SequenceReader<byte> reader)
    {
        var length = reader.ReadInt32();
        var bytes = reader.ReadBytes(length);
        using var handle = Ktx2Transcoder.Open(bytes);

        var width = (int)Ktx2Transcoder.GetWidth(handle);
        var height = (int)Ktx2Transcoder.GetHeight(handle);

        var isSrgb = Ktx2Transcoder.IsSrgb(handle);
        Debug.Assert(isSrgb == (settings.Usage == TextureUsage.SRgb));

        var layers = Ktx2Transcoder.GetLayers(handle);
        Debug.Assert(layers <= 1);

        var faces = Ktx2Transcoder.GetFaces(handle);
        Debug.Assert(faces == 1);

        var (transcodeFormat, textureFormat, flags) = ToDecoderSettings(settings);

        // For textures with multiple layers or faces the ordering of these elements
        // does not map to how DirectX expects it. Fine for the current use case of a simple
        // 2D texture (with mipmaps).
        var levels = Ktx2Transcoder.TranscodeAll(handle, transcodeFormat, flags);
        var mipLevels = new MipLevel[levels.Length];
        for (var i = 0; i < levels.Length; i++)
        {
            mipLevels[i] = new MipLevel(levels[i].Data, levels[i].RowPitch);
        }

        return new Texture2D(device, mipLevels, textureFormat, width, height);
    }

    private static (TranscodeFormat, TextureFormat, DecodeFlags) ToDecoderSettings(Texture2DSettings settings)
    {
        var usage = settings.Usage;

        var flags = DecodeFlags.None;
        flags |= usage switch
        {
            TextureUsage.SRgb => DecodeFlags.None,
            TextureUsage.NormalMap => DecodeFlags.HighQuality,
            TextureUsage.Linear => DecodeFlags.None,
            _ => throw new NotSupportedException()
        };

        return usage switch
        {
            TextureUsage.SRgb => (TranscodeFormat.Bc7Rgba, TextureFormat.BC7_UNORM_SRGB, flags),
            TextureUsage.Linear => (TranscodeFormat.Bc7Rgba, TextureFormat.BC7_UNORM, flags),
            TextureUsage.NormalMap => (TranscodeFormat.Bc5Rg, TextureFormat.BC5_UNORM, flags),
            _ => throw new NotSupportedException()
        };
    }

    public void HotSwap(Texture2D instance, Texture2D newParts)
    {
        instance.HotSwap(newParts);
    }

    public void WriteSettings(Texture2DSettings settings, IBufferWriter<byte> writer)
    {
        writer.Write((byte)settings.Usage);
        writer.Write((byte)settings.MipMaps);
    }

    public Texture2DSettings ReadSettings(ref SequenceReader<byte> reader)
    {
        var usage = (TextureUsage)reader.ReadByte();
        var mipMaps = (MipMapGeneration)reader.ReadByte();

        return new Texture2DSettings(usage, mipMaps);
    }
}
