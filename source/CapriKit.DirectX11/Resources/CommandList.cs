using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources;

public interface ICommandList : IDisposable
{
    internal ID3D11CommandList ID3D11CommandList { get; }
}

internal sealed class CommandList : ICommandList
{
    private readonly ID3D11CommandList Value;

    internal CommandList(ID3D11CommandList commandList)
    {
        Value = commandList;
    }

    ID3D11CommandList ICommandList.ID3D11CommandList => Value;

    public void Dispose()
    {
        Value.Dispose();
    }
}
