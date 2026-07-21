using CapriKit.IO;
using System.Buffers;

namespace CapriKit.AssetPipeline;

public readonly struct NoSettings<TAsset> : IAssetSettings<TAsset> { }

public interface IAssetSettings<TAsset> { }

public interface IAssetTranscoder
{
    Guid Id { get; }
    int Version { get; }
}

// The pipeline consumes transcoders through this settings-erased view so that AssetManager,
// AssetEncoder and AssetDecoder never need a TSettings type parameter. The erased members are
// internal because only the pipeline should call them; transcoder authors implement
// IAssetTranscoder<TAsset, TSettings>, which bridges them.
public interface IAssetTranscoder<TAsset> : IAssetTranscoder
{
    internal Task Encode(AssetId id, IAssetSettings<TAsset> settings, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer);
    internal TAsset Decode(AssetId id, IAssetSettings<TAsset> settings, ref SequenceReader<byte> reader);
    internal IAssetSettings<TAsset> ReadSettings(ref SequenceReader<byte> reader);
    internal void WriteSettings(IAssetSettings<TAsset> settings, IBufferWriter<byte> writer);

    /// <summary>
    /// Moves the contents of <paramref name="newParts"/> into <paramref name="instance"/>.
    /// <paramref name="instance"/> keeps its identity and stays the live object that callers
    /// already hold references to. The transcoder is responsible for cleaning-up any
    /// orphaned resources. After calling this method <paramref name="newParts"/> must no longer
    /// be used or referenced.
    /// </summary>
    void HotSwap(TAsset instance, TAsset newParts);
}

public interface IAssetTranscoder<TAsset, TSettings> : IAssetTranscoder<TAsset>
    where TSettings : IAssetSettings<TAsset>
{
    // Asynchronous since we expect the encoder to read external files
    Task Encode(AssetId id, TSettings settings, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer);

    // Synchronous by design: the envelope owns all file IO and hands the decoder an
    // in-memory payload. The reader's buffer is only valid for the duration of the call,
    // decoders must copy out anything they want to keep.
    TAsset Decode(AssetId id, TSettings settings, ref SequenceReader<byte> reader);

    // `new` because it hides the erased ReadSettings: same parameters, more specific return type
    new TSettings ReadSettings(ref SequenceReader<byte> reader);

    void WriteSettings(TSettings settings, IBufferWriter<byte> writer);

    // Default implementations that bridge the settings-erased members to their typed
    // counterparts. This is the only place where the pipeline transitions from
    // IAssetSettings<TAsset> back to the concrete TSettings
    Task IAssetTranscoder<TAsset>.Encode(AssetId id, IAssetSettings<TAsset> settings, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
        => Encode(id, AsTypedSettings(settings), fileSystem, writer);

    TAsset IAssetTranscoder<TAsset>.Decode(AssetId id, IAssetSettings<TAsset> settings, ref SequenceReader<byte> reader)
        => Decode(id, AsTypedSettings(settings), ref reader);

    IAssetSettings<TAsset> IAssetTranscoder<TAsset>.ReadSettings(ref SequenceReader<byte> reader)
        => ReadSettings(ref reader);

    void IAssetTranscoder<TAsset>.WriteSettings(IAssetSettings<TAsset> settings, IBufferWriter<byte> writer)
        => WriteSettings(AsTypedSettings(settings), writer);

    private static TSettings AsTypedSettings(IAssetSettings<TAsset> settings)
    {
        if (settings is not TSettings typedSettings)
        {
            throw new ArgumentException(
                $"Transcoder for {typeof(TAsset).Name} expects settings of type {typeof(TSettings).Name} but got {settings?.GetType().Name ?? "null"}",
                nameof(settings));
        }

        return typedSettings;
    }
}
