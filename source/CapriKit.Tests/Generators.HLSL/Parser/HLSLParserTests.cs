using CapriKit.Generators.HLSL.Parser;
using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL.Parser;

internal class HLSLParserTests
{
    [Test]
    public async Task ParseLineShader()
    {
        var tokens = HLSLTokenizer.Parse(LineShader);
        var result = HLSLParser.Parse(tokens);

        await Assert.That(result.Variables).Count().IsEqualTo(2);
        await Assert.That(result.Variables[0].Type).IsEqualTo("sampler");
        await Assert.That(result.Variables[0].Name).IsEqualTo("TextureSampler");
        await Assert.That(result.Variables[0].Register).IsEqualTo(0);

        await Assert.That(result.Variables[1].Type).IsEqualTo("Texture2D");
        await Assert.That(result.Variables[1].Name).IsEqualTo("Color");
        await Assert.That(result.Variables[1].Register).IsEqualTo(4);

        await Assert.That(result.Structures).Count().IsEqualTo(3);
        await Assert.That(result.ConstantBuffers).Count().IsEqualTo(1);
        await Assert.That(result.EntryPoints).Count().IsEqualTo(2);

        await Assert.That(result.Structures[0].Name).IsEqualTo("VS_INPUT");
        await Assert.That(result.ConstantBuffers[0].Name).IsEqualTo("Constants");
        await Assert.That(result.EntryPoints[0].Kind).IsEqualTo(EntryPointKind.VertexShader);
        await Assert.That(result.EntryPoints[0].Name).IsEqualTo("VS");
        await Assert.That(result.EntryPoints[1].Kind).IsEqualTo(EntryPointKind.PixelShader);
        await Assert.That(result.EntryPoints[1].Name).IsEqualTo("PS");
        await Assert.That(result.Includes[0].Kind).IsEqualTo(IncludeKind.System);
        await Assert.That(result.Includes[0].Path).IsEqualTo("std.io");
        await Assert.That(result.Includes[1].Kind).IsEqualTo(IncludeKind.Local);
        await Assert.That(result.Includes[1].Path).IsEqualTo("defines.hlsl");
    }

    private static readonly string LineShader = """
        #include <std.io>
        #include "defines.hlsl"

        sampler TextureSampler : register(s0);
        precise rowmajor Matrix2x2 Color : register(t4);

        struct VS_INPUT
        {
            float3 position : POSITION;
        };

        struct PS_INPUT
        {
            float4 position : SV_POSITION;
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
        PS_INPUT VS(VS_INPUT input, uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
        {
            PS_INPUT output;

            float4 position = float4(input.position, 1.0f);
            output.position = mul(WorldViewProjection, position);

            return output;
        }

        #pragma PixelShader
        OUTPUT PS(PS_INPUT input)
        {
            OUTPUT output;

            output.color = ToLinear(Color);
            return output;
        }
        """;
}
