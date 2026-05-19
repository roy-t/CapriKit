using CapriKit.Generators.HLSL;
using CapriKit.Tests.TestUtilities;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CapriKit.Tests.Generators.HLSL;

internal class ShaderTypeGeneratorTests
{
    [Test]
    public async Task Execute()
    {
        IEnumerable<(string fileName, SourceText content)> additionalFiles =
        [
            new (@"C:/project/CapriKit.Generators.HLSL.json", SourceText.From(ConfigJson, Encoding.UTF8)),
            new (@"C:/project/Assets/Shaders/utils/Defines.hlsl", SourceText.From(ShaderFileToInclude, Encoding.UTF8)),
            new (@"C:/project/Assets/Shaders/Lines/LineShader.hlsl", SourceText.From(ShaderWithRelativeIncludes, Encoding.UTF8))
        ];

        IEnumerable<(string fileName, SourceText content)> generatedFiles =
        [
           new (@"Lines.LineShader.Hlsl.g.cs", SourceText.From(ExpectedGeneratedSource, Encoding.UTF8)),
           new (@"Utils.Defines.Hlsl.g.cs", SourceText.From(ExpectedGeneratedInclude, Encoding.UTF8))
        ];

        await Assert.That(GeneratorSubject.OfType<ShaderTypeGenerator>())
            .WithAdditionalFiles(additionalFiles)
            .Generates(generatedFiles);
    }

    private const string ConfigJson = """
        {
          "targetNamespace": "MyGame.Shaders",
          "contentRoot": "Assets/Shaders"
        }
        """;

    private const string ShaderFileToInclude = """
        struct DEFINED
        {
            float4 value;
        };
        """;

    private const string ShaderWithRelativeIncludes = """
        #include "../utils/defines.hlsl"

        sampler TextureSampler : register(s0);
       
        struct PS_INPUT
        {
            float4 position : SV_POSITION;
        };

        struct COMPLEX
        {
            float4 mat[3][2];
            DEFINED def;
        };

        struct OUTPUT
        {
            float4 color : SV_Target0;
        };

        cbuffer Constants : register(b0)
        {
            float4x4 WorldViewProjection;
            float4 Color;
        };       

        #pragma PixelShader
        OUTPUT PS(PS_INPUT input)
        {
            OUTPUT output;

            output.color = ToLinear(Color);
            return output;
        }
        """;

    private const string ExpectedGeneratedInclude = """
        namespace MyGame.Shaders.Utils;
        public class Defines
        {
            public const string Path = "utils/Defines.hlsl";
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public struct Defined
            {
                public System.Numerics.Vector4 Value;
            }
        }
        
        """;

    private const string ExpectedGeneratedSource = """
        using static MyGame.Shaders.Utils.Defines;
        namespace MyGame.Shaders.Lines;
        public class LineShader
        {
            public const string Path = "Lines/LineShader.hlsl";
            public const uint TextureSampler = 0;
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public struct PsInput
            {
                /// <summary>
                /// Semantic: SV_POSITION
                /// </summary>
                public System.Numerics.Vector4 Position;
            }
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public struct Complex
            {
                /// <summary>
                /// Dimensions: [3][2]
                /// </summary>
                public ComplexMatArray Mat;
                public Defined Def;
            }
            [System.Runtime.CompilerServices.InlineArray(6)]
            public struct ComplexMatArray
            {
                private System.Numerics.Vector4 element0;
            }
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public struct Output
            {
                /// <summary>
                /// Semantic: SV_Target0
                /// </summary>
                public System.Numerics.Vector4 Color;
            }
            public const uint ConstantsRegister = 0;
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = 80)]
            public struct Constants
            {
                [System.Runtime.InteropServices.FieldOffset(0)]
                public System.Numerics.Matrix4x4 WorldViewProjection;
                [System.Runtime.InteropServices.FieldOffset(64)]
                public System.Numerics.Vector4 Color;
            }
            /// <summary>
            /// Kind: PixelShader
            /// </summary>
            public const string Ps = "PS";
        }
        
        """;
}
