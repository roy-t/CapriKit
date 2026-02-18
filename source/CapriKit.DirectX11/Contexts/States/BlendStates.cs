using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts.States;

public sealed class BlendState : IDisposable
{
    internal BlendState(ID3D11BlendState state, string nameHint)
    {
        Name = DebugName.For(this, nameHint);
        ID3D11BlendState = state;
        ID3D11BlendState.DebugName = Name;
    }

    public string Name { get; }

    internal ID3D11BlendState ID3D11BlendState { get; }

    public void Dispose()
    {
        ID3D11BlendState.Dispose();
    }
}

public sealed class BlendStates : IDisposable
{
    internal BlendStates(ID3D11Device device)
    {
        NonPreMultiplied = Create(device, BlendDescription.NonPremultiplied, nameof(NonPreMultiplied));
        AlphaBlend = Create(device, BlendDescription.AlphaBlend, nameof(AlphaBlend));
        Opaque = Create(device, BlendDescription.Opaque, nameof(Opaque));
        Additive = Create(device, BlendDescription.Additive, nameof(Additive));
    }

    public BlendState NonPreMultiplied { get; }
    public BlendState AlphaBlend { get; }
    public BlendState Opaque { get; }
    public BlendState Additive { get; }

    private static BlendState Create(ID3D11Device device, BlendDescription description, string name)
    {
        var state = device.CreateBlendState(description);
        return new BlendState(state, name);
    }

    public void Dispose()
    {
        NonPreMultiplied.Dispose();
        AlphaBlend.Dispose();
        Opaque.Dispose();
        Additive.Dispose();
    }
}
