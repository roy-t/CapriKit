using CapriKit.IO;

namespace CapriKit.AssetPipeline;

public sealed record Asset<T>(T Value, IReadOnlyList<Dependency> Dependencies);
