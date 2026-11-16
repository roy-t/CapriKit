namespace CapriKit.Tests.Tool.Performance;

/// <summary>
/// Rolling statistics of the total time spend and time spend per section.
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
