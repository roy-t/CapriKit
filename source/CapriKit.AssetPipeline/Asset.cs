using CapriKit.IO;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Unique asset identifier
/// </summary>
/// <param name="Path">Virtual file path that points to the file the asset originates from.</param>
/// <param name="Key">Optional key to a sub-resources in Path.</param>
public record AssetId(FilePath Path, string Key = "");

/// <summary>
/// An asset, including its identifier and information on how it was built.
/// </summary>
internal record Asset<TAsset, TSettings>(AssetId Id, TAsset Value, AssetBuildMetaData<TSettings> BuildMetaData)
    where TAsset : class;

/// <summary>
/// Record of the exact transcoder, settings and files used to build the asset.
/// </summary>
internal record class AssetBuildMetaData<TSettings>(Guid TranscoderId, int TranscoderVersion, TSettings Settings, IReadOnlyList<Dependency> Dependencies);

/// <summary>
/// A file used to build the asset and the date and time it was last changed
/// </summary>
internal sealed record Dependency(FilePath File, DateTime Version);
