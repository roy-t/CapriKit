namespace CapriKit.AssetPipeline;

/// <summary>
/// A typed reference to an asset that was requested from an <see cref="AssetBundleBuilder{TContents}"/>,
/// used to read that asset back once its bundle is ready.
/// </summary>
/// <param name="Id">The asset this handle points at.</param>
/// <param name="Owner">
/// Identifies the builder that created this handle, so that resolving it against a bundle it does not
/// belong to is caught.
/// </param>
public readonly record struct AssetHandle<TValue>(AssetId Id, Guid Owner)
    where TValue : class;
