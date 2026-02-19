using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// Constant buffers are usually used in shader to read data that changes every frame. For example, the camera position.
/// Note: The structures used in constant buffers must match the packing described here:
/// https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-packing-rules
/// </summary>
public sealed class ConstantBuffer<T> : DeviceBuffer<T>
    where T : unmanaged
{
    public ConstantBuffer(Device device, string? nameHint = null) : base(device)
    {
        Name = DebugName.For(this, nameHint);
        EnsureCapacity(1);
    }

    public override string Name { get; }

    protected override ID3D11Buffer CreateBuffer(uint sizeInBytes)
    {
        var constBufferDesc = new BufferDescription
        {
            Usage = ResourceUsage.Dynamic,
            ByteWidth = sizeInBytes,
            BindFlags = BindFlags.ConstantBuffer,
            CPUAccessFlags = CpuAccessFlags.Write,
            StructureByteStride = PrimitiveSizeInBytes
        };

        return Device.CreateBuffer(constBufferDesc);
    }
}
