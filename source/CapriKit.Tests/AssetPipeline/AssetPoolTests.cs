using CapriKit.AssetPipeline;

namespace CapriKit.Tests.AssetPipeline;

/// <summary>
/// The pool is where the loading threads, the hot-reloader and the main thread meet. A mistake in its
/// reference counting either disposes an asset that is still in use or leaks it, so these tests go beyond
/// the happy path and pin down the lease/return contract, the deferred disposal and the threading.
/// </summary>
internal class AssetPoolTests
{
    private static readonly AssetId Id = new("Hello.txt");
    private static readonly AssetId OtherId = new("Goodbye.txt");

    [Test]
    public async Task PutOrLease()
    {
        var pool = new AssetPool();
        var resource = new Resource();

        var stored = pool.PutOrLease(Id, resource);

        await Assert.That(stored).IsSameReferenceAs(resource);
        await Assert.That(resource.DisposeCount).IsEqualTo(0);

        // Putting an asset takes the first lease on it, returning that lease releases the asset again
        pool.Return(Id);
        pool.DisposeReleased();
        await Assert.That(resource.DisposeCount).IsEqualTo(1);

        pool.Dispose();
    }

    [Test]
    public async Task PutOrLease_DisposesTheLoserAndLeasesTheWinner()
    {
        var pool = new AssetPool();
        var winner = new Resource();
        var loser = new Resource();

        pool.PutOrLease(Id, winner);
        var stored = pool.PutOrLease(Id, loser);

        await Assert.That(stored).IsSameReferenceAs(winner);

        // Disposal always waits for the main thread, never happens inside PutOrLease itself
        await Assert.That(loser.DisposeCount).IsEqualTo(0);
        pool.DisposeReleased();
        await Assert.That(loser.DisposeCount).IsEqualTo(1);
        await Assert.That(winner.DisposeCount).IsEqualTo(0);

        // Both calls took a lease on the winner, so both have to be returned
        pool.Return(Id);
        pool.DisposeReleased();
        await Assert.That(winner.DisposeCount).IsEqualTo(0);

        pool.Return(Id);
        pool.DisposeReleased();
        await Assert.That(winner.DisposeCount).IsEqualTo(1);

        pool.Dispose();
    }

    [Test]
    public async Task PutOrLease_LeasesWithoutDisposingWhenTheSameInstanceIsPutTwice()
    {
        // The asset manager materializes an asset once per waiting handle but always from the same loaded
        // instance, so putting the identical object twice is a normal path rather than a corner case.
        var pool = new AssetPool();
        var resource = new Resource();

        pool.PutOrLease(Id, resource);
        var stored = pool.PutOrLease(Id, resource);

        await Assert.That(stored).IsSameReferenceAs(resource);

        // The instance is live and now leased twice, collecting must not touch it
        pool.DisposeReleased();
        await Assert.That(resource.DisposeCount).IsEqualTo(0);

        pool.Return(Id);
        pool.DisposeReleased();
        await Assert.That(resource.DisposeCount).IsEqualTo(0);

        pool.Return(Id);
        pool.DisposeReleased();
        await Assert.That(resource.DisposeCount).IsEqualTo(1);

        pool.Dispose();
    }

    [Test]
    public async Task PutOrLease_ThrowsWhenTheAssetIsStoredAsAnotherType()
    {
        var pool = new AssetPool();
        var stored = new Resource();
        var wrongType = new OtherResource();

        pool.PutOrLease(Id, stored);

        await Assert.That(() => pool.PutOrLease(Id, wrongType)).Throws<InvalidOperationException>();

        // The rejected candidate is still cleaned up, and the failed call did not take a lease
        pool.DisposeReleased();
        await Assert.That(wrongType.DisposeCount).IsEqualTo(1);
        await Assert.That(stored.DisposeCount).IsEqualTo(0);

        pool.Return(Id);
        pool.DisposeReleased();
        await Assert.That(stored.DisposeCount).IsEqualTo(1);

        pool.Dispose();
    }

    [Test]
    public async Task TryLease()
    {
        var pool = new AssetPool();
        var resource = new Resource();
        pool.PutOrLease(Id, resource);

        var found = pool.TryLease<Resource>(Id, out var leased);

        await Assert.That(found).IsTrue();
        await Assert.That(leased).IsSameReferenceAs(resource);

        pool.Return(Id);
        pool.Return(Id);
        pool.DisposeReleased();
        pool.Dispose();
    }

    [Test]
    public async Task TryLease_ReturnsFalseForAnUnknownAsset()
    {
        var pool = new AssetPool();

        var found = pool.TryLease<Resource>(Id, out var leased);

        await Assert.That(found).IsFalse();
        await Assert.That(leased).IsNull();

        pool.Dispose();
    }

