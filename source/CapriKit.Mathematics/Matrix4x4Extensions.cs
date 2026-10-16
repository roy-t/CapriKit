using System.Numerics;

namespace CapriKit.Mathematics;

public static class Matrix4x4Extensions
{
    extension(Matrix4x4)
    {
        /// <summary>
        /// Maps the Z element from [0..1] to [1..0]
        /// </summary>
        public static Matrix4x4 ReverseZ => new( 1,  0,  0,  0,
                                                 0,  1,  0,  0,
                                                 0,  0, -1,  0,
                                                 0,  0,  1,  1);

        /// <summary>
        /// Creates a perspective projection matrix with an infinite far plane that maps the near plane to depth 1 and the far plane to depth 0.
        /// Use with a depth buffer cleared to 0 and a depth stencil set set to <c>Greater</c>.
        /// </summary>
        public static Matrix4x4 CreatePerspectiveFieldOfViewReverseZ(float fieldOfView, float aspectRatio, float nearPlaneDistance)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fieldOfView, 0.0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fieldOfView, MathF.PI);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(nearPlaneDistance, 0.0f);

            var yScale = 1.0f / MathF.Tan(fieldOfView * 0.5f);
            var xScale = yScale / aspectRatio;

            // The limit of the finite version for far -> infinity, depth becomes near / distance
            return new Matrix4x4(xScale, 0, 0, 0,
                                 0, yScale, 0, 0,
                                 0, 0, 0, -1,
                                 0, 0, nearPlaneDistance, 0);
        }
    }
}
