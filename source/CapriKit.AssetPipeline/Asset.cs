using CapriKit.IO;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Unique asset identifier
/// </summary>
/// <param name="Path">Virtual file path that points to the file the asset originates from.</param>
/// <param name="Key">Optional key to a sub-resources in Path.</param>
public record AssetId(FilePath Path, string Key = "")
{
    public override string ToString()
    {
        if (string.IsNullOrEmpty(Key))
        {
            return Path;
        }

        return $"{Path}:{Key}";
    }
}

/// <summary>
/// An asset
/// </summary>
internal abstract record Asset(AssetId Id);

/// <summary>
/// An asset, including its identifier
/// </summary>
internal record Asset<TAsset>(AssetId Id, TAsset Value) : Asset(Id)
    where TAsset : class;

/// <summary>
/// An asset, including its identifier and information on how it was built.
/// </summary>
internal sealed record Asset<TAsset, TSettings>(AssetId Id, TAsset Value, AssetBuildMetaData<TSettings> BuildMetaData) : Asset<TAsset>(Id, Value)
    where TAsset : class;

/// <summary>
/// One finished load request handed back to the main thread: either a materializer that puts the asset in
/// the cache and takes a lease on it, or the failure that stopped the load.
/// </summary>
internal readonly record struct LoadResult(AssetId Id, Func<object>? Materialize, AssetLoadException? Failure)
{
    public static LoadResult Success(AssetId id, Func<object> materialize) => new(id, materialize, null);

    public static LoadResult Failed(AssetLoadException failure) => new(failure.Asset, null, failure);
}

/// <summary>
/// Record of the exact transcoder, settings and files used to build the asset.
/// </summary>
internal record class AssetBuildMetaData<TSettings>(Guid TranscoderId, int TranscoderVersion, TSettings Settings, IReadOnlyList<Dependency> Dependencies);

/// <summary>
/// A file used to build the asset and the date and time it was last changed
/// </summary>
internal sealed record Dependency(FilePath File, DateTime Version);
