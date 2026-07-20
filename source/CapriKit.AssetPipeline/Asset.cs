using CapriKit.IO;

namespace CapriKit.AssetPipeline;

internal sealed record Asset<T>(T Value, IReadOnlyList<Dependency> Dependencies);
