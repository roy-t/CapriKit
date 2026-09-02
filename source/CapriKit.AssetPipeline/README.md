# CapriKit.AssetPipeline

Reusable asset pipeline that provides multi-threaded building, loading and hot-reloading of assets.

## Usage

1. Create the asset manager and register a transcoder, an implementation of `IAssetTranscoder` that knows how to build and load one asset type:
```
var assetManager = new AssetManager(LoggerFactory, ScopedFileSystem);
assetManager.RegisterTranscoder(new VertexShaderTranscoder(GraphicsDevice));
```

2. Define the contents of a bundle: a plain type that holds the strongly typed assets that you want to load.
```
record UIBundle(PixelShader Shader, Texture2D Font);
```

3. Ask a builder for the assets you need, it will return a handle that you can use to materialize that asset in the future.
```
var builder = new AssetBundleBuilder<UIBundle>(assetManager);
var shaderHandle = builder.Request<PixelShader>(new AssetId("./shader.hlsl"));
var fontHandle = builder.Request<Texture2D>(new AssetId("./robo.png"));
```

4. Use the obtained handles to describe how the contents can be constructed, and build the bundle.
```
using var bundle = builder.Build(r => new UIBundle(r.Get(shaderHandle), r.Get(fontHandle)));
```

5.  Update the asset manager each frame so that it can manage the loading tasks and other bookkeeping.
```
assetManager.Update();
```

6. Periodically check if loading has finished and obtain your strongly typed contents. You can use `bundle.Total`, `bundle.Loaded` and `bundle.LastCompletedItem` for progress tracking. Any failures to load or build an item will also surface by calling `bundle.IsReady`.
```
if (bundle.IsReady(out var ui))
{
    // Yay!
}
```

7. Under the hood, bundles lease loaded assets from the `AssetPool` which uses reference tracking to decide when to unload an asset. If you no longer need the assets, or when the program exits, you need to dispose your bundle. You can dispose your bundle even if loading hasn't completed yet. The asset system will ensure that any late arrivals are disposed of properly.
```
bundle.Dispose();
```

To ensure everyone correctly unloads their assets an exception will be thrown if the `AssetManager` (and underlying `AssetPool`) are disposed while some assets are still referenced by an asset bundle.

## Hot reloading
Hot reloading triggers if a file that was used to build a live asset changes. The change triggers a rebuild and reload mechanism that executes in the background. Once the new asset is ready the next call to `assetManager.Update()` triggers the actual swap by calling `HotSwap` on the asset type's assigned `IAssetTranscoder`.

## Implementation

TODO
