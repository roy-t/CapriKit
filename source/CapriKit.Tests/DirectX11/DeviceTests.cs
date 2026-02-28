using CapriKit.DirectX11;

namespace CapriKit.Tests.DirectX11;

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
