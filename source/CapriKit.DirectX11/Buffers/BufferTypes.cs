using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

// Marker interfaces to be able to add extension methods to device buffers. Buffers have different traits
// so adding them via extension methods is easier than via a convoluted type hierarchy.

/// <summary>
/// A buffer that lives on the GPU device
/// </summary>
public interface IDeviceBuffer<T>
    where T : unmanaged
{
    int Capacity { get; }

    int Length { get; }

    string Name { get; }

    void EnsureCapacity(int primitiveCount, int reserveExtra = 0);

    internal ID3D11Buffer? ID3D11Buffer { get; }
}

/// <summary>
/// A buffer that the CPU can write to.
/// </summary>
public interface ICpuWriteToBuffer<T> : IDeviceBuffer<T>
    where T : unmanaged
{ }

/// <summary>
/// A buffer that the CPU can read from.
/// </summary>
public interface ICpuReadFromBuffer<T> : IDeviceBuffer<T>
        where T : unmanaged
{ }

/// <summary>
/// A buffer that the shader can read from.
/// </summary>
public interface IShaderReadFromBuffer<T> : IDeviceBuffer<T>
    where T : unmanaged
{ }

/// <summary>
/// A buffer that the shader can write to.
/// </summary>
public interface IShaderWriteToBuffer<T> : IDeviceBuffer<T>
    where T : unmanaged
{ }
