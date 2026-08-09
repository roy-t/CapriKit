using CapriKit.IO;

namespace CapriKit.AssetPipeline.v2;

/// <summary>
/// Unique asset identifier
/// </summary>
/// <param name="Key">Optional key to a sub-resources in Path.</param>
/// <param name="Path">Virtual file path that points to the file the asset originates from.</param>
public record AssetId(string Key, FilePath Path);

public record Asset<TAsset, TSettings>(AssetId Id, TAsset Value, AssetBuildMetaData<TSettings> BuildMetaData)
    where TAsset : class;

public record class AssetBuildMetaData<TSettings>(Guid TranscoderId, int TranscoderVersion, TSettings Settings, IReadOnlyList<Dependency> Dependencies);

public sealed record Dependency(FilePath File, DateTime Version);
