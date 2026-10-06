struct Material
{
    float3 baseColor; // Already in linear color space
    float metallic;
    float roughness;
    float3 emissionColor; // Already in linear color space
    float emissionStrength;
};

struct Triangle
{
    uint materialIndex;
};

#pragma Input
struct VS_INPUT
{
    float3 position : POSITION;
    float3 normal: NORMAL0;
};

struct PS_INPUT
{
    float4 position : SV_POSITION;
};

cbuffer Constants : register(b0)
{
    float4x4 ViewProjection;
    float4x4 World;
    uint TriangleOffset;
};

StructuredBuffer<Material> Materials : register(t0);
StructuredBuffer<Triangle> Triangles : register(t1);

#pragma VertexShader
PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output;
    float4x4 wvp = mul(ViewProjection, World);
    output.position = mul(wvp, float4(input.position, 1.0f));

    return output;
}

#pragma PixelShader
float4 PS(PS_INPUT input, uint primitive : SV_PrimitiveID) : SV_Target
{
    Triangle tri = Triangles[primitive + TriangleOffset];
    Material mat = Materials[tri.materialIndex];
    return float4(mat.baseColor, 1.0f);   
}
