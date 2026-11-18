using CapriKit.DirectX11.Debug;
using System.Diagnostics;

namespace CapriKit.Tests.Tool.Performance;

/// <summary>
/// Collects GPU and CPU performance data. Memory and pipeline statistics are updated every frame, while
/// timing information uses a rolling window and is summarized every second. 
/// </summary>
internal sealed class PerformanceDataCollector
{
    private const int WindowSize = 512;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);

    private long lastRefresh;
    private int framesSinceLastRefresh;
    private long bytesAllocatedSinceLastRefresh;
    private PipelineStatistics[] pipeline = [];

    public TimingGroup Cpu { get; } = new(WindowSize);

    public TimingGroup Gpu { get; } = new(WindowSize);

    public ReadOnlySpan<PipelineStatistics> Pipeline => pipeline;

    public GpuMemoryStatistics GpuMemory { get; private set; }
    public CpuMemoryStatistics CpuMemory { get; private set; }

    public float BytesAllocatedPerSecond { get; private set; }

    public int Capacity => WindowSize;

    public void Collect(GpuProfiler source)
    {
        Gpu.Add(source.Sections, source.Total, source.Durations);
        GpuMemory = new GpuMemoryStatistics(source.MemoryUsage, source.MemoryBudget);

        if (pipeline.Length == 0)
        {
            pipeline = new PipelineStatistics[source.Sections.Count + 1];
        }

        pipeline[0] = default;
        for (var i = 0; i < source.Statistics.Length; i++)
        {
            pipeline[i + 1] = source.Statistics[i];
            pipeline[0] += source.Statistics[i];
        }

        Refresh();
    }

    public void Collect(CpuProfiler source)
    {
        Cpu.Add(source.Sections, source.Total, source.Durations);
        CpuMemory = source.Memory;
        bytesAllocatedSinceLastRefresh += source.Memory.AllocatedBytesPerFrame;
        framesSinceLastRefresh++;

        Refresh();
    }

    public void Refresh()
    {
        var elapsed = Stopwatch.GetElapsedTime(lastRefresh);
        if (elapsed >= RefreshInterval)
        {
            Cpu.Refresh();
            Gpu.Refresh();
            BytesAllocatedPerSecond = bytesAllocatedSinceLastRefresh / (float)framesSinceLastRefresh;

            bytesAllocatedSinceLastRefresh = 0;
            framesSinceLastRefresh = 0;
            lastRefresh = Stopwatch.GetTimestamp();
        }
    }

    public void Reset()
    {
        Cpu.Reset();
        Gpu.Reset();
        Array.Clear(pipeline);
        GpuMemory = default;
        CpuMemory = default;
        bytesAllocatedSinceLastRefresh = 0;
        BytesAllocatedPerSecond = 0;
        framesSinceLastRefresh = 0;
        lastRefresh = Stopwatch.GetTimestamp();
    }
}