    [Test]
    public async Task TryLease_ReturnsFalseForAnAssetThatIsWaitingToBeDisposed()
    {
        // Returning the last lease evicts the asset immediately and only defers the disposal itself, so a
        // late lease has to miss instead of handing out an asset that is about to be disposed.
        var pool = new AssetPool();
        var resource = new Resource();
        pool.PutOrLease(Id, resource);
        pool.Return(Id);

        var found = pool.TryLease<Resource>(Id, out var leased);

        await Assert.That(found).IsFalse();
        await Assert.That(leased).IsNull();

        pool.DisposeReleased();
        await Assert.That(resource.DisposeCount).IsEqualTo(1);

        pool.Dispose();
    }

    [Test]
    public async Task TryLease_ThrowsWhenTheAssetIsStoredAsAnotherType()
    {
        var pool = new AssetPool();
        var resource = new Resource();
        pool.PutOrLease(Id, resource);

        await Assert.That(() => pool.TryLease<OtherResource>(Id, out _)).Throws<InvalidOperationException>();

        // The failed lease must not have counted, so a single return still releases the asset
        pool.Return(Id);
        pool.DisposeReleased();
        await Assert.That(resource.DisposeCount).IsEqualTo(1);

        pool.Dispose();
    }

    [Test]
    public async Task Return()
    {
        var pool = new AssetPool();
        var resource = new Resource();
        pool.PutOrLease(Id, resource);
        pool.TryLease<Resource>(Id, out _);

        pool.Return(Id);

        // One lease is left, so the asset stays alive and leaseable
        pool.DisposeReleased();
        await Assert.That(resource.DisposeCount).IsEqualTo(0);
        await Assert.That(pool.TryLease<Resource>(Id, out _)).IsTrue();
        pool.Return(Id);

        pool.Return(Id);

        // The last return evicts the asset but leaves the disposal to the main thread
        await Assert.That(resource.DisposeCount).IsEqualTo(0);
        pool.DisposeReleased();
        await Assert.That(resource.DisposeCount).IsEqualTo(1);

        pool.Dispose();
    }

    [Test]
    public async Task Return_ThrowsForAnAssetThatWasNeverStored()
    {
        var pool = new AssetPool();

        await Assert.That(() => pool.Return(Id)).Throws<InvalidOperationException>();

        pool.Dispose();
    }

    [Test]
    public async Task Return_ThrowsWhenReturnedMoreOftenThanLeased()
    {
        var pool = new AssetPool();
        pool.PutOrLease(Id, new Resource());
        pool.Return(Id);

        // The entry is gone once the last lease came back, so an extra return is a bug in the caller
        await Assert.That(() => pool.Return(Id)).Throws<InvalidOperationException>();

        pool.DisposeReleased();
        pool.Dispose();
    }

    [Test]
    public async Task DisposeReleased()
    {
        var pool = new AssetPool();
        var first = new Resource();
        var second = new Resource();
        pool.PutOrLease(Id, first);
        pool.PutOrLease(OtherId, second);
        pool.Return(Id);
        pool.Return(OtherId);

        pool.DisposeReleased();

        await Assert.That(first.DisposeCount).IsEqualTo(1);
        await Assert.That(second.DisposeCount).IsEqualTo(1);

        // Collecting again must not dispose anything a second time
        pool.DisposeReleased();
        await Assert.That(first.DisposeCount).IsEqualTo(1);
        await Assert.That(second.DisposeCount).IsEqualTo(1);

        pool.Dispose();
    }

    [Test]
    public async Task DisposeReleased_IgnoresAssetsThatAreNotDisposable()
    {
        var pool = new AssetPool();
        pool.PutOrLease(Id, new PlainResource());
        pool.Return(Id);

        pool.DisposeReleased();

        await Assert.That(pool.TryLease<PlainResource>(Id, out _)).IsFalse();

        pool.Dispose();
    }

    [Test]
    public async Task DisposeReleased_DoesNotHoldTheLockWhileDisposing()
    {
        // Assets dispose graphics resources and may well touch the pool themselves. Doing that while
        // holding the lock would deadlock against every other thread that loads or returns an asset.
        var pool = new AssetPool();
        pool.PutOrLease(OtherId, new Resource());

        var otherThreadCouldUseThePool = false;
        var reentrant = new CallbackResource(() =>
        {
            var lease = Task.Run(() => pool.TryLease<Resource>(OtherId, out _));
            otherThreadCouldUseThePool = lease.Wait(TimeSpan.FromSeconds(5)) && lease.Result;
        });

        pool.PutOrLease(Id, reentrant);
        pool.Return(Id);

        pool.DisposeReleased();

        await Assert.That(otherThreadCouldUseThePool).IsTrue();

        pool.Return(OtherId);
        pool.Return(OtherId);
        pool.DisposeReleased();
        pool.Dispose();
    }

