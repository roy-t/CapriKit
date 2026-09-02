using CapriKit.Collections;
using CapriKit.Concurrency.Async;
using CapriKit.Concurrency.Primitives;
using CapriKit.IO;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

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

    private readonly LightweightChannel<Result> Incoming;
    private readonly Lock RequestLock;
    private readonly Dictionary<AssetId, OneOrMany<IAssetRequester>> Outstanding;

    // Every requester that holds, or is going to hold, leases. Kept so that Dispose can name whoever forgot
    // to unload instead of only reporting that some number of assets was left behind.
    private readonly Dictionary<IAssetRequester, Registration> LiveRequesters;

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

        // Requesters are identified by who they are, never by what they consider equal to themselves.
        LiveRequesters = new(ReferenceEqualityComparer.Instance);
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
    /// Announces a requester that is about to ask for assets, so that one which is never unloaded can be
    /// named at shutdown.
    /// Threading: thread-safe.
    /// </summary>
    internal void Register(IAssetRequester requester, string origin)
    {
        lock (RequestLock) { LiveRequesters[requester] = new Registration(origin); }
    }

    /// <summary>
    /// Starts loading an asset and delivers it to <paramref name="requester"/> once it arrives. The asset is
    /// either taken from the cache, loaded from disk, or rebuilt and then loaded. An asset that is already
    /// cached is delivered before this method returns.
    /// Threading: complex, multiple threads can enter this method, but the thread that owns <paramref name="requester"/>
    /// must ensure that no other threads touch it until this method returns. This method
    /// guarantees that the same asset is not built multiple times concurrently.
    /// </summary>
    internal void Load<TAsset, TSettings>(AssetId id, TSettings settings, IAssetRequester requester)
        where TAsset : class
    {
        // Looked up before the cache is even checked, so that a missing or mismatched transcoder throws immediately.
        var transcoder = GetTranscoder<TAsset, TSettings>();

        // At this time an asset is either already loaded, already requested or requested for the first time.
        // The lock ensure that this does not change while we check what we should do with the request.
        lock (RequestLock)
        {
            // Check if the asset was loaded before
            if (Cache.TryLease<TAsset>(id, out var cachedAsset))
            {
                LogLoadedFromCache(Logger, id);

                // Only one requester here, so a refusal can hand the lease straight back.
                if (!Deliver(id, requester, JobResult<object>.Success(id.ToString(), cachedAsset)))
                {
                    Cache.Return(id);
                }

                return;
            }

            // Join the request if this asset is already on its way, otherwise start one.
            ref var waiting = ref CollectionsMarshal.GetValueRefOrAddDefault(Outstanding, id, out var alreadyRequested);
            waiting.Add(requester);

            if (!alreadyRequested)
            {
                Task.Run(() => RequestAsset(id, settings, transcoder)).FireAndForget(
                ex =>
                {
                    LogFailed(Logger, id);
                    Incoming.Write(Result.Failed(new AssetLoadException(id, ex.SourceException)));
                });
            }
        }
    }

    /// <summary>
    /// Transfers the ownership of a loaded and leased asset toward the requester and records the lease that
    /// a successful hand-over creates. Giving back the lease of a refused asset is left to the caller, which
    /// is the only one that knows whether more requesters are still waiting for it.
    /// Threading: must be called while holding <see cref="RequestLock"/>.
    /// </summary>
    /// <returns>True if the requester took ownership, false if it refused because it was unloaded.</returns>
    private bool Deliver(AssetId id, IAssetRequester requester, JobResult<object> result)
    {
        if (!requester.Accept(id, result)) { return false; }

        // A failed load never took a lease, so there is nothing to record for it.
        if (result.IsSuccess)
        {
            Debug.Assert(LiveRequesters.ContainsKey(requester), "Delivered an asset to a requester that is not registered");

            if (LiveRequesters.TryGetValue(requester, out var registration))
            {
                registration.Leased.Add(id);
            }
        }

        return true;
    }

    /// <summary>
    /// Performs the actual loading or building and loading of the asset.
    /// Threading: the caller has to guarantee that this does not run concurrently for the same asset-id.
    /// </summary>
    private async Task RequestAsset<TAsset, TSettings>(AssetId id, TSettings settings, IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        // Check if the asset can be loaded from an up-to-date build
        var build = await AssetDecoder.TryDecodeBuildMetaData(id, transcoder, FileSystem);
        if (build != default && IsUpToDate(transcoder, settings, build, FileSystem))
        {
            var upToDateAsset = await AssetDecoder.Decode(id, transcoder, FileSystem);
            Incoming.Write(Result.Success(id, () => TrackAndTakeLease(upToDateAsset, transcoder)));
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
            Incoming.Write(Result.Success(id, () => TrackAndTakeLease(freshAsset, transcoder)));
            LogBuildAndLoaded(Logger, id);
        }
    }

    /// <summary>
    /// Hands back every lease the requester was given and forgets it. The manager records those leases as
    /// it hands them out, so unloading needs nothing but the requester's identity. An asset that is still
    /// on its way was never leased to it and is returned when it arrives and the requester refuses it.
    /// Threading: complex, multiple threads can enter this method, but the thread that owns <paramref name="requester"/>
    /// must ensure that no other threads touch it until this method returns.
    /// </summary>
    internal void Unload(IAssetRequester requester)
    {
        lock (RequestLock)
        {
            // Taking the registration out is what makes a second unload a no-op.
            if (!LiveRequesters.Remove(requester, out var registration)) { return; }

            foreach (var id in registration.Leased) { Cache.Return(id); }
        }
    }

    /// <summary>
    /// Materializes assets that have finished loading, removes unused items from the cache
    /// and hot-reloads changed assets.
    /// A failed load does not throw here, it is handed to the requesters that were waiting for it 
    /// Threading: Should only be called from the primary thread.
    /// </summary>
    public void Update()
    {
        // Ensure that while we check which assets are done loading and up-to-date that
        // a new request to `Load` of the same asset does not miss these update.
        lock (RequestLock)
        {
            while (Incoming.TryRead(out var result))
            {
                if (!Outstanding.Remove(result.Id, out var waiting)) { continue; } // Should never happen

                var refused = 0;
                foreach (var requester in waiting)
                {
                    if (result.Failure is not null)
                    {
                        // A failed load never took a lease, so a requester turning it down costs nothing.
                        var failure = JobResult<object>.Failure(result.Id.ToString(), ExceptionDispatchInfo.Capture(result.Failure));
                        Deliver(result.Id, requester, failure);
                    }
                    else
                    {
                        var asset = result.Materialize!(); // materialize every time so the reference count is correct
                        var success = JobResult<object>.Success(result.Id.ToString(), asset);
                        if (!Deliver(result.Id, requester, success)) { refused++; }
                    }
                }

                // Counted first and returned after, because materializing takes one lease per requester and
                // the pool evicts at zero: returning as we went would drop an asset that two bundles are
                // waiting for to zero in between, which queues it for disposal and lets the next
                // materialize put that very same instance back in the pool as if it were new.
                for (var i = 0; i < refused; i++) { Cache.Return(result.Id); }
            }
        }

        Cache.DisposeReleased();
        HotReloadManager.Update();
    }

    /// <summary>
    /// Shuts the asset manager down. Logs asset that you failed to unload.
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
    /// never reached a requester, so nobody leases them and nothing would ever dispose them. Taking the lease
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
    /// Logs every requester that still holds leases, so that the leak the <see cref="AssetPool"/> throws
    /// about can be traced back to the code that caused it.
    /// </summary>
    private void ReportAssetsThatWereNeverUnloaded()
    {
        lock (RequestLock)
        {
            foreach (var registration in LiveRequesters.Values)
            {
                // A bundle that never received anything is simply unused, not a leak.
                if (registration.Leased.Count > 0)
                {
                    LogBundleNotUnloaded(Logger, registration.Origin, registration.Leased.Count);
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
    /// Used when handing newly loaded assets to their requesters. Puts the new asset into the cache,
    /// tracks it and then takes a lease for the requester.
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

    // Union to hold build results, TODO: replace with a proper union type when switching to C# 15
    private readonly record struct Result(AssetId Id, Func<object>? Materialize, AssetLoadException? Failure)
    {
        public static Result Success(AssetId id, Func<object> materialize) => new(id, materialize, null);

        public static Result Failed(AssetLoadException failure) => new(failure.Asset, null, failure);
    }

    /// <summary>
    /// One live requester: where it was created, and every lease it has been handed. Recorded here rather
    /// than asked of the requester, so that unloading it and reporting it as a leak both need nothing but
    /// its identity. This makes the manager the only place that knows who leases what.
    /// </summary>
    private sealed class Registration(string origin)
    {
        public string Origin { get; } = origin;
        public List<AssetId> Leased { get; } = [];
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
