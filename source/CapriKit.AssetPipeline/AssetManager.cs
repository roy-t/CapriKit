using CapriKit.Concurrency.Async;
using CapriKit.Concurrency.Primitives;
using CapriKit.IO;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Manages the building, loading, caching, clean-up and hot-reloading of assets.
/// </summary>
public sealed partial class AssetManager : IDisposable
{
    private readonly ILogger<AssetManager> Logger;
    private readonly ScopedFileSystem FileSystem;
    private readonly AssetPool Cache;
    private readonly HotReloadManager HotReloadManager;
    private readonly ConcurrentDictionary<Type, IAssetTranscoder> Transcoders;

    private readonly LightweightChannel<LoadResult> Incoming;
    private readonly Lock RequestLock;
    private readonly Dictionary<AssetId, List<AssetHandle>> Outstanding;

    // Every bundle that holds, or is going to hold, leases. Kept so that Dispose can name whoever forgot
    // to unload instead of only reporting that some number of assets was left behind.
    private readonly HashSet<AssetBundle> LiveBundles;

    public AssetManager(ILoggerFactory logger, ScopedFileSystem fileSystem)
    {
        Logger = logger.CreateLogger<AssetManager>();
        FileSystem = fileSystem;
        Cache = new();
        HotReloadManager = new(logger, Cache, FileSystem);
        Transcoders = [];
        Incoming = new();
        RequestLock = new();
        Outstanding = [];
        LiveBundles = [];
    }

    /// <summary>
    /// Register a transcoder for the given asset type. Registering a transcoders for a type
    /// that was already assigned a transcoder throws an exception.
    /// Threading: thread-safe, multiple threads can register transcoders at the same time.
    /// </summary>
    public void RegisterTranscoder<TAsset, TSettings>(IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        var key = typeof(TAsset);
        var original = Transcoders.GetOrAdd(key, transcoder);
        if (original != transcoder)
        {
            throw new Exception($"A transcoder for key: {key.FullName} was already registered");
        }
    }

