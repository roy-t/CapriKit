using CapriKit.IO;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Unique asset identifier
/// </summary>
/// <param name="Key">Optional key to a sub-resources in Path.</param>
/// <param name="Path">Virtual file path that points to the file the asset originates from.</param>
public record AssetId(string Key, FilePath Path);


/// <summary>
/// An active asset
/// </summary>
/// <typeparam name="TAsset"></typeparam>
/// <param name="Id">The unique id, refers to a virtual file location (id.Path) and if required a sub-resource in that file (id.Key).</param>
/// <param name="Value">The asset</param>
/// <param name="Settings">The settings used for encoding/decoding</param>
/// <param name="Dependencies">Files that this asset depends on, if any of these files changed the asset needs te be rebuild.</param>
public sealed record Asset<TAsset>(AssetId Id, TAsset Value, IAssetSettings<TAsset> Settings, IReadOnlyList<Dependency> Dependencies)
    where TAsset : class;

public sealed record Dependency(FilePath File, DateTime Version);


/// <summary>
/// Result of building an asset
/// </summary>
public sealed class AssetJob<TAsset>
    where TAsset : class

{
    private readonly Asset<TAsset>? Asset;
    private readonly ExceptionDispatchInfo? Exception;

    private AssetJob(AssetId id, Asset<TAsset>? asset, ExceptionDispatchInfo? exception)
    {
        Id = id;
        Asset = asset;
        Exception = exception;
    }

    public AssetId Id { get; }

    public bool OnSuccess([NotNullWhen(true)] out Asset<TAsset>? asset)
    {
        asset = Asset;
        return asset != null;
    }

    public bool OnFailure([NotNullWhen(true)] out ExceptionDispatchInfo? exception)
    {
        exception = Exception;
        return exception != null;
    }

    public bool OnMissing()
    {
        return Asset == null && Exception == null;
    }

    public static AssetJob<TAsset> Failure(AssetId id, ExceptionDispatchInfo exception)
    {
        return new AssetJob<TAsset>(id, null, exception);
    }

    public static AssetJob<TAsset> Success(AssetId id, Asset<TAsset> asset)
    {
        return new AssetJob<TAsset>(id, asset, null);
    }

    public static AssetJob<TAsset> Missing(AssetId id)
    {
        return new AssetJob<TAsset>(id, null, null);
    }

    public void Match(Action<AssetId, Asset<TAsset>> onSuccess, Action<AssetId, ExceptionDispatchInfo> onFailure, Action<AssetId> onMissing)
    {
        if (Asset != null) { onSuccess(Id, Asset); }
        else if (Exception != null) { onFailure(Id, Exception); }
        else { onMissing(Id); }
    }

    public TReturn Match<TReturn>(Func<AssetId, Asset<TAsset>, TReturn> onSuccess,
        Func<AssetId, ExceptionDispatchInfo, TReturn> onFailure,
        Func<AssetId, TReturn> onMissing)
    {
        if (Asset != null) { return onSuccess(Id, Asset); }
        if (Exception != null) { return onFailure(Id, Exception); }
        return onMissing(Id);
    }
}

