using CapriKit.DirectX11.Debug;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace CapriKit.DirectX11.Contexts.States;

public sealed class SamplerState : IDisposable
{
    internal SamplerState(ID3D11SamplerState state, string? label, string? caller, string? callerFile, int callerLine)
    {
        Name = DebugName.For(this, label, caller, callerFile, callerLine);

        State = state;
        State.DebugName = Name;
    }

    public string Name { get; }

    internal ID3D11SamplerState State { get; }

    public void Dispose()
    {
        State.Dispose();
    }
}

public sealed class SamplerStates : IDisposable
{
    internal SamplerStates(ID3D11Device device)
    {
        PointWrap = Create(device, SamplerDescription.PointWrap, nameof(PointWrap));
        PointClamp = Create(device, SamplerDescription.PointClamp, nameof(PointClamp));
        LinearWrap = Create(device, SamplerDescription.LinearWrap, nameof(LinearWrap));
        LinearClamp = Create(device, SamplerDescription.LinearClamp, nameof(LinearClamp));
        AnisotropicWrap = Create(device, SamplerDescription.AnisotropicWrap, nameof(AnisotropicWrap));
        CompareLessEqualClamp = Create(device, CreateCompareLessEqualClamp(), nameof(CompareLessEqualClamp));
        CompareGreaterClamp = Create(device, CreateCompareGreaterClamp(), nameof(CompareGreaterClamp));
    }

    public SamplerState PointWrap { get; }
    public SamplerState PointClamp { get; }
    public SamplerState LinearWrap { get; }
    public SamplerState LinearClamp { get; }
    public SamplerState AnisotropicWrap { get; }

    public SamplerState CompareLessEqualClamp { get; }
    public SamplerState CompareGreaterClamp { get; }

    private static SamplerState Create(ID3D11Device device, SamplerDescription description, string label, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        var state = device.CreateSamplerState(description);
        return new SamplerState(state, label, caller, callerFile, callerLine);
    }

    private static SamplerDescription CreateCompareLessEqualClamp()
    {
        return new SamplerDescription()
        {
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            BorderColor = Colors.White,
            ComparisonFunc = ComparisonFunction.LessEqual,
            Filter = Filter.ComparisonMinMagLinearMipPoint, // Note: this was changed from MiniEngine and should give better quality for shadow maps/PCF
            MaxAnisotropy = 1,
            MaxLOD = float.MaxValue,
            MinLOD = float.MinValue,
            MipLODBias = 0
        };
    }

    private static SamplerDescription CreateCompareGreaterClamp()
    {
        return new SamplerDescription()
        {
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            BorderColor = Colors.White,
            ComparisonFunc = ComparisonFunction.Greater,
            Filter = Filter.ComparisonMinMagLinearMipPoint, // Note: this was changed from MiniEngine and should give better quality for shadow maps/PCF
            MaxAnisotropy = 1,
            MaxLOD = float.MaxValue,
            MinLOD = float.MinValue,
            MipLODBias = 0
        };
    }

    public void Dispose()
    {
        PointWrap.Dispose();
        PointClamp.Dispose();
        LinearWrap.Dispose();
        LinearClamp.Dispose();
        AnisotropicWrap.Dispose();
        CompareLessEqualClamp.Dispose();
        CompareGreaterClamp.Dispose();
    }
}
