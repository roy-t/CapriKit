using CapriKit.IO;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline;

/// <summary>
/// A freshly rebuilt asset that is waiting for the main thread to move it into the live instance.
/// </summary>
/// <param name="Dependencies">Files read while rebuilding, used to keep the file-to-asset map up-to-date.</param>
/// <param name="HotSwap">
/// Moves the rebuilt data into the live instance. Threading: main thread only, see <see cref="IAssetTranscoder{TAsset, TSettings}.HotSwap"/>.
/// </param>
internal sealed record ReloadedAsset(IReadOnlyList<Dependency> Dependencies, Action HotSwap);

/// <summary>
/// Everything the <see cref="HotReloadManager"/> needs to rebuild a single asset. The asset and settings
/// types are erased so that assets of every type can live in one collection.
/// </summary>
internal abstract class TrackedAsset(AssetId id, IReadOnlyList<Dependency> dependencies)
{
    public AssetId Id { get; } = id;

    /// <summary>
    /// The files that the most recent successful build of this asset read.
    /// Threading: only touched by the main thread while it holds the manager's lock.
    /// </summary>
    public IReadOnlyList<Dependency> Dependencies { get; set; } = dependencies;

    /// <summary>
    /// Leases the live asset from the cache and then starts rebuilding it on the thread pool. Returns false if
    /// the asset is no longer in the cache, in which case no lease was taken and nothing was started.
    /// The lease is taken before the task starts so that the caller always knows whether a lease exists, the
    /// caller owns that lease and must return it once <paramref name="reload"/> has completed.
    /// Threading: main thread only.
    /// </summary>
    public abstract bool TryStartReload(AssetPool cache, IVirtualFileSystem fileSystem, [NotNullWhen(true)] out Task<ReloadedAsset>? reload);
}

/// <inheritdoc cref="TrackedAsset"/>
internal sealed class TrackedAsset<TAsset, TSettings> : TrackedAsset
    where TAsset : class
{
    private readonly TSettings Settings;
    private readonly IAssetTranscoder<TAsset, TSettings> Transcoder;

    public TrackedAsset(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
        : base(asset.Id, asset.BuildMetaData.Dependencies)
    {
        Settings = asset.BuildMetaData.Settings;
        Transcoder = transcoder;
    }

    public override bool TryStartReload(AssetPool cache, IVirtualFileSystem fileSystem, [NotNullWhen(true)] out Task<ReloadedAsset>? reload)
    {
        // The lease pins the live instance for the entire rebuild, so the background thread never has to
        // wonder whether the object it is going to hot-swap into still exists.
        if (!cache.TryLease<TAsset>(Id, out var live))
        {
            reload = null;
            return false;
        }

        reload = Task.Run(() => Reload(live, fileSystem));
        return true;
    }

    private async Task<ReloadedAsset> Reload(TAsset live, IVirtualFileSystem fileSystem)
    {
        // Build into memory rather than over the existing build on disk: other threads may be reading that
        // file to load the very same asset and overwriting it underneath them would fail those loads.
        using var stream = new MemoryStream();
        await AssetEncoder.Encode(Id, Transcoder, Settings, fileSystem, stream);

        stream.Seek(0, SeekOrigin.Begin);
        var rebuilt = await AssetDecoder.Decode(Id, Transcoder, fileSystem, stream);

        return new ReloadedAsset(rebuilt.BuildMetaData.Dependencies, () => Transcoder.HotSwap(live, rebuilt.Value));
    }
}
