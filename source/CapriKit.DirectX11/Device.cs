using CapriKit.DirectX11.Debug;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.DXGI.Debug;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace CapriKit.DirectX11;

public sealed class Device : IDisposable
{
    private const Format BackBufferFormat = Format.R8G8B8A8_UNorm;
    private const Format RenderTargetViewFormat = Format.R8G8B8A8_UNorm_SRgb;

    private readonly nint WindowHandle;

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

    public Device(nint windowHandle, int width, int height)
    {
        this.WindowHandle = windowHandle;
        this.Viewport = new Rectangle(0, 0, width, height);

        var swapChainDescription = CreateSwapChainDescription(windowHandle, width, height);
        var result = D3D11CreateDeviceAndSwapChain(null, DriverType.Hardware, Flags, [FeatureLevel.Level_11_1], swapChainDescription,
            out var swapChain, out var device, out var _, out var context);
        result.CheckError();

        IDXGISwapChain = swapChain ?? throw new Exception($"Failed to create {nameof(IDXGISwapChain)}");
        ID3D11Device = device ?? throw new Exception($"Failed to create {nameof(ID3D11Device)}");
        ID3D11DeviceContext = context ?? throw new Exception($"Failed to create {nameof(IDXGISwapChain)}");

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
    public int Width => this.Viewport.Width;
    public int Height => this.Viewport.Height;
    public bool VSync { get; set; } = true;

    public void Present()
    {
        if (VSync)
        {
            IDXGISwapChain.Present(1, PresentFlags.None);
        }
        else
        {
            IDXGISwapChain.Present(1, PresentFlags.AllowTearing);
        }
    }

    public void Resize(int width, int height)
    {
        Viewport = new Rectangle(0, 0, width, height);
        BackBufferView.Dispose();
        BackBuffer.Dispose();

        var swapChainDescription = CreateSwapChainDescription(WindowHandle, width, height);
        var result = IDXGISwapChain.ResizeBuffers(swapChainDescription.BufferCount,
            swapChainDescription.BufferDescription.Width,
            swapChainDescription.BufferDescription.Height,
            swapChainDescription.BufferDescription.Format,
            swapChainDescription.Flags);

        result.CheckError();

        CreateBackBuffer();
    }

    private static SwapChainDescription CreateSwapChainDescription(nint windowHandle, int width, int height)
    {
        return new SwapChainDescription()
        {
            BufferCount = 2,
            BufferDescription = new ModeDescription((uint)width, (uint)height, BackBufferFormat),
            BufferUsage = Usage.RenderTargetOutput,
            Flags = SwapChainFlags.AllowTearing,
            OutputWindow = windowHandle,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.FlipDiscard // for v-sync off and GSync support
        };
    }

    [MemberNotNull(nameof(BackBuffer), nameof(BackBufferView))]
    private void CreateBackBuffer()
    {
        BackBuffer = IDXGISwapChain.GetBuffer<ID3D11Texture2D1>(0);

        // Explicitly set the RTV to a format with SRGB while the actual backbuffer is a format without SRGB to properly
        // let the output window be gamma corrected. See: https://docs.microsoft.com/en-us/windows/win32/direct3ddxgi/converting-data-color-space
        var view = new RenderTargetViewDescription(BackBuffer, RenderTargetViewDimension.Texture2D, RenderTargetViewFormat);
        BackBufferView = this.ID3D11Device.CreateRenderTargetView(this.BackBuffer, view);
        BackBufferView.DebugName = DebugName.For(BackBufferView.GetType(), "BackBuffer_View");
    }

    public void Dispose()
    {
        BackBufferView.Dispose();
        BackBuffer.Dispose();

        ID3D11DeviceContext.Dispose();
        ID3D11Device.Dispose();
        IDXGISwapChain.Dispose();

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

#endif
    }
}
