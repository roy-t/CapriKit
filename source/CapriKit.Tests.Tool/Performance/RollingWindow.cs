namespace CapriKit.Tests.Tool.Performance;

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
