using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using Microsoft.Extensions.Logging.Abstractions;
using System.Numerics;

namespace CapriKit.Tests.DirectX11.Buffers;

/// <summary>
/// Each buffer type builds its native ID3D11Buffer from a BufferDescription that DirectX only validates
/// when the buffer is actually created. These tests force that creation for every buffer type so that an
/// invalid combination of usage, bind flags and CPU access flags fails here instead of at runtime.
/// </summary>
[NotInParallel("DirectX")]
internal class BufferCreationTests
{
    /// <summary>A constant buffer is only valid if its size is a multiple of 16 bytes.</summary>
    private record struct Constants(Vector4 Tint);

    private record struct Vertex(Vector3 Position, Vector3 Normal);

    private static readonly Vertex[] Vertices =
    [
        new(new Vector3(0, 0, 0), Vector3.UnitY),
        new(new Vector3(1, 0, 0), Vector3.UnitY),
        new(new Vector3(0, 0, 1), Vector3.UnitY),
    ];

    [Test]
    public async Task ConstantBuffer_Create()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        // The constructor sizes the buffer for exactly one T
        using var buffer = new ConstantBuffer<Constants>(device, nameof(ConstantBuffer_Create));

        await AssertCreated(device, buffer, 1);
    }

    [Test]
    public async Task StructuredBuffer_Create()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        using var buffer = new StructuredBuffer<Vertex>(device, nameof(StructuredBuffer_Create));
        buffer.EnsureCapacity(3);

        await AssertCreated(device, buffer, 3);
    }

    [Test]
    public async Task RWStructuredBuffer_Create()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        using var buffer = new RWStructuredBuffer<float>(device, nameof(RWStructuredBuffer_Create));
        buffer.EnsureCapacity(3);

        await AssertCreated(device, buffer, 3);
    }

    [Test]
    public async Task StagingBuffer_Create()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        using var buffer = new StagingBuffer<float>(device, nameof(StagingBuffer_Create));
        buffer.EnsureCapacity(3);

        await AssertCreated(device, buffer, 3);
    }

    [Test]
    public async Task VertexBuffer_Create()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        using var buffer = new VertexBuffer<Vertex>(device, nameof(VertexBuffer_Create));
        buffer.EnsureCapacity(3);

        await AssertCreated(device, buffer, 3);
    }

    [Test]
    public async Task IndexBuffer_CreateU16()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        using var buffer = IndexBuffers.CreateU16(device, nameof(IndexBuffer_CreateU16));
        buffer.EnsureCapacity(3);

        await AssertCreated(device, buffer, 3);
    }

    [Test]
    public async Task IndexBuffer_CreateU32()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        using var buffer = IndexBuffers.CreateU32(device, nameof(IndexBuffer_CreateU32));
        buffer.EnsureCapacity(3);

        await AssertCreated(device, buffer, 3);
    }

    [Test]
    public async Task ImmutableStructuredBuffer_Create()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        // Immutable buffers take their data in the constructor, there is no later write path
        using var buffer = new ImmutableStructuredBuffer<Vertex>(device, Vertices, nameof(ImmutableStructuredBuffer_Create));

        await AssertCreated(device, buffer, Vertices.Length);
    }

    [Test]
    public async Task ImmutableVertexBuffer_Create()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        using var buffer = new ImmutableVertexBuffer<Vertex>(device, Vertices, nameof(ImmutableVertexBuffer_Create));

        await AssertCreated(device, buffer, Vertices.Length);
    }

    [Test]
    public async Task IndexBuffer_CreateU16Immutable()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        using var buffer = IndexBuffers.CreateU16Immutable(device, [0, 1, 2], nameof(IndexBuffer_CreateU16Immutable));

        await AssertCreated(device, buffer, 3);
    }

    [Test]
    public async Task IndexBuffer_CreateU32Immutable()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        using var buffer = IndexBuffers.CreateU32Immutable(device, [0, 1, 2], nameof(IndexBuffer_CreateU32Immutable));

        await AssertCreated(device, buffer, 3);
    }

    /// <summary>
    /// Growing and shrinking both recreate the native buffer, so both directions have to produce a
    /// description DirectX still accepts.
    /// </summary>
    [Test]
    public async Task SetCapacity_GrowsAndShrinks()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        using var buffer = new StructuredBuffer<Vertex>(device, nameof(SetCapacity_GrowsAndShrinks));
        buffer.SetCapacity(8);
        await Assert.That(buffer.Capacity).IsEqualTo(8);

        buffer.SetCapacity(2);
        await Assert.That(buffer.Capacity).IsEqualTo(2);

        await AssertCreated(device, buffer, 2);
    }

    /// <summary>
    /// Proves the native buffer exists and that creating it did not make the debug layer complain.
    /// LogMessages throws on every message of severity warning or higher.
    /// </summary>
    private static async Task AssertCreated<T>(Device device, IImmutableDeviceBuffer<T> buffer, int expectedLength)
        where T : unmanaged
    {
        await Assert.That(buffer.ID3D11Buffer).IsNotNull();
        await Assert.That(buffer.Length).IsEqualTo(expectedLength);

        device.LogMessages();
    }
}
