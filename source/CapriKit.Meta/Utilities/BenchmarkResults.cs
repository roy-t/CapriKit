namespace CapriKit.Meta.Utilities;

public record BenchmarkResults(string Title, Hostenvironmentinfo HostEnvironmentInfo, Benchmark[] Benchmarks);

public record Hostenvironmentinfo(string BenchmarkDotNetCaption, string BenchmarkDotNetVersion, string OsVersion, string ProcessorName, int PhysicalProcessorCount, int PhysicalCoreCount, int LogicalCoreCount, string RuntimeVersion, string Architecture, bool HasAttachedDebugger, bool HasRyuJit, string Configuration, string DotNetCliVersion, Chronometerfrequency ChronometerFrequency, string HardwareTimerKind);

public record Chronometerfrequency(int Hertz);

public record Benchmark(string DisplayInfo, string Namespace, string Type, string Method, string MethodTitle, string Parameters, string FullName, string HardwareIntrinsics, Statistics Statistics, Measurement[] Measurements);

public record Statistics(double[] OriginalValues, int N, double Min, double LowerFence, double Q1, double Median, double Mean, double Q3, double UpperFence, double Max, double InterquartileRange, object[] LowerOutliers, double?[] UpperOutliers, double?[] AllOutliers, double StandardError, double Variance, double StandardDeviation, double Skewness, double Kurtosis, Confidenceinterval ConfidenceInterval, Percentiles Percentiles);

public record Confidenceinterval(int N, double Mean, double StandardError, int Level, double Margin, double Lower, double Upper);

public record Percentiles(double P0, double P25, double P50, double P67, double P80, double P85, double P90, double P95, double P100);

public record Measurement(string IterationMode, string IterationStage, int LaunchIndex, int IterationIndex, int Operations, int Nanoseconds);
