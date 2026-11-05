#ifndef __NORMALS
#define __NORMALS

// Samples and reconstructs a normal that is stored in a texture
// with the X coordinate in the red channel and the Y coordinate in the green channel
float3 SampleNormal(Texture2D tex, sampler samp, float2 uv)
{
    float2 n = tex.Sample(samp, uv).rg * 2.0f - 1.0f;
    return float3(n, sqrt(saturate(1.0f - dot(n, n))));
}

float3x3 _CotangentFrame(float3 N, float3 p, float2 uv)
{
    // get edge vectors of the pixel triangle
    float3 dp1 = ddx(p);
    float3 dp2 = ddy(p);
    float2 duv1 = ddx(uv);
    float2 duv2 = ddy(uv);

    // solve the linear system
    float3 dp2perp = cross(dp2, N);
    float3 dp1perp = cross(N, dp1);
    float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    float3 B = dp2perp * duv1.y + dp1perp * duv2.y;

    // construct a scale-invariant frame 
    float handedness = dot(N, cross(dp1, dp2)) < 0.0f ? -1.0f : 1.0f;
    float invmax = handedness * rsqrt(max(dot(T, T), dot(B, B)));
    return float3x3(T * invmax, B * invmax, N);
}

// Normal mapping without precomputed tangents. Algorithm by Christian Schüler.
// See: http://www.thetenthplanet.de/archives/1180.
// Transforms a tangent-space normal from a normal map to world space.
// Uses screen-space derivatives, so only call this from a pixel shader.
// - texNormal: decoded tangent-space normal in [-1, 1], e.g. from SampleNormal
// - geomNormal: normalized, interpolated vertex normal (world space)
// - worldPosition: position of the pixel being shaded (world space)
// - uv: the coordinates used to sample the normal map
float3 PerturbNormal(float3 texNormal, float3 geomNormal, float3 worldPosition, float2 uv)
{
    float3x3 tbn = _CotangentFrame(geomNormal, worldPosition, uv);
    return normalize(mul(texNormal, tbn));
}

// Computes the influence of a light source on a surface using Lambert's cosine law.
// Takes two normalized vectors. N is the surface normal and L points from the surface towards the light.
float Lambert(float3 N, float3 L)
{
    return saturate(dot(N, L));
}

// Reconstructs a normal given the three vertices of a triangle.
// The normal points in the direction of the viewer if the vertices are given in clock wise order.
float3 GetTriangleNormal(float4 t0, float4 t1, float4 t2)
{
    float3 c = cross(t2.xyz - t0.xyz, t1.xyz - t0.xyz);
    return normalize(c);
}

#endif
