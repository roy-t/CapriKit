using CapriKit.IO;

namespace CapriKit.AssetPipeline;

internal sealed record Asset<T>(T Value, IReadOnlySet<FilePath> Dependencies);
