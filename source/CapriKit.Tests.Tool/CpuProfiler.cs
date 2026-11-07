using System.Diagnostics;

namespace CapriKit.Tests.Tool;

/// <summary>
/// Measures how long the CPU spends on consecutive sections of a frame.
/// Call <see cref="BeginFrame"/> at the start of a frame, <see cref="EndSection"/> after the work
/// of each section, in the order the sections were given to the constructor, and <see cref="EndFrame"/>
/// at the end of the frame. Results are available immediately after <see cref="EndFrame"/>.
/// </summary>
internal sealed class CpuProfiler
{
    private readonly long[] Timestamps; // 0 = frame start, i + 1 = end of section i
    private readonly float[] SectionDurations;

    private int nextSection;

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
    }
}
