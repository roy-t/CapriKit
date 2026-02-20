using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Contexts.States;
using CapriKit.DirectX11.Debug;
using CapriKit.Win32;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
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
    private readonly ID3D11DeviceContext ID3D11DeviceContext;

    internal readonly ID3D11Device ID3D11Device;
    internal ID3D11Texture2D BackBuffer;
    internal ID3D11RenderTargetView BackBufferView;

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

        var deviceResult = D3D11CreateDevice(null, DriverType.Hardware, Flags, [FeatureLevel.Level_11_1], out var device, out _, out var context);
        deviceResult.CheckError();

#if DEBUG // Setup error checking as early as possible
        IDXGIDebug = DXGIGetDebugInterface1<IDXGIDebug>();
        IDXGIInfoQueue = DXGIGetDebugInterface1<IDXGIInfoQueue>();
        IDXGIInfoQueue.PushEmptyStorageFilter(DebugAll);
        IDXGIInfoQueue.SetBreakOnSeverity(DebugAll, InfoQueueMessageSeverity.Warning, true);
        IDXGIInfoQueue.SetBreakOnSeverity(DebugAll, InfoQueueMessageSeverity.Error, true);
        IDXGIInfoQueue.SetBreakOnSeverity(DebugAll, InfoQueueMessageSeverity.Corruption, true);
        InfoQueueSubscription = new InfoQueueSubscription(IDXGIInfoQueue);
#endif

        AllowTearing = SupportsTearingDuringPresent(device);
        var swapChainDescription = CreateSwapChainDescription(window.Width, window.Height, AllowTearing);

        ID3D11Device = device ?? throw new Exception($"Failed to create {nameof(ID3D11Device)}");
        ID3D11DeviceContext = context ?? throw new Exception($"Failed to create {nameof(IDXGISwapChain)}");
        IDXGISwapChain = CreateSwapChain(device, swapChainDescription, window.Handle);

        ImmediateDeviceContext = new ImmediateDeviceContext(this, ID3D11DeviceContext);

        SamplerStates = new SamplerStates(device);
        BlendStates = new BlendStates(device);
        DepthStencilStates = new DepthStencilStates(device);
        RasterizerStates = new RasterizerStates(device);

        CreateBackBuffer();
    }

    public Rectangle Viewport { get; private set; }
    public int Width => Viewport.Width;
    public int Height => Viewport.Height;
    public bool VSync { get; set; } = true;

    public bool AllowTearing { get; private set; } = false;

    public SamplerStates SamplerStates { get; }
    public BlendStates BlendStates { get; }
    public DepthStencilStates DepthStencilStates { get; }
    public RasterizerStates RasterizerStates { get; }

    public ImmediateDeviceContext ImmediateDeviceContext { get; }

    public DeferredDeviceContext CreateDeferredContext(string? nameHint = null, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null)
    {
        var context = ID3D11Device.CreateDeferredContext();
        context.DebugName = DebugName.For(context, nameHint, caller, callerFile);
        return new DeferredDeviceContext(this, context);
    }

    public void Clear()
    {
        ID3D11DeviceContext.ClearRenderTargetView(BackBufferView, Colors.CornflowerBlue);
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

    public void Resize(int width, int height)
    {
        Viewport = new Rectangle(0, 0, width, height);
        BackBufferView.Dispose();
        BackBuffer.Dispose();

        var swapChainDescription = CreateSwapChainDescription(width, height, AllowTearing);
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
        BackBufferView.DebugName = DebugName.For(BackBufferView, nameof(BackBufferView));
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

    private static IDXGISwapChain1 CreateSwapChain(ID3D11Device device, SwapChainDescription1 description, nint windowHandle)
    {
        using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
        using var adapter = dxgiDevice.GetParent<IDXGIAdapter>();
        using var factory = adapter.GetParent<IDXGIFactory2>();

        var swapChain = factory.CreateSwapChainForHwnd(device, windowHandle, description);
        factory.MakeWindowAssociation(windowHandle, WindowAssociationFlags.IgnoreAltEnter);

        return swapChain;
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
