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
        await Assert.That(result.Variables[0].Register).IsEqualTo(0u);

        await Assert.That(result.Variables[1].Type).IsEqualTo("Matrix2x2");
        await Assert.That(result.Variables[1].Name).IsEqualTo("Mat");
        await Assert.That(result.Variables[1].Register).IsEqualTo(4u);

        await Assert.That(result.Structures).Count().IsEqualTo(3);
        await Assert.That(result.ConstantBuffers).Count().IsEqualTo(1);

        var entryPoints = result.Functions.Where(f => f.Kind != FunctionKind.Function).ToList();
        await Assert.That(entryPoints).Count().IsEqualTo(2);

        await Assert.That(result.Structures[0].Name).IsEqualTo("VS_INPUT");
        await Assert.That(result.ConstantBuffers[0].Name).IsEqualTo("Constants");
        await Assert.That(entryPoints[0].Kind).IsEqualTo(FunctionKind.VertexShader);
        await Assert.That(entryPoints[0].Name).IsEqualTo("VS");
        await Assert.That(entryPoints[1].Kind).IsEqualTo(FunctionKind.PixelShader);
        await Assert.That(entryPoints[1].Name).IsEqualTo("PS");
        await Assert.That(result.Includes[0].Kind).IsEqualTo(IncludeKind.System);
        await Assert.That(result.Includes[0].Path).IsEqualTo("std.io");
        await Assert.That(result.Includes[1].Kind).IsEqualTo(IncludeKind.Local);
        await Assert.That(result.Includes[1].Path).IsEqualTo("defines.hlsl");
    }

    private static readonly string LineShader = """
        #include <std.io>
        #include "defines.hlsl"

        sampler TextureSampler : register(s0);
        precise row_major Matrix2x2 Mat : register(t4);

        static float4 ToLinear(float4 v)
        {
            float3 rgb = pow(abs(v.rgb), float3(2.2f, 2.2f, 2.2f));
            return float4(rgb.rgb, v.a);
        }

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
