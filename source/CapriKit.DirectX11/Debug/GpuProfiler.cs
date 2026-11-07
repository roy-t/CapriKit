using CapriKit.DirectX11.Contexts;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Debug;

/// <summary>
/// Measures how long the GPU spends on consecutive sections of a frame, and how much work it does in them,
/// using timestamp and pipeline statistics queries.
/// Call <see cref="BeginFrame"/> at the start of a frame, <see cref="EndSection"/> after the commands
/// of each section, in the order the sections were given to the constructor, and <see cref="EndFrame"/>
/// before presenting. Results arrive a few frames late so that reading them never stalls the CPU.
/// Frames are not measured while the GPU is too far behind to reuse a set of queries.
/// Timings are approximate: the GPU overlaps work, so commands near a section boundary can be partially
/// attributed to the neighbouring section.
/// </summary>
public sealed class GpuProfiler : IDisposable
{
    // Number of frames that can be in flight, the GPU is usually at most 3 frames behind the CPU
    private const int Latency = 4;

    private readonly ID3D11Query[] Disjoint;
    private readonly ID3D11Query[,] Timestamps; // [slot, 0 = frame start, i + 1 = end of section i]
    private readonly ID3D11Query[,] PipelineQueries; // [slot, section]
    private readonly bool[] Pending; // Queries of a slot must be read before they can be reused
    private readonly float[] SectionDurations;
    private readonly PipelineStatistics[] SectionStatistics;

    private int writeSlot;
    private int readSlot;
    private bool isMeasuring;
    private int nextSection;

    public GpuProfiler(Device device, params string[] sections)
    {
        ArgumentOutOfRangeException.ThrowIfZero(sections.Length);

        Sections = sections;
        SectionDurations = new float[sections.Length];
        SectionStatistics = new PipelineStatistics[sections.Length];
        Pending = new bool[Latency];
        Disjoint = new ID3D11Query[Latency];
        Timestamps = new ID3D11Query[Latency, sections.Length + 1];
        PipelineQueries = new ID3D11Query[Latency, sections.Length];

        for (var slot = 0; slot < Latency; slot++)
        {
            Disjoint[slot] = device.ID3D11Device.CreateQuery(new QueryDescription(QueryType.TimestampDisjoint, QueryFlags.None));
            for (var i = 0; i < sections.Length + 1; i++)
            {
                Timestamps[slot, i] = device.ID3D11Device.CreateQuery(new QueryDescription(QueryType.Timestamp, QueryFlags.None));
            }

            for (var i = 0; i < sections.Length; i++)
            {
                PipelineQueries[slot, i] = device.ID3D11Device.CreateQuery(new QueryDescription(QueryType.PipelineStatistics, QueryFlags.None));
            }
        }
    }

    /// <summary>
    /// Names of the measured sections, in the same order as <see cref="Durations"/>.
    /// </summary>
    public IReadOnlyList<string> Sections { get; }

    /// <summary>
    /// GPU time in milliseconds of each section, from the most recent frame for which results were available.
    /// Only changes when <see cref="EndFrame"/> returns true.
    /// </summary>
    public ReadOnlySpan<float> Durations => SectionDurations;

    /// <summary>
    /// GPU time in milliseconds from the start of the frame to the end of the last section.
    /// </summary>
    public float Total { get; private set; }

    /// <summary>
    /// Work done by the GPU in each section, from the same frame as <see cref="Durations"/>.
    /// </summary>
    public ReadOnlySpan<PipelineStatistics> Statistics => SectionStatistics;

    public void BeginFrame(ImmediateDeviceContext context)
    {
        nextSection = 0;

        // Issuing a query whose results have not been read yet is reported as a warning by the debug layer
        isMeasuring = !Pending[writeSlot];
        if (isMeasuring)
        {
            context.ID3D11DeviceContext.Begin(Disjoint[writeSlot]);
            context.ID3D11DeviceContext.End(Timestamps[writeSlot, 0]);
            context.ID3D11DeviceContext.Begin(PipelineQueries[writeSlot, 0]);
        }
    }

