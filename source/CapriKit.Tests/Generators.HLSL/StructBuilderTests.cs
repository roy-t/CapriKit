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

        var tokens = HLSLTokenizer.Parse(source);
        var parser = new ParseState(tokens);
        if (!ConstantBufferParser.TryParse(parser, out var buffer))
        {
            Assert.Fail("Failed to parse constant buffer");
            return;
        }

        var builder = new SourceCodeBuilder();
        var translator = new StructTranslator();
        StructBuilder.WriteStruct(builder, translator, buffer);
        var code = builder.Build();
        await Assert.That(code).IsEqualTo(expected);
    }

    [Test]
    public async Task WriteStruct_Struct()
    {
        var source = """
            struct Data
            {
                float3 A : SV_SEMANTIC; 
                nointerpolation float B;
                float3 C;               
                float2 D;               
                float E[3][1]; // flattened to [3]
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

        var tokens = HLSLTokenizer.Parse(source);
        var parser = new ParseState(tokens);
        if (!StructureParser.TryParse(parser, out var structure))
        {
            Assert.Fail("Failed to parse struct");
            return;
        }

        var builder = new SourceCodeBuilder();
        var translator = new StructTranslator();
        StructBuilder.WriteStruct(builder, translator, structure);
        var code = builder.Build();
        await Assert.That(code).IsEqualTo(expected);
    }

    [Test]
    public async Task WriteStruct_StructWithStructs()
    {
        var elementSource = """
            struct Element
            {
                float2 El;
            };
            """;

        var dataSource = """
            struct Data
            {
                Element A;
                Element B;
            };
            """;

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

        var elementTokens = HLSLTokenizer.Parse(elementSource);
        var elementsParser = new ParseState(elementTokens);
        if (!StructureParser.TryParse(elementsParser, out var elementStructure))
        {
            Assert.Fail("Failed to parse struct");
            return;
        }

        var dataTokens = HLSLTokenizer.Parse(dataSource);
        var dataParser = new ParseState(dataTokens);
        if (!StructureParser.TryParse(dataParser, out var dataStructure))
        {
            Assert.Fail("Failed to parse struct");
            return;
        }

        var builder = new SourceCodeBuilder();
        var translator = new StructTranslator();
        translator.LayoutStruct(elementStructure); // ensure elements are already known
        StructBuilder.WriteStruct(builder, translator, dataStructure);
        var code = builder.Build();
        await Assert.That(code).IsEqualTo(expected);
    }
}