    /// <summary>
    /// Creates a bundle to load assets into. The bundle owns everything loaded into it until it is
    /// unloaded, so it is the thing to keep and to dispose. The call site is captured so that a bundle that
    /// is never unloaded can point at the code that created it, callers should not pass those two arguments.
    /// Threading: thread-safe.
    /// </summary>
    public AssetBundle CreateBundle([CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        var bundle = new AssetBundle(this, $"{Path.GetFileName(file.AsSpan())}:{line}");
        lock (RequestLock) { LiveBundles.Add(bundle); }

        return bundle;
    }

    /// <summary>
    /// Starts loading an asset. The asset will either be loaded from the cache, from disk, or rebuild and then loaded.
    /// The caller gets a handle to be used in an <see cref="AssetBundle"/> which can be resolved
    /// to the actual asset when loading finishes using <see cref="AssetBundleLoader{TBundle}.IsReady"/>
    /// Threading: the manager's own state is safe to touch from any thread concurrently, and this method
    /// guarantees that the same asset is not loaded multiple times concurrently. The bundle above it is
    /// what limits callers to one thread, see <see cref="AssetBundle"/>.
    /// </summary>
    internal AssetHandle<TAsset> Load<TAsset, TSettings>(AssetId id, TSettings settings)
        where TAsset : class
    {
        var transcoder = GetTranscoder<TAsset, TSettings>();
        var handle = new AssetHandle<TAsset>(id);

        // At this time an asset is either already loaded, already requested or requested for the first time.
        // The lock ensure that this does not change while we check what we should do with the request.
        lock (RequestLock)
        {
            // Check if the asset was loaded before
            if (Cache.TryLease<TAsset>(id, out var cachedAsset))
            {
                LogLoadedFromCache(Logger, id);
                handle.Resolve(cachedAsset);
                return handle;
            }

            // Check if the asset was already requested
            if (Outstanding.TryGetValue(id, out var requestors))
            {
                requestors.Add(handle);
            }
            else
            {
                // Request the asset
                Outstanding[id] = [handle];
                Task.Run(() => RequestAsset(id, settings, transcoder)).FireAndForget(
                ex =>
                {
                    LogFailed(Logger, id);
                    Incoming.Write(LoadResult.Failed(new AssetLoadException(id, ex.SourceException)));
                });
            }
        }

        return handle;
    }

    /// <summary>
    /// Performs the actual loading or building and loading of the asset.
    /// Threading: The caller has to guarantee that this method does not run concurrently for the same asset-id.
    /// </summary>
    private async Task RequestAsset<TAsset, TSettings>(AssetId id, TSettings settings, IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        // Check if the asset can be loaded from an up-to-date build
        var build = await AssetDecoder.TryDecodeBuildMetaData(id, transcoder, FileSystem);
        if (build != default && IsUpToDate(transcoder, settings, build, FileSystem))
        {
            var upToDateAsset = await AssetDecoder.Decode(id, transcoder, FileSystem);
            Incoming.Write(LoadResult.Success(id, () => TrackAndTakeLease(upToDateAsset, transcoder)));
            LogLoadedFromFile(Logger, id);
        }
        else // If not, try to rebuild and load the asset
        {
            if (!FileSystem.Exists(id.Path))
            {
                throw new FileNotFoundException("Could not find primary file to build asset from", id.Path);
            }

            await AssetEncoder.Encode(id, transcoder, settings, FileSystem);
            var freshAsset = await AssetDecoder.Decode(id, transcoder, FileSystem);
            Incoming.Write(LoadResult.Success(id, () => TrackAndTakeLease(freshAsset, transcoder)));
            LogBuildAndLoaded(Logger, id);
        }
    }

    /// <summary>
    /// Unloads all assets in the bundle. A bundle that failed to load can be unloaded too: the assets in it
    /// that did load are returned, and the ones that failed never took anything that needs returning. So can
    /// a bundle that is still loading, the leases its assets take when they do arrive are returned right away.
    /// Unloading the same bundle twice is safe, the second call does nothing.
    /// Threading: the lock makes unloading safe against <see cref="Update"/> and against a second unload of
    /// the same bundle, including from another thread. It does not make unloading safe against
    /// <see cref="AssetBundle.Load{TAsset}(AssetId)"/> on that same bundle, which touches the same list
    /// without the lock.
    /// </summary>
    internal void Unload(AssetBundle bundle)
    {
        try
        {
            RequestLock.Enter();
            if (bundle.IsActive)
            {
                // Give back exactly what was taken. The pool counts one lease per resolved handle, so an
                // asset this bundle asked for twice has to be returned twice, and one that failed to load
                // or never arrived has nothing to return.
                foreach (var handle in bundle.Handles)
                {
                    if (handle.IsLoaded) { Cache.Return(handle.Id); }
                }

                LiveBundles.Remove(bundle);
            }
        }
        finally
        {
            // Marks the handles that have not arrived yet as unwanted, Update returns their leases for us.
            bundle.IsActive = false;
            RequestLock.Exit();
        }
    }

    /// <summary>
    /// Materializes assets that have finished loading, removes unused items from the cache
    /// and hot-reloads changed assets.
    /// A failed load does not throw here, it is handed to the handles that were waiting for it and
    /// surfaces from <see cref="AssetBundleLoader{TBundle}.IsReady"/> instead. Update can still throw for
    /// reasons that are not about one asset failing to build: an asset id used for two different asset
    /// types, or an asset whose own Dispose throws while the pool cleans up.
    /// Threading: Should only be called from the primary thread.
    /// </summary>
    public void Update()
    {
        // Ensure that while we check which assets are done loading and up-to-date that administration
        // a new request to `Load` of the same asset does not miss these update.
        lock (RequestLock)
        {
            while (Incoming.TryRead(out var result))
            {
                // Whether it loaded or failed the request is over, so it stops accepting handles. Doing that
                // here, under the lock that Load uses, is what stops a handle joining a dead request: it
                // either made this list, or it misses and its own Load starts a fresh request.
                if (!Outstanding.Remove(result.Id, out var waiting)) { continue; }

                var abandoned = 0;
                foreach (var handle in waiting)
                {
                    if (result.Failure is not null) { handle.Fail(result.Failure); }
                    else
                    {
                        handle.Resolve(result.Materialize!());

                        // The bundle was unloaded while this asset was still on its way. Unload could not
                        // return the lease that resolving just took, because back then there was nothing
                        // to return yet, so this is where it has to be given back.
                        if (handle.Owner is { IsActive: false }) { abandoned++; }
                    }
                }

                // Counted first and returned after, because resolving takes one lease per handle and the
                // pool evicts at zero: returning as we went would drop an asset that two handles of the
                // same bundle asked for to zero in between, and queue it for disposal twice.
                // Returning rather than skipping the materialize is deliberate too, it routes the asset
                // through the pool, which is what disposes it.
                for (var i = 0; i < abandoned; i++) { Cache.Return(result.Id); }
            }
        }

        Cache.DisposeReleased();
        HotReloadManager.Update();
    }

    /// <summary>
    /// Shuts the asset manager down. Bundles that were never unloaded are reported but deliberately not
    /// unloaded for you: this only runs at shutdown, where there is nobody left to hand the assets to, so
    /// quietly cleaning up after the caller would only hide the bug that the log and the exception report.
    /// Threading: Should only be called from the primary thread.
    /// </summary>
    public void Dispose()
    {
        // Every step below runs even if an earlier one threw. This is the last chance to hand assets back
        // to the pool and to name the bundles that were left behind, so one failing step must not take the
        // others with it: swallowing here costs a log line, skipping would cost the whole diagnostic.

        // Dispose the hot-reload manager first so that it can release any reference to
        // assets it might still hold.
        try { HotReloadManager.Dispose(); }
        catch (Exception ex) { LogShutdownStepFailed(Logger, nameof(HotReloadManager), ex); }

        try { DrainIncoming(); }
        catch (Exception ex) { LogShutdownStepFailed(Logger, nameof(DrainIncoming), ex); }

        try { ReportAssetsThatWereNeverUnloaded(); }
        catch (Exception ex) { LogShutdownStepFailed(Logger, nameof(ReportAssetsThatWereNeverUnloaded), ex); }

        // Deliberately not guarded: the leak it throws about is the whole point of the check.
        Cache.Dispose();
    }

    /// <summary>
    /// Settles the loads that finished after the last <see cref="Update"/>. Those assets were decoded but
    /// never reached a handle, so nobody leases them and nothing would ever dispose them. Taking the lease
    /// and immediately giving it back moves them through the pool, which does dispose them.
    /// </summary>
    private void DrainIncoming()
    {
        lock (RequestLock)
        {
            while (Incoming.TryRead(out var result))
            {
                Outstanding.Remove(result.Id);

                // A failed load never built anything, so there is nothing to dispose of.
                if (result.Failure is not null) { continue; }

                result.Materialize!();
                Cache.Return(result.Id);
            }
        }
    }

    /// <summary>
    /// Names every bundle that still holds leases, so that the leak the <see cref="AssetPool"/> throws
    /// about can be traced back to the code that caused it.
    /// </summary>
    private void ReportAssetsThatWereNeverUnloaded()
    {
        lock (RequestLock)
        {
            foreach (var bundle in LiveBundles)
            {
                // A bundle that never loaded anything is simply unused, not a leak.
                if (bundle.Total > 0)
                {
                    LogBundleNotUnloaded(Logger, bundle.Origin, bundle.Total);
                }
            }
        }
    }

    // Thread safe, only touches the file system and uses thread safe transcoder methods and properties.
    private static bool IsUpToDate<TAsset, TSettings>(IAssetTranscoder<TAsset, TSettings> transcoder, TSettings settings, AssetBuildMetaData<TSettings> build, IReadOnlyVirtualFileSystem fileSystem)
        where TAsset : class
    {
        // Transcoders differ
        if (transcoder.Id != build.TranscoderId || transcoder.Version != build.TranscoderVersion)
        {
            return false;
        }

        // Settings differ
        var inUse = new ArrayBufferWriter<byte>();
        transcoder.WriteSettings(settings, inUse);

        var inFile = new ArrayBufferWriter<byte>();
        transcoder.WriteSettings(build.Settings, inFile);

        if (!inUse.WrittenSpan.SequenceEqual(inFile.WrittenSpan))
        {
            return false;
        }

        // Dependencies have changed
        foreach (var (file, version) in build.Dependencies)
        {
            // Treat a file as up-to-date if does not exist
            if (fileSystem.Exists(file))
            {
                // Otherwise double check the file date matches
                var lastWrite = fileSystem.LastWriteTime(file);
                if (version != lastWrite)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Used when assigning newly loaded assets to their handles. Puts the new asset into the cache,
    /// tracks it and then takes a lease for the handle.
    /// </summary>
    private TAsset TrackAndTakeLease<TAsset, TSettings>(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        // Though the asset manager does not allow loading the same asset multiple times the cache contract does allow it
        var actualObject = Cache.PutOrLease(asset.Id, asset.Value);

        var actualWrapper = new Asset<TAsset, TSettings>(asset.Id, actualObject, asset.BuildMetaData);
        HotReloadManager.Track(actualWrapper, transcoder);
        return actualObject;
    }

    // Thread safe because Transcoders is thread safe
    private IAssetTranscoder<TAsset, TSetting> GetTranscoder<TAsset, TSetting>() where TAsset : class
    {
        var type = typeof(TAsset);
        if (!Transcoders.TryGetValue(type, out var transcoder))
        {
            throw new Exception($"Missing transcoder for asset type: {type.FullName}");
        }

        if (transcoder is not IAssetTranscoder<TAsset, TSetting> specializedTranscoder)
        {
            throw new Exception($"Expected transcoder of type: {type.FullName} but got transcoder of type: {transcoder.GetType().FullName}");
        }

        return specializedTranscoder;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded asset: {asset} from cache")]
    private static partial void LogLoadedFromCache(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded up-to-date asset: {asset} from file")]
    private static partial void LogLoadedFromFile(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Information, Message = "Build and loaded fresh asset: {asset}")]
    private static partial void LogBuildAndLoaded(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Error, Message = "Building or loading asset: {asset} failed.")]
    private static partial void LogFailed(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Error, Message = "The asset bundle created at {origin} was never unloaded, it still holds {assets} asset(s)")]
    private static partial void LogBundleNotUnloaded(ILogger logger, string origin, int assets);

    [LoggerMessage(Level = LogLevel.Error, Message = "Shutting the asset manager down failed during {step}, the remaining steps still ran")]
    private static partial void LogShutdownStepFailed(ILogger logger, string step, Exception exception);
}
