using CapriKit.Generators.HLSL;
using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;
using System.Collections.Immutable;

namespace CapriKit.Tests.Generators.HLSL;

internal class ShaderIndexTests
{
    [Test]
    public async Task CreateStructScope_TransitiveIncludesExposeStructs()
    {
        var leafPath = @"C:\project\Assets\Shaders\Leaf.hlsl";
        var middlePath = @"C:\project\Assets\Shaders\Middle.hlsl";
        var rootPath = @"C:\project\Assets\Shaders\Root.hlsl";

        var leaf = Parse("""
            struct LEAF
            {
                float4 Value;
            };
            """);
        var middle = Parse("""
            #include "leaf.hlsl"

            struct WRAPPER
            {
                LEAF Leaf;
            };
            """);
        var root = Parse("""
            #include "middle.hlsl"

            struct ROOT
            {
                WRAPPER Wrapper;
            };
            """);

        var index = new ShaderIndex(ImmutableArray.Create<(string Path, ShaderMetadata? Shader)>(
            (leafPath, leaf),
            (middlePath, middle),
            (rootPath, root)));

        var scope = index.CreateStructScope(rootPath, root);
        var translator = new StructTranslator(scope);
        var layout = translator.LayoutStruct(root.Structures[0]);

        await Assert.That(scope.IncludedFiles.Select(f => Path.GetFileName(f.Path))).IsEquivalentTo(["Leaf.hlsl", "Middle.hlsl"]);
        await Assert.That(layout.Type.Size).IsEqualTo(16u);
        await Assert.That(layout.Members[0].Type.Name).IsEqualTo("Wrapper");
        await Assert.That(layout.Members[0].Type.Size).IsEqualTo(16u);
    }

    [Test]
    public async Task CreateStructScope_IgnoresSystemAndMissingIncludes()
    {
        var rootPath = @"C:\project\Assets\Shaders\Root.hlsl";
        var root = Parse("""
            #include <system.hlsl>
            #include "missing.hlsl"

            struct ROOT
            {
                float4 Value;
            };
            """);

        var index = new ShaderIndex(ImmutableArray.Create<(string Path, ShaderMetadata? Shader)>((rootPath, root)));

        var scope = index.CreateStructScope(rootPath, root);

        await Assert.That(scope.IncludedFiles).IsEmpty();
    }

    private static ShaderMetadata Parse(string source)
    {
        var tokens = HLSLTokenizer.Parse(source);
        return HLSLParser.Parse(tokens);
    }
}
