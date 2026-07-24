namespace CapriKit.AssetPipeline;

/// <summary>
/// An active asset
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="Id">The unique id, refers to a virtual file location (id.Path) and if required a sub-resource in that file (id.Key).</param>
/// <param name="Value">The asset</param>
/// <param name="Dependencies">Files that this asset depends on, if any of these files changed the asset needs te be rebuild.</param>
public sealed record Asset<T>(AssetId Id, T Value, IReadOnlyList<Dependency> Dependencies)
    where T : class;
