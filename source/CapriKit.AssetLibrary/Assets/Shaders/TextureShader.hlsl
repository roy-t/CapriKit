#include "Includes/Normals.hlsl"

#pragma Input
struct VS_INPUT
{
    float4 position : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct PS_INPUT
{
    float4 position : SV_POSITION;
    float3 normal : NORMAL;
    float3 world : WORLD;
    float2 uv : TEXCOORD0;
};

cbuffer Constants : register(b0)
{
    float4x4 ViewProjection;
    float4x4 World;
};

Texture2D Diffuse : register(t0);
Texture2D Normal: register(t1);

sampler DiffuseSampler : register(s0);
sampler NormalSampler : register(s1);

static const float3 LIGHT_DIRECTION = normalize(float3(-0.4f,-1.0f, -1.0f));
static const float3 LIGHT_COLOR = float3(1.0f, 1.0f, 1.0f);
static const float3 AMBIENT_COLOR = float3(0.0f, 0.0f, 0.0f);

#pragma VertexShader
PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output;
    float4x4 wvp = mul(ViewProjection, World);
    output.position = mul(wvp, input.position);
    output.normal = mul((float3x3) World, input.normal);
    output.world = mul(World, input.position).xyz;
    output.uv = input.uv;

    return output;
}

#pragma PixelShader
float4 PS(PS_INPUT input) : SV_Target
{
    float3 N = normalize(input.normal);
    float3 texNormal = SampleNormal(Normal, NormalSampler, input.uv);
    float3 normal = PerturbNormal(texNormal, N, input.world, input.uv);
    
    float3 diffuse = Diffuse.Sample(DiffuseSampler, input.uv).rgb;
    float d = saturate(dot(normal, -LIGHT_DIRECTION)); // surface to light!
    float3 color = diffuse * (AMBIENT_COLOR + LIGHT_COLOR * d);///

    //return float4(d, d, d, 1.0f);
    return float4(color, 1.0f);
}