    [Test]
    public async Task Dispose()
    {
        var pool = new AssetPool();
        var resource = new Resource();
        pool.PutOrLease(Id, resource);
        pool.Return(Id);

        // Everything came back, so disposing is clean and still collects what was queued
        pool.Dispose();

        await Assert.That(resource.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task Dispose_ThrowsWhenAssetsWereNotReturned()
    {
        var pool = new AssetPool();
        var resource = new Resource();
        pool.PutOrLease(Id, resource);

        // The pool is the only place that can catch a bundle that was never unloaded
        await Assert.That(() => pool.Dispose()).Throws<Exception>();

        // The leaked asset is reported, not disposed: somebody out there still holds a reference to it
        await Assert.That(resource.DisposeCount).IsEqualTo(0);
    }

    [Test]
    public async Task Dispose_OnlyReportsLeaksOnce()
    {
        var pool = new AssetPool();
        pool.PutOrLease(Id, new Resource());

        await Assert.That(() => pool.Dispose()).Throws<Exception>();

        // A pool in a using block is disposed a second time while the first exception unwinds
        await Assert.That(() => pool.Dispose()).ThrowsNothing();
    }

    [Test]
    public async Task Dispose_MakesThePoolUnusable()
    {
        var pool = new AssetPool();
        pool.Dispose();

        await Assert.That(() => pool.PutOrLease(Id, new Resource())).Throws<ObjectDisposedException>();
        await Assert.That(() => pool.TryLease<Resource>(Id, out _)).Throws<ObjectDisposedException>();
        await Assert.That(() => pool.Return(Id)).Throws<ObjectDisposedException>();

        // Collecting stays safe so that a game loop that is shutting down does not have to check
        await Assert.That(() => pool.DisposeReleased()).ThrowsNothing();
    }

    [Test]
    public async Task PutOrLease_IsThreadSafe()
    {
        // Several loading threads can finish the same asset at the same time. Exactly one instance may
        // win, every caller must get a lease on that winner and every loser must be cleaned up.
        const int threads = 16;
        var pool = new AssetPool();
        var candidates = Enumerable.Range(0, threads).Select(_ => new Resource()).ToArray();

        var results = await Task.WhenAll(candidates.Select(candidate => Task.Run(() => pool.PutOrLease(Id, candidate))));

        var winner = results[0];
        await Assert.That(results.Distinct().Count()).IsEqualTo(1);
        await Assert.That(candidates.Count(candidate => ReferenceEquals(candidate, winner))).IsEqualTo(1);

        pool.DisposeReleased();
        await Assert.That(winner.DisposeCount).IsEqualTo(0);
        await Assert.That(candidates.Sum(candidate => candidate.DisposeCount)).IsEqualTo(threads - 1);

        // Every call took a lease, so the winner only dies after the last one comes back
        for (var i = 0; i < threads; i++)
        {
            pool.Return(Id);
        }

        pool.DisposeReleased();
        await Assert.That(winner.DisposeCount).IsEqualTo(1);

        pool.Dispose();
    }

    [Test]
    public async Task TryLease_IsThreadSafe()
    {
        // The reference count is the only thing keeping an asset alive, so it has to survive many threads
        // leasing and returning the same asset at the same time.
        const int threads = 8;
        const int iterations = 500;

        var pool = new AssetPool();
        var resource = new Resource();

        // This lease keeps the asset alive for the entire test
        pool.PutOrLease(Id, resource);

        await Task.WhenAll(Enumerable.Range(0, threads).Select(thread => Task.Run(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                if (pool.TryLease<Resource>(Id, out _))
                {
                    pool.Return(Id);
                }
            }
        })));

        // The asset was never evicted along the way, so only the initial lease is left
        pool.DisposeReleased();
        await Assert.That(resource.DisposeCount).IsEqualTo(0);

        pool.Return(Id);
        pool.DisposeReleased();
        await Assert.That(resource.DisposeCount).IsEqualTo(1);

        pool.Dispose();
    }

    /// <summary>Counts disposals so that tests can tell a missing, a late and a double dispose apart.</summary>
    private class CountingResource : IDisposable
    {
        private int disposeCount;

        public int DisposeCount => Volatile.Read(ref disposeCount);

        public void Dispose() => Interlocked.Increment(ref disposeCount);
    }

    private sealed class Resource : CountingResource;

    /// <summary>Unrelated to <see cref="Resource"/> so that casting one to the other fails.</summary>
    private sealed class OtherResource : CountingResource;

    private sealed class PlainResource;

    private sealed class CallbackResource(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
