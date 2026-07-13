using CapriKit.IO;

namespace CapriKit.AssetPipeline;

internal sealed record Envelope<T>(T Asset, IReadOnlySet<FilePath> Dependencies);
