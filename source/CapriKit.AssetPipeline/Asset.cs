using CapriKit.IO;

namespace CapriKit.AssetPipeline;

public sealed record Asset<T>(AssetId Id, T Value, IReadOnlyList<Dependency> Dependencies);
