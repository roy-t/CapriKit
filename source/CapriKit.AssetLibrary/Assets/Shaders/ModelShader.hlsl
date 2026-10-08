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
    float3 normal : NORMAL;
};

cbuffer Constants : register(b0)
{
    float4x4 ViewProjection;
    float4x4 World;
};

StructuredBuffer<Material> Materials : register(t0);
StructuredBuffer<Triangle> Triangles : register(t1);

#pragma VertexShader
PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output;
    float4x4 wvp = mul(ViewProjection, World);
    output.position = mul(wvp, float4(input.position, 1.0f));
    output.normal = mul((float3x3) World, input.normal);

    return output;
}

static const float3 LIGHT_DIRECTION = normalize(float3(-0.4f, 1.0f, -0.6f));
static const float3 LIGHT_COLOR = float3(1.0f, 1.0f, 1.0f);
static const float3 AMBIENT_COLOR = float3(0.15f, 0.15f, 0.15f);

#pragma PixelShader
float4 PS(PS_INPUT input, uint primitive : SV_PrimitiveID) : SV_Target
{
    Triangle tri = Triangles[primitive];
    Material mat = Materials[tri.materialIndex];

    float3 normal = normalize(input.normal);
    float diffuse = saturate(dot(normal, LIGHT_DIRECTION));
    float3 color = mat.baseColor * (AMBIENT_COLOR + LIGHT_COLOR * diffuse)
                   + mat.emissionColor * mat.emissionStrength;

    return float4(color, 1.0f);
}
