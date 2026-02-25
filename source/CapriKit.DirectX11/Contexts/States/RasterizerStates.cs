using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts.States;

public sealed class RasterizerState : IDisposable
{
    internal RasterizerState(ID3D11RasterizerState state, string nameHint, RasterizerDescription description)
    {
        Name = DebugName.For(this, nameHint);

        State = state;
        State.DebugName = Name;
        Description = description;
    }

    public string Name { get; }

    internal RasterizerDescription Description { get; }
    internal ID3D11RasterizerState State { get; }

    public void Dispose()
    {
        State.Dispose();
    }
}

public sealed class RasterizerStates : IDisposable
{
    private const int DefaultDepthBias = 0;
    private const float DefaultDepthBiasClamp = 0.0f;
    private const float DefaultSlopeScaledDepthBias = 0.0f;

    internal RasterizerStates(ID3D11Device device)
    {
        // TODO: default descriptions do not enable the scissor rectangle!

        WireFrame = Create(device, RasterizerDescription.Wireframe, nameof(WireFrame));
        Line = Create(device, CreateLine(), nameof(Line));

        CullNone = Create(device, RasterizerDescription.CullNone, nameof(CullNone));

        CullCounterClockwise = Create(device, RasterizerDescription.CullBack, nameof(CullCounterClockwise));
        CullClockwise = Create(device, RasterizerDescription.CullFront, nameof(CullClockwise));

        CullNoneNoDepthClip = Create(device, CreateCullNoneNoDepthClip(), nameof(CullNoneNoDepthClip));
        CullCounterClockwiseNoDepthClip = Create(device, CreateCullCounterClockwiseNoDepthClip(), nameof(CullCounterClockwiseNoDepthClip));
        CullClockwiseNoDepthClip = Create(device, CreateCullClockwiseNoDepthClip(), nameof(CullClockwiseNoDepthClip));

        Default = CullCounterClockwise;
    }

    public RasterizerState Default { get; set; }

    public RasterizerState WireFrame { get; }

    /// <summary>
    /// Note: incompatible with MSAA
    /// </summary>
    public RasterizerState Line { get; }

    public RasterizerState CullNone { get; }

    public RasterizerState CullCounterClockwise { get; }

    public RasterizerState CullClockwise { get; }

    public RasterizerState CullNoneNoDepthClip { get; }
    public RasterizerState CullCounterClockwiseNoDepthClip { get; }
    public RasterizerState CullClockwiseNoDepthClip { get; }

    public static RasterizerState CreateBiased(Device device, RasterizerState template, int depthBias, float depthBiasClamp = 0.0f, float slopeScaledDepthBias = 0.0f)
    {
        var description = template.Description;
        description.DepthBias = depthBias;
        description.DepthBiasClamp = depthBiasClamp;
        description.SlopeScaledDepthBias = slopeScaledDepthBias;
        return Create(device.ID3D11Device, description, string.Empty);
    }

    private static RasterizerState Create(ID3D11Device device, RasterizerDescription description, string name)
    {
        description.ScissorEnable = true;
        var state = device.CreateRasterizerState(description);
        return new RasterizerState(state, name, description);
    }

    private static RasterizerDescription CreateCullNoneNoDepthClip()
    {
        return new RasterizerDescription()
        {
            CullMode = CullMode.None,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthBias = DefaultDepthBias,
            DepthBiasClamp = DefaultDepthBiasClamp,
            SlopeScaledDepthBias = DefaultSlopeScaledDepthBias,
            DepthClipEnable = false,
            ScissorEnable = true,
            MultisampleEnable = true,
            AntialiasedLineEnable = false,
        };
    }

    private static RasterizerDescription CreateCullCounterClockwiseNoDepthClip()
    {
        return new RasterizerDescription()
        {
            CullMode = CullMode.Front,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthBias = DefaultDepthBias,
            DepthBiasClamp = DefaultDepthBiasClamp,
            SlopeScaledDepthBias = DefaultSlopeScaledDepthBias,
            DepthClipEnable = false,
            ScissorEnable = true,
            MultisampleEnable = true,
            AntialiasedLineEnable = false,
        };
    }

    private static RasterizerDescription CreateCullClockwiseNoDepthClip()
    {
        return new RasterizerDescription()
        {
            CullMode = CullMode.Back,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthBias = DefaultDepthBias,
            DepthBiasClamp = DefaultDepthBiasClamp,
            SlopeScaledDepthBias = DefaultSlopeScaledDepthBias,
            DepthClipEnable = false,
            ScissorEnable = true,
            MultisampleEnable = true,
            AntialiasedLineEnable = false,
        };
    }

    private static RasterizerDescription CreateLine()
    {

        return new RasterizerDescription()
        {
            CullMode = CullMode.None,
            FillMode = FillMode.Solid,
            FrontCounterClockwise = false,
            DepthBias = DefaultDepthBias,
            DepthBiasClamp = DefaultDepthBiasClamp,
            SlopeScaledDepthBias = DefaultSlopeScaledDepthBias,
            DepthClipEnable = true,
            ScissorEnable = true,
            MultisampleEnable = false,
            AntialiasedLineEnable = true,
        };
    }

    public void Dispose()
    {
        Default.Dispose();
        WireFrame.Dispose();
        Line.Dispose();

        CullNone.Dispose();
        CullCounterClockwise.Dispose();
        CullClockwise.Dispose();

        CullNoneNoDepthClip.Dispose();
        CullCounterClockwiseNoDepthClip.Dispose();
        CullClockwiseNoDepthClip.Dispose();
    }
}
