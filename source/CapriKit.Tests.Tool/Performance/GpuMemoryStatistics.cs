namespace CapriKit.Tests.Tool.Performance;

/// <param name="MemoryUsage">Total local GPU memory used by this process, in bytes</param>
/// <param name="MemoryBudget">OS allowed local GPU memory budget, in bytes, before swapping occurs</param>
internal readonly record struct GpuMemoryStatistics(ulong MemoryUsage, ulong MemoryBudget);