    /// <summary>
    /// Marks the end of the given section, which is also the start of the next section.
    /// Skipped sections are recorded with a duration of zero.
    /// </summary>
    public void EndSection(ImmediateDeviceContext context, int section)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(section, nextSection);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(section, Sections.Count);

        for (; nextSection <= section; nextSection++)
        {
            if (isMeasuring)
            {
                context.ID3D11DeviceContext.End(Timestamps[writeSlot, nextSection + 1]);
                context.ID3D11DeviceContext.End(PipelineQueries[writeSlot, nextSection]);
                if (nextSection + 1 < Sections.Count)
                {
                    context.ID3D11DeviceContext.Begin(PipelineQueries[writeSlot, nextSection + 1]);
                }
            }
        }
    }

    /// <summary>
    /// Ends the frame and reads back the results of all frames the GPU has finished.
    /// Returns true if new results are available in <see cref="Durations"/> and <see cref="Total"/>.
    /// </summary>
    public bool EndFrame(ImmediateDeviceContext context)
    {
        if (nextSection < Sections.Count)
        {
            EndSection(context, Sections.Count - 1);
        }

        if (isMeasuring)
        {
            context.ID3D11DeviceContext.End(Disjoint[writeSlot]);
            Pending[writeSlot] = true;
            writeSlot = (writeSlot + 1) % Latency;
        }

        var hasResults = false;
        while (Pending[readSlot] && TryReadResults(context.ID3D11DeviceContext, readSlot, out var isValid))
        {
            hasResults |= isValid;
            Pending[readSlot] = false;
            readSlot = (readSlot + 1) % Latency;
        }

        return hasResults;
    }

    // Returns false if the GPU has not finished the slot yet, isValid is false if the results are unreliable
    private bool TryReadResults(ID3D11DeviceContext context, int slot, out bool isValid)
    {
        isValid = false;

        // DoNotFlush: if the GPU is not done yet try again next frame instead of waiting for it
        if (!context.GetData(Disjoint[slot], AsyncGetDataFlags.DoNotFlush, out QueryDataTimestampDisjoint disjoint))
        {
            return false;
        }

        // Read every query, even if the results are disjoint, so that the queries can be reused
        Span<ulong> stamps = stackalloc ulong[Sections.Count + 1];
        for (var i = 0; i < stamps.Length; i++)
        {
            if (!context.GetData(Timestamps[slot, i], AsyncGetDataFlags.DoNotFlush, out stamps[i]))
            {
                return false;
            }
        }

        Span<QueryDataPipelineStatistics> statistics = stackalloc QueryDataPipelineStatistics[Sections.Count];
        for (var i = 0; i < statistics.Length; i++)
        {
            if (!context.GetData(PipelineQueries[slot, i], AsyncGetDataFlags.DoNotFlush, out statistics[i]))
            {
                return false;
            }
        }

        // Disjoint means the GPU clock frequency changed during the frame, for example due to power saving
        if (disjoint.Disjoint)
        {
            return true;
        }

        var toMilliseconds = 1000.0 / disjoint.Frequency;
        for (var i = 0; i < SectionDurations.Length; i++)
        {
            SectionDurations[i] = (float)((stamps[i + 1] - stamps[i]) * toMilliseconds);
            SectionStatistics[i] = PipelineStatistics.From(statistics[i]);
        }
        Total = (float)((stamps[^1] - stamps[0]) * toMilliseconds);

        isValid = true;
        return true;
    }

    public void Dispose()
    {
        foreach (var query in Disjoint)
        {
            query.Dispose();
        }

        foreach (var query in Timestamps)
        {
            query.Dispose();
        }

        foreach (var query in PipelineQueries)
        {
            query.Dispose();
        }
    }
}
