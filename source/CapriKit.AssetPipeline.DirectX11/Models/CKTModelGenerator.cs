using System.Numerics;
using Vortice.Mathematics;

namespace CapriKit.AssetPipeline.DirectX11.Models;

public static class CKTModelGenerator
{
    private static Color3 ToLinearColor(byte byteR, byte byteG, byte byteB)
    {
        const float gamma = 2.2f;
        var r = MathF.Pow(byteR / (float)byte.MaxValue, gamma);
        var g = MathF.Pow(byteG / (float)byte.MaxValue, gamma);
        var b = MathF.Pow(byteB / (float)byte.MaxValue, gamma);
        return new Color3(r, g, b);
    }

    public static CKTModelData CreateUnitCube()
    {
        var forward = new Vector3(0, 0, -1);
        var backward = new Vector3(0, 0, 1);
        var up = new Vector3(0, 1, 0);
        var down = new Vector3(0, -1, 0);
        var left = new Vector3(-1, 0, 0);
        var right = new Vector3(1, 0, 0);

        var vertices = new CKTVertex[]
        {
            // Front face starting from upper left
            new(new Vector3(-1, 1, -1) * 0.5f, forward),
            new(new Vector3(1, 1, -1) * 0.5f, forward),
            new(new Vector3(1, -1, -1) * 0.5f, forward),
            new(new Vector3(-1, -1, -1) * 0.5f, forward),

            // Left face starting from its upper left
            new(new Vector3(-1, 1, 1) * 0.5f, left),
            new(new Vector3(-1, 1, -1) * 0.5f, left),
            new(new Vector3(-1, -1, -1) * 0.5f, left),
            new(new Vector3(-1, -1, 1) * 0.5f, left),

            // Back face starting from its upper left
            new(new Vector3(1, 1, 1) * 0.5f, backward),
            new(new Vector3(-1, 1, 1) * 0.5f, backward),
            new(new Vector3(-1, -1, 1) * 0.5f, backward),
            new(new Vector3(1, -1, 1) * 0.5f, backward),

            // right face starting from its upper left
            new(new Vector3(1, 1, -1) * 0.5f, right),
            new(new Vector3(1, 1, 1) * 0.5f, right),
            new(new Vector3(1, -1, 1) * 0.5f, right),
            new(new Vector3(1, -1, -1) * 0.5f, right),

            // top face starting from its upper left
            new(new Vector3(-1, 1, 1) * 0.5f, up),
            new(new Vector3(1, 1, 1) * 0.5f, up),
            new(new Vector3(1, 1, -1) * 0.5f, up),
            new(new Vector3(-1, 1, -1) * 0.5f, up),

            // bottom face starting from its upper left
            new(new Vector3(-1, -1, -1) * 0.5f, down),
            new(new Vector3(1, -1, -1) * 0.5f, down),
            new(new Vector3(1, -1, 1) * 0.5f, down),
            new(new Vector3(-1, -1, 1) * 0.5f, down),
        };

        var indices = new uint[6 * 6];
        var triangles = new CKTTriangle[6 * 2];
        for (var face = 0u; face < 6u; face++)
        {
            var t = face * 2u;
            var i = face * 6u;
            var v = face * 4u;

            indices[i + 0] = 0 + v;
            indices[i + 1] = 1 + v;
            indices[i + 2] = 2 + v;

            indices[i + 3] = 2 + v;
            indices[i + 4] = 3 + v;
            indices[i + 5] = 0 + v;

            triangles[t + 0] = new CKTTriangle(face);
            triangles[t + 1] = new CKTTriangle(face);
        }

        var materials = new CKTMaterial[]
        {
            new(ToLinearColor(255, 28, 11), 0.0f, 1.0f, new Color3(0, 0, 0), 0.0f), // red
            new(ToLinearColor(191, 18, 195), 0.0f, 1.0f, new Color3(0, 0, 0), 0.0f), // purple
            new(ToLinearColor(7, 7, 255), 0.0f, 1.0f, new Color3(0, 0, 0), 0.0f), // blue
            new(ToLinearColor(35, 216, 1), 0.0f, 1.0f, new Color3(0, 0, 0), 0.0f), // green
            new(ToLinearColor(253, 255, 22), 0.0f, 1.0f, new Color3(0, 0, 0), 0.0f), // yellow
            new(ToLinearColor(255, 140, 27), 0.0f, 1.0f, new Color3(0, 0, 0), 0.0f), // orange
        };

        var meshes = new CKTMesh[]
        {
            new(0, vertices.Length, 0, indices.Length, 0, triangles.Length, new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f))
        };

        var header = new CKTHeader(FileTypeIdentifier.Create(), 1, "UnitCube", materials.Length, meshes.Length, vertices.Length, indices.Length, triangles.Length);

        return new CKTModelData(header, meshes, materials, vertices, indices, triangles);
    }
}
