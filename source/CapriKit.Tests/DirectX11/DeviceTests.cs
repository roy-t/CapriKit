using CapriKit.DirectX11;

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
        using var device = new Device();
        await Assert.That(device).IsNotNull();
    }
}
