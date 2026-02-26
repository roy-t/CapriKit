using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources;

// Copy of Vortice.Direct3.PrimitiveTopology
public enum PrimitiveTopology
{
    Undefined = 0,
    PointList = 1,
    LineList = 2,
    LineStrip = 3,
    TriangleList = 4,
    TriangleStrip = 5,
    TriangleFan = 6,
    LineListAdjacency = 10,
    LineStripAdjacency = 11,
    TriangleListAdjacency = 12,
    TriangleStripAdjacency = 13,
    PatchListWith1ControlPoints = 33,
    PatchListWith2ControlPoints = 34,
    PatchListWith3ControlPoints = 35,
    PatchListWith4ControlPoints = 36,
    PatchListWith5ControlPoints = 37,
    PatchListWith6ControlPoints = 38,
    PatchListWith7ControlPoints = 39,
    PatchListWith8ControlPoints = 40,
    PatchListWith9ControlPoints = 41,
    PatchListWith10ControlPoints = 42,
    PatchListWith11ControlPoints = 43,
    PatchListWith12ControlPoints = 44,
    PatchListWith13ControlPoints = 45,
    PatchListWith14ControlPoints = 46,
    PatchListWith15ControlPoints = 47,
    PatchListWith16ControlPoints = 48,
    PatchListWith17ControlPoints = 49,
    PatchListWith18ControlPoints = 50,
    PatchListWith19ControlPoints = 51,
    PatchListWith20ControlPoints = 52,
    PatchListWith21ControlPoints = 53,
    PatchListWith22ControlPoints = 54,
    PatchListWith23ControlPoints = 55,
    PatchListWith24ControlPoints = 56,
    PatchListWith25ControlPoints = 57,
    PatchListWith26ControlPoints = 58,
    PatchListWith27ControlPoints = 59,
    PatchListWith28ControlPoints = 60,
    PatchListWith29ControlPoints = 61,
    PatchListWith30ControlPoints = 62,
    PatchListWith31ControlPoints = 63,
    PatchListWith32ControlPoints = 64
}
public interface IComputeShader
{
    internal ID3D11ComputeShader ID3D11ComputeShader { get; }

    /// <summary>
    /// The shader kernel defines how big the groups for each dimension are.
    /// Use this method to compute the minimum number of groups to dispatch to
    /// cover your entire volume.
    /// </summary>    
    (uint X, uint Y, uint Z) GetDispatchSize(uint dimX, uint dimY, uint dimZ);
}

public interface IShaderResourceView
{
    internal ID3D11ShaderResourceView ID3D11ShaderResourceView { get; }
}


public interface IUnorderedAccessView
{
    internal ID3D11UnorderedAccessView ID3D11UnorderedAccessView { get; }
}

public interface IUnorderedAccessViewArray
{
    internal ID3D11UnorderedAccessView[] ID3D11UnorderedAccessViews { get; }
}

public interface IInputLayout
{
    internal ID3D11InputLayout ID3D11InputLayout { get; }
}

public interface IRenderTargetView
{
    internal ID3D11RenderTargetView ID3D11RenderTargetView { get; }
}

public interface IRenderTargetViewArray
{
    internal ID3D11RenderTargetView[] ID3D11RenderTargetViews { get; }
}

public interface IDepthStencilView
{
    internal ID3D11DepthStencilView ID3D11DepthStencilView { get; }
}

public interface IDepthStencilViewArray
{
    internal ID3D11DepthStencilView[] ID3D11DepthStencilViews { get; }
}

public interface IPixelShader
{
    internal ID3D11PixelShader ID3D11PixelShader { get; }
}

public interface IVertexShader
{
    internal ID3D11VertexShader ID3D11VertexShader { get; }

    IInputLayout CreateInputLayout(Device device, InputElementDescription[] elements); // TODO: use a non Vortice struct for this    
}

public interface ICommandList
{
    internal ID3D11CommandList ID3D11CommandList { get; }
}

public sealed class CommandList : ICommandList
{
    private readonly ID3D11CommandList Value;

    internal CommandList(ID3D11CommandList commandList)
    {
        Value = commandList;
    }

    ID3D11CommandList ICommandList.ID3D11CommandList => Value;
}

public class InputLayout : IInputLayout
{
    private readonly ID3D11InputLayout Value;

    internal InputLayout(ID3D11InputLayout inputLayout)
    {
        Value = inputLayout;
    }

    ID3D11InputLayout IInputLayout.ID3D11InputLayout => Value;
}


public class ShaderResourceView : IShaderResourceView
{
    private readonly ID3D11ShaderResourceView Value;

    internal ShaderResourceView(ID3D11ShaderResourceView view)
    {
        Value = view;
    }

    ID3D11ShaderResourceView IShaderResourceView.ID3D11ShaderResourceView => Value;
}

public sealed class UnorderedAccessView : IUnorderedAccessView
{
    private readonly ID3D11UnorderedAccessView Value;

    internal UnorderedAccessView(ID3D11UnorderedAccessView view)
    {
        Value = view;
    }

    ID3D11UnorderedAccessView IUnorderedAccessView.ID3D11UnorderedAccessView => Value;
}
