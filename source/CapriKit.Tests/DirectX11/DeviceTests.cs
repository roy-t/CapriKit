using CapriKit.DirectX11;
using System;
using System.Collections.Generic;
using System.Text;

namespace CapriKit.Tests.DirectX11;

internal class DeviceTests
{
    [Test]
    public async Task CanCreate()
    {
        using var device = new Device();
        await Assert.That(device).IsNotNull();
    }
}
