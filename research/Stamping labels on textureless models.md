# Stamping labels on textureless models

## Context
We were looking at `BlenderModelTest` in `CapriKit.Tests.Tool`. It renders a CKT model (`Container.1TUE` from `assets/Parts.blend`) with `ModelShader.hlsl`. The CKT format has no UVs. Every triangle gets a material (base color, metallic, roughness, emission) through `SV_PrimitiveID`.

For the game I want to "stamp" logos and text onto these models. An example is a shipping company logo on the side of a container. The stamp should follow the corrugated steel, be cheap, and be placeable at random. It has to work for thousands of objects, each with a few stamps. Regular texturing felt like the wrong fit, and I worried that projectors (deferred decals) need depth information and get expensive.

## Key observation
A projector does not need the depth buffer if it is applied while shading the object it belongs to. The pixel shader already knows the position of the surface it is shading. Pass the object-space position from the vertex shader, transform it into the stamp's box, and you have the decal UV. The depth buffer only matters when a decal is drawn as a separate pass, after the fact, over geometry it doesn't know about.

Planar projection also matches how real container markings are made: paint sprayed through a stencil from straight on. On the corrugation flanks the projected image stretches, but seen from the front it looks undistorted, just like the real thing. The lighting already comes from the corrugated normals, so the stamp picks up the shape for free.

## Techniques

### 1. Object-space projected stamps in the forward pass (recommended starting point)
Each instance gets a small list of stamps. Each stamp holds an object→stamp-box transform, a texture slice and a tint. The pixel shader loops over the instance's stamps. A pixel inside the unit box samples the logo and blends it into the base color before lighting.

```hlsl
struct Stamp
{
    float4 row0, row1, row2; // object space -> stamp space, inside when all |xyz| <= 0.5
    uint slice;              // logo index in a Texture2DArray
    float3 tint;
};

struct Instance
{
    float4x4 world;
    uint stampOffset;
    uint stampCount;
};

StructuredBuffer<Instance> Instances : register(t2);
StructuredBuffer<Stamp> Stamps : register(t3);
Texture2DArray Logos : register(t4);

float3 ApplyStamps(float3 baseColor, float3 objectPos, float3 objectNormal, Instance instance)
{
    // Derivatives must be taken outside the divergent loop, transform them per stamp instead
    float3 dpdx = ddx(objectPos);
    float3 dpdy = ddy(objectPos);

    for (uint i = 0; i < instance.stampCount; i++)
    {
        Stamp s = Stamps[instance.stampOffset + i];
        float4 p = float4(objectPos, 1.0f);
        float3 local = float3(dot(s.row0, p), dot(s.row1, p), dot(s.row2, p));
        if (any(abs(local) > 0.5f)) { continue; }

        // Reject surfaces facing away from the projector (e.g. the inside or the opposite wall).
        // row2.xyz is the stamp's +Z axis in object space, by convention pointing out of the surface
        float facing = dot(objectNormal, normalize(s.row2.xyz));
        if (facing < 0.2f) { continue; }

        float2 uv = local.xy + 0.5f;
        float2 uvdx = float2(dot(s.row0.xyz, dpdx), dot(s.row1.xyz, dpdx));
        float2 uvdy = float2(dot(s.row0.xyz, dpdy), dot(s.row1.xyz, dpdy));
        float4 texel = Logos.SampleGrad(LinearClamp, float3(uv, s.slice), uvdx, uvdy);
        baseColor = lerp(baseColor, texel.rgb * s.tint, texel.a);
    }
    return baseColor;
}
```

- **Cost:** per pixel and per stamp, three dot products and a bounds test. Only pixels inside a box pay for the texture sample. With at most a handful of stamps per instance this is negligible next to drawing thousands of instances at all.
- **Random placement:** randomize the transform per instance on the CPU. Nothing about the mesh changes.
- **Instancing:** this needs instancing anyway. `World` moves from the constant buffer into `Instances[SV_InstanceID]`, and the vertex shader passes the instance id (as `nointerpolation`) and the object-space position and normal to the pixel shader.
- **No format change:** the CKT format stays textureless. The logo textures are separate assets.
- **Drawback:** stamps are tied to one object. A decal that spans several objects (a scorch mark on the ground and a container) needs technique 2.

### 2. Clustered forward decals (world space)
This generalizes technique 1 into a world-space system. Decal boxes are binned into a screen-space or froxel grid on the CPU or in a compute shader. The forward pixel shader looks up its cell and loops over the decals in it. No depth buffer is read. id Software used this in DOOM (2016), with all decals in one atlas (Tiago Sousa, "The devil is in the details", SIGGRAPH 2016).

- **Pros:** decals are independent of objects, the cost per pixel is bounded by cell occupancy, and the same grid can serve clustered lights later.
- **Cons:** you need the clustering infrastructure, and decals on moving objects must be re-binned every frame. That's overkill if stamps always belong to one object.
- **When to use it:** once lights and world decals (dirt, oil, tire marks) show up, put both in the same grid.

### 3. Deferred box decals (the "projector" I was worried about)
Draw a box per decal. The pixel shader reads depth, reconstructs the position, transforms it into the box and blends into the G-buffer or color buffer.

