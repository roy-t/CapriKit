using CapriKit.IO;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Unique asset identifier
/// </summary>
/// <param name="Key">Optional key to a sub-resources in Path.</param>
/// <param name="Path">Virtual file path that points to the file the asset originates from.</param>
public record AssetId(string Key, FilePath Path);
