using CapriKit.IO;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Unique asset identifier
/// </summary>
/// <param name="Key">Optional key to a sub-resources in Path.</param>
/// <param name="Path">Virtual file path that points to the file the asset originates from.</param>
public record AssetId(string Key, FilePath Path);

internal record Asset<TAsset, TSettings>(AssetId Id, TAsset Value, AssetBuildMetaData<TSettings> BuildMetaData)
    where TAsset : class;

internal record class AssetBuildMetaData<TSettings>(Guid TranscoderId, int TranscoderVersion, TSettings Settings, FilePath OutputFile, IReadOnlyList<Dependency> Dependencies);

internal sealed record Dependency(FilePath File, DateTime Version);
