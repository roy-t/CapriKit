using CapriKit.DirectX11.Contexts;

namespace CapriKit.Tests.Tool.Tests.Framework;

internal interface ITestScreen : IDisposable
{
    public string Title { get; }

    public void Render(DeviceContext context);
}
