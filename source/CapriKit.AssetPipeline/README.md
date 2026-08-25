# CapriKit.AssetPipeline

Reusable asset pipeline that provides multi-threaded building, loading and hot-reloading of assets.

## Usage

1. Create the asset manager and register a transcoder, an implementation of `IAssetTranscoder` that knows how to build and load one asset type:
```
var assetManager = new AssetManager(LoggerFactory, ScopedFileSystem);
assetManager.RegisterTranscoder(new VertexShaderTranscoder(GraphicsDevice));
```

2. Define a bundle that reflects the strongly typed assets that you want to load.
```
record UIBundle(PixelShader Shader, Texture2D Font);
```

3. Use the asset manager to create a bundle builder and start building (if needed) and loading assets.
```
var builder = assetManager.CreateBundle();
var shaderHandle = builder.Load<PixelShader>(new AssetId("./shader.hlsl"));
var fontHandle = builder.Load<Texture2D>(new AssetId("./"robo.png"));
```
4. Use the obtained handles to describe how the bundle can be construct once building and loading finishes.
```
var loader = builder.Build(r => new UIBundle(r.get(shaderHandle), r.get(fontHandle));
```

5.  Do not forget to update the asset manager each frame so that it can manage the loading tasks and other bookkeeping.
```
assetManager.Update();
```

6. Check each frame if loading finished and obtain your strongly typed bundle
```
if (loader.isReady(out var bundle))
{
    // Yay!
}
```

7. If you no longer need the assets, or if the program exists, return the loader so the assets can be disposed. Loading and unloading uses reference counting to ensure that an asset is only truly disposed of it nobody references it anymore. To ensure everyone correctly unloads their assets an exception will be thrown if the `AssetManager` (and underlying `AssetPool`) are disposed without first unloading all assets.
```
assetManager.Unload(loader);
```

## Hot reloading
Hot reloading happens automatically if the files that were used to build the asset are present and change.

## Implementation
