using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Debug;
using Microsoft.Extensions.Logging.Abstractions;
using System.Numerics;

namespace CapriKit.Tests.DirectX11.Debug;

// Debug names are empty in release builds
#if DEBUG
internal class DebugNameTests
{
    [Test]
    public async Task For()
    {
        var name = DebugName.For<List<int>>("label", "Caller", @"C:\source\File.cs", 42);

        await Assert.That(name).IsEqualTo("List<Int32>:label@File.Caller:42");
    }

    [Test]
    public async Task For_Constructor()
    {
        var name = DebugName.For<int>(null, ".ctor", @"C:\source\File.cs", 42);

        await Assert.That(name).IsEqualTo("Int32@File:42");
    }

    [Test]
    [NotInParallel("DirectX")]
    public async Task For_ForwardedByResources()
    {
        using var device = new Device(NullLoggerFactory.Instance);

        using var buffer = new ConstantBuffer<Vector4>(device);

        await Assert.That(buffer.Name).StartsWith($"ConstantBuffer<Vector4>@{nameof(DebugNameTests)}.{nameof(For_ForwardedByResources)}:");
    }
}
#endif
