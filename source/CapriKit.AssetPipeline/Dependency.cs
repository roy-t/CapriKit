using CapriKit.IO;

namespace CapriKit.AssetPipeline;

public sealed record Dependency(FilePath File, DateTime Version);
