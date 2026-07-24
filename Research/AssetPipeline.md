# Asset Pipeline
Now that I have support for encoding textures offline and then loading/transcoding them when the game runs (see CapriKit.SuperCompressed) it is time to start work on a real asset pipeline. I want you to help me create a high-level architecture for it. The expected output (when we are ready for that) is a markdown file in the `Research` folder.

# Goals
1. Encoding/compile/compress assets into formats that makes them easy to ship and easy load
2. Load the assets when requested by the game into ready-to-use objects (for example, when done a texture is uploaded to the GPU and ready to be referenced)

## Subgoals
1. Flexible enough to support textures, shaders, models and audio in a similar process
2. Support for detecting changes on-disk and then rebuilding and hot-reloading them
3. Loading a groups of assets (bundles) in parallel while the main loop keeps running uninterrupted. 
    3a. I am thinking of a process similar to the parallel loading of test screens in the background in `C:\projects\csharp\CapriKit\source\CapriKit.Tests.Tool\Program.cs` with a `LightweightChannel` `C:\projects\csharp\CapriKit\source\CapriKit.Concurrency\Primitives\LightweightChannel.cs`
    3b. I wonder if I need two steps here, full background work and then coordination with the main loop to finish the loading of things like textures that need to be uploaded to GPU memory, which I think you cannot do in parallel/without a DeviceContext and help of the main thread.
    3c. I need a way to identify assets (maybe just a record  `Asset` or `Asset<T>` which holds the relative path to the asset). If I load a bundle I should be able to wait for the bundle to be fully loaded and then get the concrete Texture, Model, etc... object from the bundle using that record.
    3d. Somehow that Texture, model, etc.. object needs to have a point of indirection to support that hot reloading


I am saying bundle here as a helpful abstraction in code, but on disk I want each file to stay independent. I know it is a common optimization to compress multiple assets into one file and load those together (better disk IO) but I think that will complicate hot reloading and other IO code. (Happy to be proven wrong though, so maybe discuss it as an extra option but assume for now we are not doing it).

## Assumptions
1. Assume assets do not need to carry extra information for the content pipeline. For example, if a texture should be loaded as normal map, srgb or linear texture is determined by the caller at run-time, not via an extra config file.
2. Assume that if an asset is loaded it stays loaded, we're more going for games like Factorio, Stationeers, Satisfactory and not for level-based games.

## Examples
I have created a similar system before. See `C:\projects\csharp\MiniEngine3\src\Mini.Engine.Content\` and especially `C:\projects\csharp\MiniEngine3\src\Mini.Engine.Content\ContentManager.cs` and `C:\projects\csharp\MiniEngine3\src\Mini.Engine.Content\ContentProcessor.cs` but there were a couple of problems
- Background loading was super hacky
- A lot of boilerplate code, for example in the folder `C:\projects\csharp\MiniEngine3\src\Mini.Engine.Content\Shaders\` you can see that I needed 3 classes for each type of shader. I really want to have to use a lot less code.