- **Cost:** it's cheaper than it sounds. One depth read per covered pixel, and thousands of boxes can be drawn with instancing.
- **Cons:** you need a depth prepass or a copy of the depth buffer, since you can't sample and bind the same depth target. In a forward renderer there's no normal buffer for the facing test, so you reconstruct normals from depth, which gives ugly edges on corrugation. You also fight depth precision at grazing angles, and decals on moving objects must be moved along with them.
- **Verdict:** for stamps owned by an object, technique 1 does the same thing with fewer passes and none of these problems.

### 4. Decal meshes (clipped geometry)
Clip the mesh triangles against the stamp box (on the CPU, at build time or at spawn time) to get a small mesh with UVs. Render it after the object with a depth bias or a small offset along the normal.

- **Pros:** zero cost on pixels that have no stamp, and it works with any shader.
- **Cons:** extra geometry per stamp, z-fighting that needs tuning, and per-instance meshes when placement is random. A good fit for fixed stamp positions: one decal mesh per model type, instanced, with the logo picked per instance.

### 5. Authored decal regions with UVs
In Blender, mark the faces that can receive stamps (say the container sides) and give only those a "project from view" UV. Add an optional second vertex stream to CKT with those UVs plus a flag. At runtime each instance supplies a UV-space rect for the logo inside that region.

- **Pros:** artists control exactly where stamps can go, and the shader does no projection math.
- **Cons:** it changes the file format, adds vertex data for all vertices, and puts texturing back into the pipeline in a limited form. Technique 1 with authored stamp slots (see below) gets the same artist control without UVs.

### 6. Text as string stamps with an (M)SDF glyph atlas
Container ID codes like `MSKU 123456 7` are random per instance and stenciled in a monospace font. Treat a text label as a stamp whose UV picks a character cell instead of a logo:

```hlsl
// snip: inside the stamp loop, for a stamp flagged as text
uint charIndex = (uint)(uv.x * s.length);
uint glyph = Characters[s.textOffset + charIndex];
float2 glyphUv = float2(frac(uv.x * s.length), uv.y);
float distance = GlyphAtlas.SampleGrad(LinearClamp, float3(glyphUv, glyph), uvdx * s.length, uvdy).r;
float alpha = saturate((distance - 0.5f) / fwidth(distance) + 0.5f);
```

- A (multi-channel) signed distance field atlas stays sharp from up close to far away, with small textures and no mip trouble. Logos with crisp vector shapes can use MSDF as well.
- A monospace font keeps the lookup trivial: no kerning, no shaping, no HarfBuzz.
- The SLUG note (`SLUG Text Rendering.md`) is the high-quality alternative for arbitrary fonts, but it's much heavier than stenciled labels need.

### 7. Analytic SDF stamps
Very simple marks (stripes, warning bands, circles, arrows, hazard diamonds) can be evaluated directly in the shader from a shape id and a few parameters. There's no texture at all, and it's perfectly sharp. This slots in as another stamp kind next to logos and text in technique 1.

## Comparison

| Technique | Depth needed | Extra passes | Random placement | Follows corrugation | Complexity |
|---|---|---|---|---|---|
| 1. Object-space projected stamps | No | No | Trivial | Yes | Low |
| 2. Clustered forward decals | No | Binning | Trivial | Yes | Medium-high |
| 3. Deferred box decals | Yes | Yes | Trivial | Yes, normals are poor | Medium |
| 4. Decal meshes | No | Extra draw | Costly (re-clip) | Yes | Medium |
| 5. Authored UV regions | No | No | Within region | Yes | Medium (format change) |
| 6. String stamps (SDF) | No | No | Trivial | Yes | Low on top of 1 |
| 7. Analytic SDF stamps | No | No | Trivial | Yes | Low on top of 1 |

## Suggested path
1. Add instancing to the model rendering: an `Instances` structured buffer and `DrawIndexedInstanced`.
2. Implement technique 1 with a `Texture2DArray` of logos, all the same size, so mips are simple.
3. Add *stamp slots* to the Blender exporter: empties named with a prefix like `Stamp.` give an oriented box in object space (location, rotation, scale). The CKT file stores them per model. At spawn time the game picks a slot, a logo and an optional random offset or scale within the slot. Artists control where stamps can go, and the runtime controls what goes there.
4. Add text stamps (technique 6) with a monospace MSDF font for ID codes.
5. Move to technique 2 only when world-space decals or clustered lights are needed.

## Gotchas
- **Derivatives in divergent code:** `ddx`/`ddy` and implicit-LOD `Sample` inside the stamp loop are undefined when pixels in a quad take different branches. Take `ddx(objectPos)` outside the loop and use `SampleGrad` (shown above).
- **Facing test:** without it, a box that goes through the container also stamps the opposite wall, mirrored. Use the interpolated normal. Set the threshold low enough that the corrugation flanks still receive paint.
- **Box depth:** keep the stamp box thin along its projection axis, about the corrugation depth plus a margin, so it doesn't catch internal or nearby geometry.
- **Normal space:** the facing test needs the normal in object space. Pass it along before the world transform is applied.
- **LOD:** at far LODs, set `stampCount` to 0 for the instance, or let the mips fade the logos out.
- **Base color only:** the stamp changes base color, and optionally roughness (paint is often less rough than weathered steel). Lighting stays unchanged, which is what keeps it consistent with the per-triangle materials.
