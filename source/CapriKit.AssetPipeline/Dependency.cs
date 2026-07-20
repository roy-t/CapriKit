using CapriKit.IO;

namespace CapriKit.AssetPipeline;

internal sealed record Dependency(FilePath File, DateTime LastWrite);
