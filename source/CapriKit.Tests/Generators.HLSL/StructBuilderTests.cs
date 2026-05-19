using CapriKit.Generators.HLSL;
using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL;

internal class StructBuilderTests
{
    [Test]
    public async Task WriteStruct_ConstantBuffer()
    {
        var source = """
            cbuffer Constants // takes up 96 bytes although there is only 48 bytes of data
            {
                float3 A : SV_SEMANTIC; // bytes 0-3, 4-7, 8-11
                nointerpolation float B;// bytes 12-15, fits in the last 4 bytes of this block
                float3 C;               // bytes 16-27
                float2 D;               // bytes 32-39, does not fit in the previous block, so starts at new boundary
                float E[3][1];          // bytes 48-95, arrays always starts at a new block, flattened to [3]
            };
            """;

        var expected = """
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = 96)]
            public struct Constants
            {
                /// <summary>
                /// Semantic: SV_SEMANTIC
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(0)]
                public System.Numerics.Vector3 A;
                /// <summary>
                /// Modifiers: nointerpolation
                /// </summary>
                [System.Runtime.InteropServices.FieldOffset(12)]
                public float B;
                [System.Runtime.InteropServices.FieldOffset(16)]
                public System.Numerics.Vector3 C;
                [System.Runtime.InteropServices.FieldOffset(32)]
                public System.Numerics.Vector2 D;
                /// <summary>
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

        var tokens = HLSLTokenizer.Parse(source);
        var parser = new ParseState(tokens);
        if (!ConstantBufferParser.TryParse(parser, out var buffer))
        {
            Assert.Fail("Failed to parse constant buffer");
            return;
        }

        var builder = new SourceCodeBuilder();
        StructBuilder.WriteStruct(builder, buffer);
        var code = builder.Build();
        await Assert.That(code).IsEqualTo(expected);
    }

    [Test]
    public async Task WriteStruct_Struct()
    {
        var source = """
            struct Data // sequential layout
            {
                float3 A : SV_SEMANTIC; 
                nointerpolation float B;
                float3 C;               
                float2 D;               
                float E[3][1]; // flattened to [3]
            };
            """;

        var expected = """
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public struct Data
            {
                /// <summary>
                /// Semantic: SV_SEMANTIC
                /// </summary>
                public System.Numerics.Vector3 A;
                /// <summary>
                /// Modifiers: nointerpolation
                /// </summary>
                public float B;
                public System.Numerics.Vector3 C;
                public System.Numerics.Vector2 D;
                /// <summary>
                /// Dimensions: [3][1]
                /// </summary>
                public DataEArray E;
            }
            [System.Runtime.CompilerServices.InlineArray(3)]
            public struct DataEArray
            {
                private float element0;
            }
            
            """;

        var tokens = HLSLTokenizer.Parse(source);
        var parser = new ParseState(tokens);
        if (!StructureParser.TryParse(parser, out var structure))
        {
            Assert.Fail("Failed to parse struct");
            return;
        }

        var builder = new SourceCodeBuilder();
        StructBuilder.WriteStruct(builder, structure);
        var code = builder.Build();
        await Assert.That(code).IsEqualTo(expected);
    }
}
