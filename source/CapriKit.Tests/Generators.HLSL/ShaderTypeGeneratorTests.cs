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
            new (@"C:/project/Assets/Shaders/Lines/LineShader.hlsl", SourceText.From(ShaderWithRelativeIncludes, Encoding.UTF8))
        ];

        IEnumerable<(string fileName, SourceText content)> generatedFiles =
        [
           new (@"LineShader.g.cs", SourceText.From(ExpectedGeneratedSource, Encoding.UTF8))
        ];

        await Assert.That(GeneratorSubject.OfType<ShaderTypeGenerator>())
            .WithAdditionalFiles(additionalFiles)
            .Generates(generatedFiles);
    }

    private const string ExpectedGeneratedSource = """
        // TODO
        """;

    private const string ConfigJson = """
        {
          "targetNamespace": "MyGame.Shaders",
          "contentRoot": "Assets/Shaders"
        }
        """;

    private const string ShaderWithRelativeIncludes = """
        #include "utils/defines.hlsl"

        sampler TextureSampler : register(s0);
       
        struct PS_INPUT
        {
            float4 position : SV_POSITION;
        };

        struct COMPLEX
        {
            float4 mat[3][2];
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
}
