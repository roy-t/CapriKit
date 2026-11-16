namespace CapriKit.Tests.Tool.Performance;

/// <param name="Median">Median duration</param>
/// <param name="P99">Duration exceeded by 1% of the samples</param>
/// <param name="Noise">Interquartile range (p75 - p25)</param>
internal readonly record struct TimingStatistics(float Median, float P99, float Noise);
