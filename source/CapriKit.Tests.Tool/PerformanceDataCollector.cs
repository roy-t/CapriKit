using CapriKit.DirectX11.Debug;
using System.Diagnostics;

namespace CapriKit.Tests.Tool;

/// <param name="Median">Median duration</param>
/// <param name="P99">Duration exceeded by 1% of the samples</param>
/// <param name="Noise">Interquartile range (p75 - p25)</param>
internal readonly record struct TimingStatistics(float Median, float P99, float Noise);

/// <param name="PrivateMemorySize">Bytes committed by the process, managed and native</param>
/// <param name="AllocatedBytes">Bytes allocated on the managed heap since the process started</param>
/// <param name="AllocatedBytesPerFrame">Average bytes allocated on the managed heap per frame since the previous refresh</param>
/// <param name="TotalPauseDuration">Milliseconds paused for garbage collections since the process started</param>
/// <param name="LastPauseDuration">Milliseconds paused for the most recent garbage collection</param>
internal readonly record struct CpuMemoryStatistics(long PrivateMemorySize, long AllocatedBytes, long AllocatedBytesPerFrame, float TotalPauseDuration, float LastPauseDuration);

/// <param name="MemoryUsage">Total local GPU memory used by this process, in bytes</param>
/// <param name="MemoryBudget">OS allowed local GPU memory budget, in bytes, before swapping occurs</param>
internal readonly record struct GpuMemoryStatistics(ulong MemoryUsage, ulong MemoryBudget);

/// <summary>
/// Keeps CPU and GPU timings and memory use of the last frames and periodically summarizes them, so that the numbers are readable.
/// </summary>
internal sealed class PerformanceDataCollector
{
    private const int WindowSize = 512;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(0.5);

    private PipelineStatistics[] latestPipeline = [];
    private PipelineStatistics[] pipeline = [];
    private readonly long[] latestGenerations = new long[5];
    private readonly long[] generations = new long[5];
    private CpuMemoryStatistics latestCpuMemory;
    private long allocatedSinceRefresh;
    private int framesSinceRefresh;
    private long lastRefresh;

    public TimingGroup Cpu { get; } = new(WindowSize);

    public TimingGroup Gpu { get; } = new(WindowSize);

    /// <summary>
    /// Work done by the GPU in the most recent frame at the last refresh, in the same order as <see cref="TimingGroup.Names"/> of <see cref="Gpu"/>.
    /// Not averaged since these counts do not fluctuate in a static scene.
    /// </summary>
    public ReadOnlySpan<PipelineStatistics> Pipeline => pipeline;


    /// <summary>
    /// GPU memory usage.
    /// </summary>
    public GpuMemoryStatistics GpuMemory { get; private set; }

    /// <summary>
    /// Memory use of the process at the last refresh.
    /// </summary>
    public CpuMemoryStatistics CpuMemory { get; private set; }

    /// <summary>
    /// Size in bytes of gen0, gen1, gen2, the large object heap and the pinned object heap at the last refresh.
    /// </summary>
    public ReadOnlySpan<long> GenerationSizes => generations;

    public int Capacity => WindowSize;

    public void Collect(CpuProfiler source)
    {
        Cpu.Add(source.Sections, source.Total, source.Durations);

        allocatedSinceRefresh += source.AllocatedBytesDelta;
        framesSinceRefresh++;
        source.GenerationSizes.CopyTo(latestGenerations);
        latestCpuMemory = new CpuMemoryStatistics(
            source.PrivateMemorySize,
            source.AllocatedBytes,
            allocatedSinceRefresh / framesSinceRefresh,
            source.TotalPauseDuration,
            source.LastPauseDuration);

        RefreshIfDue();
    }

    public void Collect(GpuProfiler source)
    {
        Gpu.Add(source.Sections, source.Total, source.Durations);

        if (latestPipeline.Length == 0)
        {
            latestPipeline = new PipelineStatistics[source.Sections.Count + 1];
            pipeline = new PipelineStatistics[source.Sections.Count + 1];
        }

        latestPipeline[0] = default;
        for (var i = 0; i < source.Statistics.Length; i++)
        {
            latestPipeline[i + 1] = source.Statistics[i];
            latestPipeline[0] += source.Statistics[i];
        }

        GpuMemory = new GpuMemoryStatistics(source.MemoryUsage, source.MemoryBudget);

        RefreshIfDue();
    }

    /// <summary>
    /// Discards all samples, use this after changing what is rendered so old samples do not skew the statistics.
    /// </summary>
    public void Reset()
    {
        Cpu.Reset();
        Gpu.Reset();
        Array.Clear(latestPipeline);
        Array.Clear(pipeline);
    }

    private void RefreshIfDue()
    {
        if (Stopwatch.GetElapsedTime(lastRefresh) >= RefreshInterval)
        {
            Cpu.Refresh();
            Gpu.Refresh();
            latestPipeline.CopyTo(pipeline, 0);
            latestGenerations.CopyTo(generations, 0);
            CpuMemory = latestCpuMemory;
            allocatedSinceRefresh = 0;
            framesSinceRefresh = 0;
            lastRefresh = Stopwatch.GetTimestamp();
        }
    }
}

/// <summary>
/// Rolling statistics of the total and the sections of a profiled frame.
/// </summary>
internal sealed class TimingGroup(int capacity)
{
    private string[] names = [];
    private RollingWindow[] windows = [];
    private TimingStatistics[] statistics = [];

    /// <summary>
    /// Names of the measurements, the first one is the total of the frame followed by the profiled sections.
    /// </summary>
    public IReadOnlyList<string> Names => names;

    /// <summary>
    /// Statistics in milliseconds as of the last refresh, in the same order as <see cref="Names"/>.
    /// </summary>
    public ReadOnlySpan<TimingStatistics> Statistics => statistics;

    public int SampleCount => windows.Length > 0 ? windows[0].Count : 0;

    public void Add(IReadOnlyList<string> sections, float total, ReadOnlySpan<float> durations)
    {
        if (windows.Length == 0)
        {
            names = ["Total", .. sections];
            windows = [.. names.Select(_ => new RollingWindow(capacity))];
            statistics = new TimingStatistics[names.Length];
        }

        windows[0].Add(total);
        for (var i = 0; i < durations.Length; i++)
        {
            windows[i + 1].Add(durations[i]);
        }
    }

    public void Refresh()
    {
        for (var i = 0; i < windows.Length; i++)
        {
            statistics[i] = windows[i].GetStatistics();
        }
    }

    public void Reset()
    {
        foreach (var window in windows)
        {
            window.Clear();
        }
        Array.Clear(statistics);
    }
}

internal sealed class RollingWindow(int capacity)
{
    private readonly float[] Samples = new float[capacity];
    private int next;

    public int Count { get; private set; }

    public void Add(float sample)
    {
        Samples[next] = sample;
        next = (next + 1) % capacity;
        Count = Math.Min(Count + 1, capacity);
    }

    public void Clear()
    {
        next = 0;
        Count = 0;
    }

    public TimingStatistics GetStatistics()
    {
        if (Count == 0) { return default; }

        Span<float> sorted = stackalloc float[Count];
        Samples.AsSpan(0, Count).CopyTo(sorted);
        sorted.Sort();

        return new TimingStatistics(
            CapriKit.Mathematics.Statistics.Percentile(sorted, 0.50f),
            CapriKit.Mathematics.Statistics.Percentile(sorted, 0.99f),
            CapriKit.Mathematics.Statistics.Percentile(sorted, 0.75f) - CapriKit.Mathematics.Statistics.Percentile(sorted, 0.25f));
    }
}
