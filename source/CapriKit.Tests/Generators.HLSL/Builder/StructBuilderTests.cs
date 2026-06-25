using CapriKit.Generators.HLSL.Builder;
using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Builder;

internal class StructBuilderTests
{
    [Test]
    public async Task WriteCBufferAppliesConstantBufferPacking()
    {
        var source = """
            cbuffer Constants
            {
                float3 A : SV_SEMANTIC;
                nointerpolation float B;
                float3 C;
                float2 D;
                float E[3][1];
            };
            """;

        var expected = """
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = 96)]
            public struct Constants
            {
                /// <summary>
                /// Original Name: A
                /// Semantic: SV_SEMANTIC
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(0)]
                public System.Numerics.Vector3 A;
                /// <summary>
                /// Original Name: B
                /// Modifiers: nointerpolation
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(12)]
                public float B;
                /// <summary>
                /// Original Name: C
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(16)]
                public System.Numerics.Vector3 C;
                /// <summary>
                /// Original Name: D
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(32)]
                public System.Numerics.Vector2 D;
                /// <summary>
                /// Original Name: E
                /// Dimensions: [3][1]
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(48)]
                public ConstantsEArray E;
            }
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Size = 16)]
            public struct ConstantsEElement
            {
                public float Value;
            }
            [System.Runtime.CompilerServices.InlineArray(3)]
            public struct ConstantsEArray
            {
                private ConstantsEElement element0;
            }

            """;

        var buffer = ParseConstantBuffer(source);
        var builder = new SourceCodeBuilder();
        new StructBuilder().WriteCBuffer(builder, "", Shader(), buffer);

        await Assert.That(builder.Build()).IsEqualTo(expected);
    }

    [Test]
    public async Task WriteStructPacksTightlyWithFlattenedArray()
    {
        var source = """
            struct Data
            {
                float3 A : SV_SEMANTIC;
                nointerpolation float B;
                float3 C;
                float2 D;
                float E[3][1];
            };
            """;

        var expected = """
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = 48)]
            public struct Data
            {
                /// <summary>
                /// Original Name: A
                /// Semantic: SV_SEMANTIC
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(0)]
                public System.Numerics.Vector3 A;
                /// <summary>
                /// Original Name: B
                /// Modifiers: nointerpolation
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(12)]
                public float B;
                /// <summary>
                /// Original Name: C
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(16)]
                public System.Numerics.Vector3 C;
                /// <summary>
                /// Original Name: D
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(28)]
                public System.Numerics.Vector2 D;
                /// <summary>
                /// Original Name: E
                /// Dimensions: [3][1]
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(36)]
                public DataEArray E;
            }
            [System.Runtime.CompilerServices.InlineArray(3)]
            public struct DataEArray
            {
                private float element0;
            }

            """;

        var structure = ParseStructure(source);
        var builder = new SourceCodeBuilder();
        new StructBuilder().WriteStruct(builder, "", Shader(), structure);

        await Assert.That(builder.Build()).IsEqualTo(expected);
    }

    [Test]
    public async Task WriteStructResolvesRegisteredNestedStruct()
    {
        var element = ParseStructure("""
            struct Element
            {
                float2 El;
            };
            """);
        var data = ParseStructure("""
            struct Data
            {
                Element A;
                Element B;
            };
            """);

        var expected = """
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = 16)]
            public struct Data
            {
                /// <summary>
                /// Original Name: A
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(0)]
                public Element A;
                /// <summary>
                /// Original Name: B
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(8)]
                public Element B;
            }

            """;

        var structBuilder = new StructBuilder();
        structBuilder.RegisterStructs([("", Shader(element))]); // ensure Element is already known

        var builder = new SourceCodeBuilder();
        structBuilder.WriteStruct(builder, "", Shader(data), data);

        await Assert.That(builder.Build()).IsEqualTo(expected);
    }

    [Test]
    public async Task WriteStructEmitsInputElementDescriptionForVertexInput()
    {
        var structure = ParseStructure("""
            #pragma Input
            struct VS_INPUT
            {
                float3 Position : POSITION;
            };
            """);

        var expected = """
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = 12)]
            public struct VsInput
            {
                /// <summary>
                /// Original Name: Position
                /// Semantic: POSITION
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(0)]
                public System.Numerics.Vector3 Position;
            }
            public static readonly Vortice.Direct3D11.InputElementDescription[] VsInputElementDescription = new Vortice.Direct3D11.InputElementDescription[]
            {
                new("POSITION", 0, Vortice.DXGI.Format.R32G32B32_Float, 0, 0, Vortice.Direct3D11.InputClassification.PerVertexData, 0),
            };

            """;

        var builder = new SourceCodeBuilder();
        new StructBuilder().WriteStruct(builder, "", Shader(), structure);

        await Assert.That(builder.Build()).IsEqualTo(expected);
    }

    private static ConstantBuffer ParseConstantBuffer(string source)
    {
        var state = new ParseState(HLSLTokenizer.Parse(source));
        if (!ConstantBufferParser.TryParse(state, out var buffer))
        {
            throw new InvalidOperationException("Failed to parse constant buffer");
        }

        return buffer;
    }

    private static Structure ParseStructure(string source)
    {
        var state = new ParseState(HLSLTokenizer.Parse(source));
        if (!StructureParser.TryParse(state, out var structure))
        {
            throw new InvalidOperationException("Failed to parse struct");
        }

        return structure;
    }

    private static ShaderMetadata Shader(params Structure[] structures) => new([], [], structures, [], []);
}
