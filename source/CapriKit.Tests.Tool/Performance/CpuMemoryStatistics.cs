namespace CapriKit.Tests.Tool.Performance;

/// <param name="PrivateMemorySize">Bytes committed by the process, managed and native</param>
/// <param name="MainThreadAllocatedBytes">Exact bytes allocated on the main thread</param>
/// <param name="AllocatedBytes">Approximate bytes allocated on the managed heap since the process started</param>
/// <param name="AllocatedBytesPerFrame">Bytes allocated on the managed heap during the frame</param>
/// <param name="Collections">Number of GC collections since the application started.</param>
/// <param name="TotalPauseDuration">Milliseconds paused for garbage collections since the process started</param>
/// <param name="LastPauseDuration">Milliseconds paused for the most recent garbage collection</param>
/// <param name="Gen0Size">GC Gen 0 size in bytes</param>
/// <param name="Gen1Size">GC Gen 1 size in bytes</param>
/// <param name="Gen2Size">GC Gen 2 size in bytes</param>
/// <param name="LargeObjectHeapSize">GC Large Object Heap size in bytes</param>
/// <param name="PinnedObjectHeapSize">GC Pinned Object Heap size in bytes</param>
internal readonly record struct CpuMemoryStatistics(long PrivateMemorySize, long MainThreadAllocatedBytes, long AllocatedBytes, long AllocatedBytesPerFrame, int Collections, float TotalPauseDuration, float LastPauseDuration,
    long Gen0Size, long Gen1Size, long Gen2Size, long LargeObjectHeapSize, long PinnedObjectHeapSize);
