using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Contexts.States;
using CapriKit.DirectX11.Debug;
using System.Runtime.CompilerServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.DXGI.Debug;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace CapriKit.DirectX11;

public class HeadlessDevice : IDisposable
{
    private readonly ID3D11DeviceContext ID3D11DeviceContext;

    internal readonly ID3D11Device ID3D11Device;    

#if DEBUG
    private static readonly DeviceCreationFlags Flags = DeviceCreationFlags.Debug;
    private readonly IDXGIDebug IDXGIDebug;
    private readonly IDXGIInfoQueue IDXGIInfoQueue;
    private readonly InfoQueueSubscription InfoQueueSubscription;
#else
    private static readonly DeviceCreationFlags Flags = DeviceCreationFlags.None;
#endif

    public HeadlessDevice()
    {        
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
        
        ID3D11Device = device ?? throw new Exception($"Failed to create {nameof(ID3D11Device)}");
        ID3D11DeviceContext = context ?? throw new Exception($"Failed to create {nameof(IDXGISwapChain)}");
        
        ImmediateDeviceContext = new ImmediateDeviceContext(this, ID3D11DeviceContext);

        SamplerStates = new SamplerStates(device);
        BlendStates = new BlendStates(device);
        DepthStencilStates = new DepthStencilStates(device);
        RasterizerStates = new RasterizerStates(device);        
    }

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

    public virtual void Dispose()
    {
        // Call clear state before dispose to unbind resources
        // Call flush to fore the GPU to update state immediately
        ID3D11DeviceContext.ClearState();
        ID3D11DeviceContext.Flush();

        BlendStates.Dispose();
        DepthStencilStates.Dispose();
        RasterizerStates.Dispose();
        SamplerStates.Dispose();

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
#endif
    }
}
