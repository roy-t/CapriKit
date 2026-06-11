using CapriKit.Generators.HLSL;
using CapriKit.Tests.TestUtilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CapriKit.Tests.Generators.HLSL;

internal class ShaderTypeGeneratorTests
{
    [Test]
    public async Task Execute()
    {
        // TODO: instead of doing complex guessing of what to generate the input element description for
        // we should look at a pragma on a struct and only generate those on demand
        // the outcome of this test should stay the same (it already has the pragma) but the logic
        // in the parser/generator needs to change to take into account this pragma instead of the
        // weird guessing game. TODO: create an InputElementParser, similar to the EntryPoint Parser and go from there

        throw new Exception("TODO");

        IEnumerable<(string fileName, SourceText content)> additionalFiles =
        [
            new (@"C:/project/CapriKit.Generators.HLSL.json", SourceText.From(ConfigJson, Encoding.UTF8)),
            new (@"C:/project/Assets/Shaders/utils/Defines.hlsl", SourceText.From(ShaderFileToInclude, Encoding.UTF8)),
            new (@"C:/project/Assets/Shaders/Lines/LineShader.hlsl", SourceText.From(ShaderWithRelativeIncludes, Encoding.UTF8)),
        ];

        IEnumerable<(string fileName, SourceText content)> generatedFiles =
        [
           new (@"Lines.LineShader.Hlsl.g.cs", SourceText.From(ExpectedGeneratedSource, Encoding.UTF8)),
           new (@"Utils.Defines.Hlsl.g.cs", SourceText.From(ExpectedGeneratedInclude, Encoding.UTF8))
        ];

        IEnumerable<PackageIdentity> packageReferences =
        [
            new PackageIdentity("Vortice.Direct3D11", "3.8.3")
        ];

        await Assert.That(GeneratorSubject.OfType<ShaderTypeGenerator>())
            .WithAdditionalFiles(additionalFiles)
            .WithPackageReferences(packageReferences)
            .Generates(generatedFiles);
    }

    [Test]
    public async Task ShaderInContentRootUsesTargetNamespace()
    {
        IEnumerable<(string fileName, SourceText content)> additionalFiles =
        [
            new (@"C:/project/CapriKit.Generators.HLSL.json", SourceText.From(ContentRootConfigJson, Encoding.UTF8)),
            new (@"C:/project/Assets/Basic.hlsl", SourceText.From(ShaderInContentRoot, Encoding.UTF8))
        ];

        IEnumerable<(string fileName, SourceText content)> generatedFiles =
        [
           new (@"Basic.Hlsl.g.cs", SourceText.From(ExpectedContentRootSource, Encoding.UTF8))
        ];

        await Assert.That(GeneratorSubject.OfType<ShaderTypeGenerator>())
            .WithAdditionalFiles(additionalFiles)
            .Generates(generatedFiles);
    }

    private const string ContentRootConfigJson = """
        {
          "targetNamespace": "MyGame.Shaders",
          "contentRoot": "Assets"
        }
        """;

    private const string ShaderInContentRoot = """
        sampler TextureSampler : register(s0);
        """;

    private const string ExpectedContentRootSource = """
        namespace MyGame.Shaders;
        public class Basic
        {
            public const string Path = "Basic.hlsl";
            public const uint TextureSampler = 0;
        }

        """;

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

        #pragma GenerateInputElementDescription
        struct VS_INPUT
        {
            float2 pos : POSITION;
        };

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

        #pragma VertexShader
        PS_INPUT VS(VS_INPUT input)
        {
            return input.pos;
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
            public struct VsInput
            {
                /// <summary>
                /// Semantic: POSITION
                /// </summary>
                public System.Numerics.Vector2 Pos;
            }
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
            /// Kind: VertexShader
            /// </summary>
            public const string Vs = "VS";
            public static readonly Vortice.Direct3D11.InputElementDescription[] VsInputElementDescription = new Vortice.Direct3D11.InputElementDescription[]
            {
                new("POSITION", 0, Vortice.DXGI.Format.R32G32_Float, 0, 0, Vortice.Direct3D11.InputClassification.PerVertexData, 0),
            };
            /// <summary>
            /// Kind: PixelShader
            /// </summary>
            public const string Ps = "PS";
        }
        
        """;
}
