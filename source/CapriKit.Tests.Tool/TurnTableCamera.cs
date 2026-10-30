using CapriKit.Win32.Input;
using System.Numerics;

namespace CapriKit.Tests.Tool;

internal sealed class TurnTableCamera(Mouse mouse)
{
    private const float MovementSpeed = 0.05f;
    private const float ZoomSpeed = 0.075f;
    private const float MaxPitch = (MathF.PI / 2.0f) - 0.01f;
    private const float MinDistance = 0.1f;

    private float distance = 5.0f;
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    public void Update()
    {
        if (mouse.Held(MouseButton.Right))
        {
            yaw += -mouse.MovementF.X * MovementSpeed;
            pitch = Math.Clamp(pitch + (mouse.MovementF.Y * MovementSpeed), -MaxPitch, MaxPitch);
        }

        if (mouse.Held(MouseButton.Middle))
        {
            yaw = 0.0f;
            pitch = 0.0f;
            distance = 5.0f;
        }

        if (mouse.ScrollSteps != 0)
        {
            distance = Math.Max(MinDistance, distance + mouse.ScrollSteps * ZoomSpeed);
        }
    }

    public Matrix4x4 GetViewMatrix()
    {
        var magnitude = MathF.Cos(pitch) * distance;
        var x = MathF.Sin(yaw) * magnitude;
        var y = MathF.Sin(pitch) * distance;
        var z = MathF.Cos(yaw) * magnitude;

        return Matrix4x4.CreateLookAt(new Vector3(x, y, z), Vector3.Zero, Vector3.UnitY);
    }
}
