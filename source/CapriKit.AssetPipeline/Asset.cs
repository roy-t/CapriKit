using CapriKit.IO;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Unique asset identifier
/// </summary>
/// <param name="Path">Virtual file path that points to the file the asset originates from.</param>
/// <param name="Key">Optional key to a sub-resources in Path.</param>
public record AssetId(FilePath Path, string Key = "");

internal record Asset<TAsset, TSettings>(AssetId Id, TAsset Value, AssetBuildMetaData<TSettings> BuildMetaData)
    where TAsset : class;

internal record class AssetBuildMetaData<TSettings>(Guid TranscoderId, int TranscoderVersion, TSettings Settings, IReadOnlyList<Dependency> Dependencies);

internal sealed record Dependency(FilePath File, DateTime Version);
