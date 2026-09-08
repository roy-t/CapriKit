using CapriKit.DirectX11;
using CapriKit.Tests.TestUtilities;
using Microsoft.Extensions.Logging.Abstractions;

namespace CapriKit.Tests.DirectX11;

// While you can have multiple ID3D11Device in your application, you can only have one
// IDXGIDebug (all instances point to the same object) so leak detection via ReportLiveObjects fails
// if we run tests that use a DirectX device in parallel
[NotInParallel("DirectX")]
internal class DeviceTests
{
    /// <summary>
    /// General test to figure out if DirectX works in the testing environment
    /// </summary>
    [Test]
    public async Task CanCreate()
    {
        using var device = new Device(NullLoggerFactory.Instance);
        await Assert.That(device).IsNotNull();
    }

    /// <summary>
    /// The debug layer collects its messages in a queue that is destroyed together with the device rather
    /// than writing them out itself, so nothing is reported unless the device empties that queue.
    /// </summary>
    [Test]
    public async Task Update_ReportsDebugLayerMessagesToTheLogger()
    {
        var loggerFactory = new CapturingLoggerFactory();
        using var device = new Device(loggerFactory);

        device.LogMessages();

        // Creating a device also creates the sampler, blend, depth stencil and rasterizer states it owns,
        // each of which the debug layer reports as it is created
        await Assert.That(loggerFactory.Messages).IsNotEmpty();
        await Assert.That(loggerFactory.Messages.Any(message => message.Contains("Create ID3D11"))).IsTrue();
    }
}
