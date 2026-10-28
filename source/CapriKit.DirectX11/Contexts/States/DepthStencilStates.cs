using CapriKit.DirectX11.Debug;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts.States;

public sealed class DepthStencilState : IDisposable
{
    internal DepthStencilState(ID3D11DepthStencilState state, string? label, string? caller, string? callerFile, int callerLine)
    {
        Name = DebugName.For(this, label, caller, callerFile, callerLine);
        ID3D11DepthStencilState = state;
        ID3D11DepthStencilState.DebugName = Name;

    }

    public string Name { get; }

    internal ID3D11DepthStencilState ID3D11DepthStencilState { get; }

    public void Dispose()
    {
        ID3D11DepthStencilState.Dispose();
    }
}

public sealed class DepthStencilStates : IDisposable
{
    internal DepthStencilStates(ID3D11Device device)
    {
        None = Create(device, DepthStencilDescription.None, nameof(None));
        Default = Create(device, DepthStencilDescription.Default, nameof(Default));
        ReverseZ = Create(device, DepthStencilDescription.DepthReverseZ, nameof(ReverseZ));
        ReverseZReadOnly = Create(device, DepthStencilDescription.DepthReadReverseZ, nameof(ReverseZReadOnly));
    }

    public DepthStencilState None { get; }
    public DepthStencilState Default { get; }
    public DepthStencilState ReverseZ { get; }
    public DepthStencilState ReverseZReadOnly { get; }

    private static DepthStencilState Create(ID3D11Device device, DepthStencilDescription description, string label, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        var state = device.CreateDepthStencilState(description);
        return new DepthStencilState(state, label, caller, callerFile, callerLine);
    }

    public void Dispose()
    {
        None.Dispose();
        Default.Dispose();
        ReverseZ.Dispose();
        ReverseZReadOnly.Dispose();
    }
}
