using CapriKit.Generators.HLSL;
using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL;

internal class StructBuilderTests
{
    [Test]
    public async Task WritesConstantBufferFollowingPackingRules()
    {
        // float3 leaves 4 bytes in its register, but the array must start a new
        // one; each array element then takes a full 16-byte register.
        var source = """
            cbuffer Constants
            {
                float3 Tint;
                float Weights[2];
                float4 Color;
            };
            """;
        ConstantBufferParser.TryParse(new ParseState(HLSLTokenizer.Parse(source)), out var buffer);

        var builder = new SourceCodeBuilder();
        StructBuilder.WriteStruct(builder, buffer!);
        var code = builder.ToString();

        // TODO: these tests make no sense, just verify the full code looks like we thought it would

        // The buffer is 16-byte aligned: Tint(0) + Weights(16..48) + Color(48..64).
        await Assert.That(code).Contains("System.Runtime.InteropServices.LayoutKind.Explicit, Size = 64");
        await Assert.That(code).Contains("[System.Runtime.InteropServices.FieldOffset(0)]");
        await Assert.That(code).Contains("public System.Numerics.Vector3 Tint;");
        await Assert.That(code).Contains("[System.Runtime.InteropServices.FieldOffset(16)]");
        await Assert.That(code).Contains("public ConstantsWeightsArray Weights;");
        await Assert.That(code).Contains("[System.Runtime.InteropServices.FieldOffset(48)]");
        await Assert.That(code).Contains("public System.Numerics.Vector4 Color;");
    }

    [Test]
    public async Task WritesPaddedHelperStructsForArrays()
    {
        var source = "cbuffer Constants { float Weights[2]; };";
        ConstantBufferParser.TryParse(new ParseState(HLSLTokenizer.Parse(source)), out var buffer);

        var builder = new SourceCodeBuilder();
        StructBuilder.WriteStruct(builder, buffer!);
        var code = builder.ToString();

        // Each element is padded to a full 16-byte register and exposed as an InlineArray.
        await Assert.That(code).Contains("System.Runtime.InteropServices.LayoutKind.Sequential, Size = 16");
        await Assert.That(code).Contains("public struct ConstantsWeightsElement");
        await Assert.That(code).Contains("public float Value;");
        await Assert.That(code).Contains("[System.Runtime.CompilerServices.InlineArray(2)]");
        await Assert.That(code).Contains("public struct ConstantsWeightsArray");
    }

    [Test]
    public async Task WritesRegularStructArraysAsInlineArrays()
    {
        var source = """
            struct Light
            {
                float3 Position;
                float Intensities[4];
            };
            """;
        StructureParser.TryParse(new ParseState(HLSLTokenizer.Parse(source)), out var structure);

        var builder = new SourceCodeBuilder();
        StructBuilder.WriteStruct(builder, structure!);
        var code = builder.ToString();

        // Regular structs have no padding rules: elements are stored back-to-back.
        await Assert.That(code).Contains("public struct Light");
        await Assert.That(code).Contains("public System.Numerics.Vector3 Position;");
        await Assert.That(code).Contains("public LightIntensitiesArray Intensities;");
        await Assert.That(code).Contains("[System.Runtime.CompilerServices.InlineArray(4)]");
        await Assert.That(code).Contains("private float element0;");
        // No more unsafe fixed-size buffers.
        await Assert.That(code).DoesNotContain("fixed");
        await Assert.That(code).DoesNotContain("unsafe");
    }
}
