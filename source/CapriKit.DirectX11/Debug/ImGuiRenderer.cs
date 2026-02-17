using ImGuiNET;

namespace CapriKit.DirectX11.Debug;

// TODO: For IMGUI I should probably just render on the ImmediateContext as its a debug view
// for other rendering Tasks is probably not a good idea as I can't really await anything and
// I need to task.Run(..) and have overhead. Instead I should come up with something similar like I did
// in the old engine, but with better ergonomics. Like A DirectX11Job system that immediately starts work when
// its enqueued, separate from the simulation.
public sealed class ImGuiRenderer : IDisposable
{
    private readonly Device Device;

    public ImGuiRenderer(Device device)
    {
        Device = device;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    internal void Render(ImDrawDataPtr imDrawDataPtr)
    {
        throw new NotImplementedException();
    }
}
