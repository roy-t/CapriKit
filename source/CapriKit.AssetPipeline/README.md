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

3. Use the asset manager to create a bundle and start building (if needed) and loading assets into it. The bundle owns everything you load into it, so it is the thing to keep and to dispose.
```
using var bundle = assetManager.CreateBundle();
var shaderHandle = bundle.Load<PixelShader>(new AssetId("./shader.hlsl"));
var fontHandle = bundle.Load<Texture2D>(new AssetId("./"robo.png"));
```
4. Use the obtained handles to describe how the contents of the bundle can be constructed once building and loading finishes. This gives you a loader, which only reports progress: dropping it costs nothing, unloading still goes through the bundle.
```
var loader = bundle.Build(r => new UIBundle(r.get(shaderHandle), r.get(fontHandle));
```

5.  Do not forget to update the asset manager each frame so that it can manage the loading tasks and other bookkeeping.
```
assetManager.Update();
```

6. Check each frame if loading finished and obtain your strongly typed contents
```
if (loader.isReady(out var ui))
{
    // Yay!
}
```

7. If you no longer need the assets, or if the program exits, dispose the bundle so the assets can be disposed. Loading and unloading uses reference counting to ensure that an asset is only truly disposed of it nobody references it anymore. Unloading a bundle that is still loading is fine, the assets that are still on their way are returned as soon as they arrive.
```
bundle.Dispose();       // or, the same thing: assetManager.Unload(bundle);
```

To ensure everyone correctly unloads their assets an exception will be thrown if the `AssetManager` (and underlying `AssetPool`) are disposed without first unloading all assets. The assets that were left behind are deliberately not unloaded for you, that would only hide the bug. To help you find it the asset manager logs an error naming every bundle that was never unloaded, and the file and line that created it.

## Hot reloading
Hot reloading happens automatically if the files that were used to build the asset are present and change.

## Implementation
