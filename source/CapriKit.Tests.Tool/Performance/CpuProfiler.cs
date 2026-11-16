using System.Diagnostics;

namespace CapriKit.Tests.Tool.Performance;

/// <summary>
/// Measures how long the CPU spends on consecutive sections of a frame.
/// Call <see cref="BeginFrame"/> at the start of a frame, <see cref="EndSection"/> after the work
/// of each section, in the order the sections were given to the constructor, and <see cref="EndFrame"/>
/// at the end of the frame. Results are available immediately after <see cref="EndFrame"/>.
/// Also tracks the memory use of the process.
/// </summary>
internal sealed class CpuProfiler
{
    private readonly long[] Timestamps; // 0 = frame start, i + 1 = end of section i
    private readonly float[] SectionDurations;

    private int nextSection;
    private int lastCollectionCount = -1; // -1 so the first frame always reads the memory info

    public CpuProfiler(params string[] sections)
    {
        ArgumentOutOfRangeException.ThrowIfZero(sections.Length);

        Sections = sections;
        Timestamps = new long[sections.Length + 1];
        SectionDurations = new float[sections.Length];
    }

    /// <summary>
    /// Names of the measured sections, in the same order as <see cref="Durations"/>.
    /// </summary>
    public IReadOnlyList<string> Sections { get; }

    /// <summary>
    /// CPU time in milliseconds of each section of the last completed frame.
    /// </summary>
    public ReadOnlySpan<float> Durations => SectionDurations;

    /// <summary>
    /// CPU time in milliseconds from the start of the frame to the end of the last section.
    /// </summary>
    public float Total { get; private set; }

    /// <summary>
    /// Memory use of the process, covering all threads, as of the last completed frame.
    /// Private memory, generation sizes and last pause are only updated after a garbage collection.
    /// </summary>
    public CpuMemoryStatistics Memory { get; private set; }

    public void BeginFrame()
    {
        nextSection = 0;
        Timestamps[0] = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Marks the end of the given section, which is also the start of the next section.
    /// Skipped sections are recorded with a duration of zero.
    /// </summary>
    public void EndSection(int section)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(section, nextSection);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(section, Sections.Count);

        var now = Stopwatch.GetTimestamp();
        for (; nextSection <= section; nextSection++)
        {
            Timestamps[nextSection + 1] = now;
        }
    }

    public void EndFrame()
    {
        if (nextSection < Sections.Count)
        {
            EndSection(Sections.Count - 1);
        }

        // Durations are only updated here so they always describe one complete frame
        for (var i = 0; i < SectionDurations.Length; i++)
        {
            SectionDurations[i] = (float)Stopwatch.GetElapsedTime(Timestamps[i], Timestamps[i + 1]).TotalMilliseconds;
        }
        Total = (float)Stopwatch.GetElapsedTime(Timestamps[0], Timestamps[^1]).TotalMilliseconds;

        UpdateMemory();
    }

    private void UpdateMemory()
    {
        // Not precise: can be off by a few KB per thread, but much cheaper
        var allocated = GC.GetTotalAllocatedBytes(precise: false);
        Memory = Memory with
        {
            AllocatedBytes = allocated,
            AllocatedBytesPerFrame = allocated - Memory.AllocatedBytes,
            TotalPauseDuration = (float)GC.GetTotalPauseDuration().TotalMilliseconds
        };

        // Every garbage collection increments the gen0 count, even when collecting a higher generation
        var collectionCount = GC.CollectionCount(0);
        if (collectionCount == lastCollectionCount)
        {
            return;
        }
        lastCollectionCount = collectionCount;

        // These calls allocate, and their results only change after a garbage collection
        var info = GC.GetGCMemoryInfo();
        var generations = info.GenerationInfo;

        // A background collection pauses twice, the second pause is zero for other collections
        var pause = TimeSpan.Zero;
        foreach (var duration in info.PauseDurations)
        {
            pause += duration;
        }

        using var process = Process.GetCurrentProcess();
        Memory = Memory with
        {
            Collections = collectionCount,
            PrivateMemorySize = process.PrivateMemorySize64,
            LastPauseDuration = (float)pause.TotalMilliseconds,
            Gen0Size = generations[0].SizeAfterBytes,
            Gen1Size = generations[1].SizeAfterBytes,
            Gen2Size = generations[2].SizeAfterBytes,
            LargeObjectHeapSize = generations[3].SizeAfterBytes,
            PinnedObjectHeapSize = generations[4].SizeAfterBytes
        };
    }
}
