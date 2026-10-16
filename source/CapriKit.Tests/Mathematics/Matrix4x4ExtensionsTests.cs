using CapriKit.Mathematics;
using System.Numerics;

namespace CapriKit.Tests.Mathematics;

internal class Matrix4x4ExtensionsTests
{
    [Test]
    public async Task CreatePerspectiveFieldOfViewReverseZ()
    {
        const float near = 0.1f;
        var projection = Matrix4x4.CreatePerspectiveFieldOfViewReverseZ(MathF.PI / 2, 16.0f / 9.0f, near);

        var nearDepth = Depth(projection, near);
        var farDepth = Depth(projection, 1_000_000.0f);

        await Assert.That(nearDepth).IsEqualTo(1.0f).Within(1e-6f);
        await Assert.That(farDepth).IsEqualTo(near / 1_000_000.0f).Within(1e-9f);
    }

    [Test]
    public async Task CreatePerspectiveFieldOfViewReverseZ_MatchesFiniteXY()
    {
        var infinite = Matrix4x4.CreatePerspectiveFieldOfViewReverseZ(MathF.PI / 3, 4.0f / 3.0f, 0.1f);
        var finite = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3, 4.0f / 3.0f, 0.1f, 100.0f);

        await Assert.That(infinite.M11).IsEqualTo(finite.M11);
        await Assert.That(infinite.M22).IsEqualTo(finite.M22);
        await Assert.That(infinite.M34).IsEqualTo(finite.M34);
    }

    // Right-handed: the camera looks down -Z, so a point in front of it has a negative z
    private static float Depth(Matrix4x4 projection, float distance)
    {
        var clip = Vector4.Transform(new Vector4(0.0f, 0.0f, -distance, 1.0f), projection);
        return clip.Z / clip.W;
    }
}
