#ifndef __NORMALS
#define __NORMALS

// Samples and reconstructs a normal that is stored in a texture
// with the X coordinate in the red channel and the Y coordinate in the green channel
float3 SampleNormal(Texture2D tex, sampler samp, float2 uv)
{
    float2 n = tex.Sample(samp, uv).rg * 2.0f - 1.0f;
    return float3(n, sqrt(saturate(1.0f - dot(n, n))));
}

// Normal mapping as described by Christian Schüler in
// http://www.thetenthplanet.de/archives/1180
float3x3 CotangentFrame(float3 N, float3 p, float2 uv)
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
    
float3 PerturbNormal(float3 texNormal, float3 geomNormal, float3 worldPosition, float2 uv)
{
    float3x3 tbn = CotangentFrame(geomNormal, worldPosition, uv);
    return normalize(mul(texNormal, tbn));
}

// Normal points in the direction of the viewer if the vertices are given in clock wise order
float3 GetTriangleNormal(float4 t0, float4 t1, float4 t2)
{
    float3 c = cross(t2.xyz - t0.xyz, t1.xyz - t0.xyz);
    return normalize(c);
}

#endif
