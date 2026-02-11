using CapriKit.DirectX11.Debug;
using CapriKit.Win32;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.DXGI.Debug;
using Vortice.Mathematics;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace CapriKit.DirectX11;

public sealed class Device : IDisposable
{
    // Explicitly set the RTV to a format with SRGB while the actual backbuffer is a format without SRGB to properly
    // let the output window be gamma corrected. See: https://docs.microsoft.com/en-us/windows/win32/direct3ddxgi/converting-data-color-space
    private const Format BackBufferFormat = Format.R8G8B8A8_UNorm;
    private const Format RenderTargetViewFormat = Format.R8G8B8A8_UNorm_SRgb;

    private readonly IDXGISwapChain IDXGISwapChain;
    private readonly ID3D11Device ID3D11Device;
    private readonly ID3D11DeviceContext ID3D11DeviceContext;

    private ID3D11Texture2D BackBuffer;
    private ID3D11RenderTargetView BackBufferView;


#if DEBUG
    private static readonly DeviceCreationFlags Flags = DeviceCreationFlags.Debug;
    private readonly IDXGIDebug IDXGIDebug;
    private readonly IDXGIInfoQueue IDXGIInfoQueue;
    private readonly InfoQueueSubscription InfoQueueSubscription;
#else
    private static readonly DeviceCreationFlags Flags = DeviceCreationFlags.None;
#endif

    public Device(Win32Window window)
    {
        Viewport = new Rectangle(0, 0, window.Width, window.Height);

        var swapChainDescription = CreateSwapChainDescription(window.Width, window.Height);
        var deviceResult = D3D11CreateDevice(null, DriverType.Hardware, Flags, [FeatureLevel.Level_11_1], out var device, out _, out var context);
        deviceResult.CheckError();

        ID3D11Device = device ?? throw new Exception($"Failed to create {nameof(ID3D11Device)}");
        ID3D11DeviceContext = context ?? throw new Exception($"Failed to create {nameof(IDXGISwapChain)}");
        IDXGISwapChain = CreateSwapChain(device, swapChainDescription, window.Handle);

        CreateBackBuffer();

#if DEBUG
        IDXGIDebug = DXGIGetDebugInterface1<IDXGIDebug>();
        IDXGIInfoQueue = DXGIGetDebugInterface1<IDXGIInfoQueue>();
        IDXGIInfoQueue.PushEmptyStorageFilter(DebugAll);
        IDXGIInfoQueue.SetBreakOnSeverity(DebugAll, InfoQueueMessageSeverity.Warning, true);
        IDXGIInfoQueue.SetBreakOnSeverity(DebugAll, InfoQueueMessageSeverity.Error, true);
        IDXGIInfoQueue.SetBreakOnSeverity(DebugAll, InfoQueueMessageSeverity.Corruption, true);
        InfoQueueSubscription = new InfoQueueSubscription(IDXGIInfoQueue);
#endif
    }

    public Rectangle Viewport { get; private set; }
    public int Width => Viewport.Width;
    public int Height => Viewport.Height;
    public bool VSync { get; set; } = true;

    public void Clear()
    {
        ID3D11DeviceContext.ClearRenderTargetView(BackBufferView, Colors.CornflowerBlue);
    }

    public void Present()
    {
        if (VSync)
        {
            IDXGISwapChain.Present(1, PresentFlags.None);
        }
        else
        {
            IDXGISwapChain.Present(0, PresentFlags.AllowTearing);
        }
    }

    public void Resize(int width, int height)
    {
        Viewport = new Rectangle(0, 0, width, height);
        BackBufferView.Dispose();
        BackBuffer.Dispose();

        var swapChainDescription = CreateSwapChainDescription(width, height);
        var result = IDXGISwapChain.ResizeBuffers(swapChainDescription.BufferCount,
            swapChainDescription.Width,
            swapChainDescription.Height,
            swapChainDescription.Format,
            swapChainDescription.Flags);

        result.CheckError();

        CreateBackBuffer();
    }

    [MemberNotNull(nameof(BackBuffer), nameof(BackBufferView))]
    private void CreateBackBuffer()
    {
        BackBuffer = IDXGISwapChain.GetBuffer<ID3D11Texture2D1>(0);
        
        var view = new RenderTargetViewDescription(BackBuffer, RenderTargetViewDimension.Texture2D, RenderTargetViewFormat);
        BackBufferView = ID3D11Device.CreateRenderTargetView(BackBuffer, view);
        BackBufferView.DebugName = DebugName.For(BackBufferView.GetType(), "BackBuffer_View");
    }
    private static SwapChainDescription1 CreateSwapChainDescription(int width, int height)
    {
        return new SwapChainDescription1()
        {
            AlphaMode = AlphaMode.Unspecified,
            BufferCount = 2,
            BufferUsage = Usage.RenderTargetOutput,
            Flags = SwapChainFlags.AllowTearing,
            Format = BackBufferFormat,
            Height = (uint)height,
            SampleDescription = new SampleDescription(1, 0),
            Scaling = Scaling.None,
            Stereo = false,
            SwapEffect = SwapEffect.FlipDiscard, // for v-sync off and GSync support
            Width = (uint)width,
        };
    }

    private static IDXGISwapChain1 CreateSwapChain(ID3D11Device device, SwapChainDescription1 description, nint windowHandle)
    {
        using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
        using var adapter = dxgiDevice.GetParent<IDXGIAdapter>();

        using var factory = adapter.GetParent<IDXGIFactory2>();
        return factory.CreateSwapChainForHwnd(device, windowHandle, description);
    }

    public void Dispose()
    {
        // Call clear state before dispose to unbind resources
        // Call flush to fore the GPU to update state immediately
        ID3D11DeviceContext.ClearState();
        ID3D11DeviceContext.Flush();

        BackBufferView.Dispose();
        BackBuffer.Dispose();

        IDXGISwapChain.Dispose();

        ID3D11DeviceContext.Dispose();
        ID3D11Device.Dispose();

#if DEBUG
        // Avoid not getting a readout of all left over objects, by breaking on the first finding
        IDXGIInfoQueue.SetBreakOnSeverity(DebugAll, InfoQueueMessageSeverity.Warning, false);
        IDXGIInfoQueue.SetBreakOnSeverity(DebugAll, InfoQueueMessageSeverity.Error, false);
        IDXGIInfoQueue.SetBreakOnSeverity(DebugAll, InfoQueueMessageSeverity.Corruption, false);

        // Report all objects that have not been cleaned up
        IDXGIDebug.ReportLiveObjects(DebugAll, ReportLiveObjectFlags.Detail | ReportLiveObjectFlags.IgnoreInternal);

        // Report any exception messages that have not been shown yet
        InfoQueueSubscription.CheckExceptions();

        IDXGIInfoQueue.Dispose();
        IDXGIDebug.Dispose();
        Console.WriteLine("Bye bye");
#endif
    }
}
