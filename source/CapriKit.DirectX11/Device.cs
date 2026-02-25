using CapriKit.Win32;
using System.Drawing;

namespace CapriKit.DirectX11;

public sealed class Device : HeadlessDevice
{    
    public Device(Win32Window window) : base()
    {
        SwapChain = new SwapChain(this, window);
    }

    public SwapChain SwapChain { get; }

    public Rectangle Viewport => SwapChain.Viewport;
    public int Width => SwapChain.Width;
    public int Height => SwapChain.Height;
    public bool VSync { get => SwapChain.VSync; set => SwapChain.VSync = value; }
    public bool AllowTearing => SwapChain.AllowTearing;

    public void Clear()
    {
        SwapChain.Clear(this);
    }

    public void Present()
    {
        SwapChain.Present();
    }

    public void Resize(int width, int height)
    {
        SwapChain.Resize(this, width, height);
    }

    public override void Dispose()
    {
        SwapChain.Dispose();
        base.Dispose();
    }
}
