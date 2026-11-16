using CapriKit.DirectX11.Debug;
using System.Diagnostics;

namespace CapriKit.Tests.Tool.Performance;

/// <summary>
/// Keeps CPU and GPU timings and memory use of the last frames and periodically summarizes them, so that the numbers are readable.
/// </summary>
internal sealed class PerformanceDataCollector
{
    private const int WindowSize = 512;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(0.5);

    private PipelineStatistics[] latestPipeline = [];
    private PipelineStatistics[] pipeline = [];
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
    /// Memory use of the process in the most recent frame at the last refresh.
    /// </summary>
    public CpuMemoryStatistics CpuMemory { get; private set; }

    /// <summary>
    /// Bytes allocated on the managed heap per frame, averaged over the frames between the last two refreshes.
    /// </summary>
    public long AverageAllocatedBytesPerFrame { get; private set; }

    public int Capacity => WindowSize;

    public void Collect(CpuProfiler source)
    {
        Cpu.Add(source.Sections, source.Total, source.Durations);

        allocatedSinceRefresh += source.Memory.AllocatedBytesPerFrame;
        framesSinceRefresh++;
        latestCpuMemory = source.Memory;

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
            CpuMemory = latestCpuMemory;
            // A refresh can be triggered by the GPU profiler before any CPU frame was collected
            AverageAllocatedBytesPerFrame = framesSinceRefresh > 0 ? allocatedSinceRefresh / framesSinceRefresh : 0;
            allocatedSinceRefresh = 0;
            framesSinceRefresh = 0;
            lastRefresh = Stopwatch.GetTimestamp();
        }
    }
}
