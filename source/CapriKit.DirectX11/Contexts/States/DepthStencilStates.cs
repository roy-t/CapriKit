using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts.States;

public sealed class DepthStencilState : IDisposable
{
    internal DepthStencilState(ID3D11DepthStencilState state, string nameHint)
    {
        Name = DebugName.For(this, nameHint);
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

    private static DepthStencilState Create(ID3D11Device device, DepthStencilDescription description, string name)
    {
        var state = device.CreateDepthStencilState(description);
        return new DepthStencilState(state, name);
    }

    public void Dispose()
    {
        None.Dispose();
        Default.Dispose();
        ReverseZ.Dispose();
        ReverseZReadOnly.Dispose();
    }
}
