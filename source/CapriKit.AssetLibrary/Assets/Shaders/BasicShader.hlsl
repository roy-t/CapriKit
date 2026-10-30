#include "Includes/Gamma.hlsl"

#pragma Input
struct VS_INPUT
{
    float2 position : POSITION;
    float4 color : COLOR;
};

struct PS_INPUT
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
};

cbuffer Constants : register(b0)
{
    float4x4 ProjectionMatrix;
};

#pragma VertexShader
PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output;
    output.position = mul(ProjectionMatrix, float4(input.position.xy, 0.0f, 1.0f));
    output.color = ToLinear(input.color);
    return output;
}

#pragma PixelShader
float4 PS(PS_INPUT input) : SV_Target
{    
    return input.color;
}
