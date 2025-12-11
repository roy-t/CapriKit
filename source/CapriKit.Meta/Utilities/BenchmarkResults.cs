namespace CapriKit.Meta.Utilities;

public record BenchmarkResults(string Title, Hostenvironmentinfo HostEnvironmentInfo, Benchmark[] Benchmarks);

public record Hostenvironmentinfo(string BenchmarkDotNetCaption, string BenchmarkDotNetVersion, string OsVersion, string ProcessorName, int PhysicalProcessorCount, int PhysicalCoreCount, int LogicalCoreCount, string RuntimeVersion, string Architecture, bool HasAttachedDebugger, bool HasRyuJit, string Configuration, string DotNetCliVersion, Chronometerfrequency ChronometerFrequency, string HardwareTimerKind);

public record Chronometerfrequency(int Hertz);

public record Benchmark(string DisplayInfo, string Namespace, string Type, string Method, string MethodTitle, string Parameters, string FullName, string HardwareIntrinsics, Statistics Statistics, Measurement[] Measurements);

public record Statistics(float[] OriginalValues, int N, float Min, float LowerFence, float Q1, float Median, float Mean, float Q3, float UpperFence, float Max, float InterquartileRange, object[] LowerOutliers, float?[] UpperOutliers, float?[] AllOutliers, float StandardError, float Variance, float StandardDeviation, float Skewness, float Kurtosis, Confidenceinterval ConfidenceInterval, Percentiles Percentiles);

public record Confidenceinterval(int N, float Mean, float StandardError, int Level, float Margin, float Lower, float Upper);

public record Percentiles(float P0, float P25, float P50, float P67, float P80, float P85, float P90, float P95, float P100);

public record Measurement(string IterationMode, string IterationStage, int LaunchIndex, int IterationIndex, int Operations, int Nanoseconds);
