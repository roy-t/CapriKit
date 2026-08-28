# CapriKit.AssetPipeline

Reusable asset pipeline that provides multi-threaded building, loading and hot-reloading of assets.

## Usage

1. Create the asset manager and register a transcoder, an implementation of `IAssetTranscoder` that knows how to build and load one asset type:
```
var assetManager = new AssetManager(LoggerFactory, ScopedFileSystem);
assetManager.RegisterTranscoder(new VertexShaderTranscoder(GraphicsDevice));
```

2. Define the contents of a bundle: a plain type that reflects the strongly typed assets that you want to load. It needs no base class or interface, the `AssetBundle` that the pipeline hands you owns the lifetime of the assets in it.
```
record UIBundle(PixelShader Shader, Texture2D Font);
```

3. Ask a builder for the assets you need. Requesting only records what the bundle will need, nothing is built or loaded yet, so a builder you abandon costs nothing.
```
var builder = new AssetBundleBuilder<UIBundle>(assetManager);
var shaderHandle = builder.Request<PixelShader>(new AssetId("./shader.hlsl"));
var fontHandle = builder.Request<Texture2D>(new AssetId("./robo.png"));
```

4. Use the obtained handles to describe how the contents can be constructed, and build the bundle. This is what actually starts building (if needed) and loading. The bundle owns everything in it, so it is the thing to keep and to dispose. A builder builds once; ask for everything before you call it.
```
using var bundle = builder.Build(r => new UIBundle(r.Get(shaderHandle), r.Get(fontHandle)));
```

5.  Do not forget to update the asset manager each frame so that it can manage the loading tasks and other bookkeeping.
```
assetManager.Update();
```

6. Check each frame if loading finished and obtain your strongly typed contents. `bundle.Total` and `bundle.Loaded` drive a progress bar, and `bundle.LastCompletedItem` puts a name on a loading screen.
```
if (bundle.IsReady(out var ui))
{
    // Yay!
}
```

7. If you no longer need the assets, or if the program exits, dispose the bundle so the assets can be disposed. Loading and unloading uses reference counting to ensure that an asset is only truly disposed of it nobody references it anymore. Unloading a bundle that is still loading is fine, the assets that are still on their way are returned as soon as they arrive.
```
bundle.Dispose();
```

A bundle holds exactly one lease per asset, so asking the same builder for the same asset twice throws rather than quietly taking a second lease that nobody gives back. Two different bundles asking for the same asset is fine, that is what the reference counting is for.

To ensure everyone correctly unloads their assets an exception will be thrown if the `AssetManager` (and underlying `AssetPool`) are disposed without first unloading all assets. The assets that were left behind are deliberately not unloaded for you, that would only hide the bug. To help you find it the asset manager logs an error naming every bundle that was never unloaded, and the file and line that created its builder.

## Hot reloading
Hot reloading happens automatically if the files that were used to build the asset are present and change.

## Implementation

Bookkeeping lives in the bundle, not in the handle. An `AssetHandle<T>` is a value: an asset id plus the
identity of the builder that made it, with no state and no lifetime of its own. The manager delivers a
finished asset by pushing it into whoever asked for it (`IAssetRequester.Accept`), so a bundle knows it is
ready when it has received as many assets as it requested rather than by polling its handles.

Because the builder issues the loads only when `Build` runs, every request lands on a bundle that already
exists. That is what lets an asset which is already in the cache be delivered during `Build` itself, and it
means the set of assets a bundle waits for is complete before the first result can arrive.

A bundle that is disposed while assets are still on their way refuses them, and the manager hands those
leases back in `Update` instead. Refusing is the single decision that decides who returns the lease, so an
asset can never be returned twice or forgotten.
