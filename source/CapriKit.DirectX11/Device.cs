using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Contexts.States;
using CapriKit.DirectX11.Debug;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.DXGI.Debug;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace CapriKit.DirectX11;

public class Device : IDisposable
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

    /// <summary>
    /// Creates the graphics device. In a debug build the DirectX debug layer is enabled and everything it
    /// reports is written to <paramref name="loggerFactory"/>, so pass the same factory as the rest of the
    /// application uses to see those messages alongside your own.
    /// </summary>
    public Device(ILoggerFactory loggerFactory)
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

        // Without this the debug layer also writes every message straight to the native debug output, where
        // it bypasses the logger and is only visible to a debugger that is attached at that moment.
        IDXGIInfoQueue.SetMuteDebugOutput(DebugAll, true);

        InfoQueueSubscription = new InfoQueueSubscription(loggerFactory, IDXGIInfoQueue);
#endif

        ID3D11Device = device ?? throw new Exception($"Failed to create {nameof(ID3D11Device)}");
        ID3D11DeviceContext = context ?? throw new Exception($"Failed to create {nameof(IDXGISwapChain)}");

        ImmediateDeviceContext = new ImmediateDeviceContext(this, ID3D11DeviceContext);

        SamplerStates = new SamplerStates(device);
        BlendStates = new BlendStates(device);
        DepthStencilStates = new DepthStencilStates(device);
        RasterizerStates = new RasterizerStates(device);
    }

    internal SamplerStates SamplerStates { get; }
    internal BlendStates BlendStates { get; }
    internal DepthStencilStates DepthStencilStates { get; }
    internal RasterizerStates RasterizerStates { get; }

    /// <summary>
    /// Gets the immediate device context, of which there is only one and which is NOT thread safe.
    /// Prefer creating a deferred context and only use the immediate device context for final rendering.
    /// </summary>
    public ImmediateDeviceContext ImmediateDeviceContext { get; }

    /// <summary>
    /// Creates a deferred rendering context to be used by a single system. Note that the deferred rendering
    /// context is not thread safe, instead each thread can create and own their own context, which still
    /// allows you to do multi-threaded rendering
    /// </summary>
    public DeferredDeviceContext CreateDeferredContext(string? nameHint = null, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null)
    {
        var context = ID3D11Device.CreateDeferredContext();
        context.DebugName = DebugName.For(context, nameHint, caller, callerFile);
        return new DeferredDeviceContext(this, context);
    }

    /// <summary>
    /// Logs DirectX debug layer log messages and throws on any message with severity warning or higher.
    /// The <seealso cref="SwapChain"/> calls this method automatically after Present. You 
    /// can call this method explicitly in cases where that does not suffice.
    /// </summary>
    public void LogMessages()
    {
#if DEBUG
        InfoQueueSubscription.LogMessages();
#endif
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
        InfoQueueSubscription.LogMessages();

        IDXGIInfoQueue.Dispose();
        IDXGIDebug.Dispose();
#endif
    }
}
