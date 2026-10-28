using CapriKit.DirectX11.Debug;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts.States;

public sealed class BlendState : IDisposable
{
    internal BlendState(ID3D11BlendState state, string? label, string? caller, string? callerFile, int callerLine)
    {
        Name = DebugName.For(this, label, caller, callerFile, callerLine);
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

    private static BlendState Create(ID3D11Device device, BlendDescription description, string label, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        var state = device.CreateBlendState(description);
        return new BlendState(state, label, caller, callerFile, callerLine);
    }

    public void Dispose()
    {
        NonPreMultiplied.Dispose();
        AlphaBlend.Dispose();
        Opaque.Dispose();
        Additive.Dispose();
    }
}
