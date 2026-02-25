using CapriKit.DirectX11.Debug;
using CapriKit.Win32;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace CapriKit.DirectX11;

public sealed class SwapChain : IDisposable
{
    // Explicitly set the RTV to a format with SRGB while the actual backbuffer is a format without SRGB to properly
    // let the output window be gamma corrected. See: https://docs.microsoft.com/en-us/windows/win32/direct3ddxgi/converting-data-color-space
    private const Format BackBufferFormat = Format.R8G8B8A8_UNorm;
    private const Format RenderTargetViewFormat = Format.R8G8B8A8_UNorm_SRgb;

    internal readonly IDXGISwapChain IDXGISwapChain;
    internal ID3D11RenderTargetView BackBufferView;

    public SwapChain(HeadlessDevice device, Win32Window window)
    {
        Viewport = new Rectangle(0, 0, window.Width, window.Height);

        AllowTearing = SupportsTearingDuringPresent(device.ID3D11Device);
        var swapChainDescription = CreateSwapChainDescription(window.Width, window.Height, AllowTearing);
        IDXGISwapChain = CreateSwapChain(device.ID3D11Device, swapChainDescription, window.Handle);

        CreateBackBuffer(device);
    }

    public Rectangle Viewport { get; private set; }
    public int Width => Viewport.Width;
    public int Height => Viewport.Height;
    public bool VSync { get; set; } = true;
    public bool AllowTearing { get; private set; } = false;

    public void Clear(HeadlessDevice device)
    {
        device.ImmediateDeviceContext.ID3D11DeviceContext.ClearRenderTargetView(BackBufferView, Colors.CornflowerBlue);
    }

    public void Resize(HeadlessDevice device, int width, int height)
    {
        Viewport = new Rectangle(0, 0, width, height);
        BackBufferView.Dispose();

        var swapChainDescription = CreateSwapChainDescription(width, height, AllowTearing);
        var result = IDXGISwapChain.ResizeBuffers(swapChainDescription.BufferCount,
            swapChainDescription.Width,
            swapChainDescription.Height,
            swapChainDescription.Format,
            swapChainDescription.Flags);

        result.CheckError();

        CreateBackBuffer(device);
    }

    public void Present()
    {
        if (!VSync && AllowTearing)
        {
            IDXGISwapChain.Present(0, PresentFlags.AllowTearing);
        }
        else
        {
            IDXGISwapChain.Present(1, PresentFlags.None);
        }
    }

    [MemberNotNull(nameof(BackBufferView))]
    private void CreateBackBuffer(HeadlessDevice device)
    {
        using var backBuffer = IDXGISwapChain.GetBuffer<ID3D11Texture2D1>(0);
        var view = new RenderTargetViewDescription(backBuffer, RenderTargetViewDimension.Texture2D, RenderTargetViewFormat);
        BackBufferView = device.ID3D11Device.CreateRenderTargetView(backBuffer, view);
        BackBufferView.DebugName = DebugName.For(BackBufferView, nameof(BackBufferView));
    }

    private static IDXGISwapChain1 CreateSwapChain(ID3D11Device device, SwapChainDescription1 description, nint windowHandle)
    {
        using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
        using var adapter = dxgiDevice.GetParent<IDXGIAdapter>();
        using var factory = adapter.GetParent<IDXGIFactory2>();

        var swapChain = factory.CreateSwapChainForHwnd(device, windowHandle, description);
        factory.MakeWindowAssociation(windowHandle, WindowAssociationFlags.IgnoreAltEnter);

        return swapChain;
    }

    private static SwapChainDescription1 CreateSwapChainDescription(int width, int height, bool allowTearing)
    {
        return new SwapChainDescription1()
        {
            AlphaMode = AlphaMode.Unspecified,
            BufferCount = 2,
            BufferUsage = Usage.RenderTargetOutput,
            Flags = allowTearing ? SwapChainFlags.AllowTearing : SwapChainFlags.None,
            Format = BackBufferFormat,
            Height = (uint)height,
            SampleDescription = new SampleDescription(1, 0),
            Scaling = Scaling.None,
            Stereo = false,
            SwapEffect = SwapEffect.FlipDiscard, // for v-sync off and GSync support
            Width = (uint)width,
        };
    }

    private static bool SupportsTearingDuringPresent(ID3D11Device device)
    {
        // Tearing support requires DXGI 1.5 which was added in Windows 10 Anniverary edition
        using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
        using var adapter = dxgiDevice.GetParent<IDXGIAdapter>();
        using var factory5 = adapter.GetParent<IDXGIFactory5>();
        return factory5 != null && factory5.PresentAllowTearing;
    }

    public void Dispose()
    {
        BackBufferView.Dispose();
        IDXGISwapChain.Dispose();
    }
}
