# CapriKit.Generators.AssetHandles

A source generator that generates constant strings with the relative path to your included assets.

## Usage

Create the configuration file `CapriKit.Generators.AssetHandles.json` that specifies:
- The namespace to generate the files in
- The root directory to base paths on
- The extensions of file types that you want to generate these classes from

```
{
  "targetNamespace": "CapriKit.AssetLibrary",
  "contentRoot": "Assets",
  "includedExtensions": [".blend", ".hlsl"]
}
```

Add the configuration file to your `.csproj`:
```
<ItemGroup>
    <AdditionalFiles Include="CapriKit.Generators.AssetHandles.json" />
</ItemGroup>
```

Add the assets to your `.csproj` file:
```
<ItemGroup>
    <AdditionalFiles Include="Assets\Shaders\BasicShader.hlsl" CopyToOutputDirectory="Always"/>
    ...
</ItemGroup>
```

In this example the path to `BasicShader.hlsl` is stored in the field `CapriKit.AssetLibrary.Shaders.BasicShader.hlsl`. If you use `CapriKit.IO` you can now create a `ScopedFileSystem` rooted in the `Assets` folder to access your files.
