using CapriKit.Win32;
using CapriKit.Win32.Input;
using ImGuiNET;
using System.Numerics;

namespace CapriKit.DirectX11.Debug;

public sealed class ImGuiController : IDisposable
{
    private readonly ImGuiIOPtr IO;
    private readonly ImGuiRenderer Renderer;
    private readonly ImGuiInput Input;

    public ImGuiController(Device device, Win32Window window, Keyboard keyboard, Mouse mouse)
    {
        ImGui.CreateContext();
        IO = ImGui.GetIO();
        Renderer = new ImGuiRenderer(device);
        Input = new ImGuiInput(IO, window, keyboard, mouse);

        Resize(device.Width, device.Height);
    }

    public void Resize(int width, int height)
    {
        IO.DisplaySize = new Vector2(width, height);
    }

    public void NewFrame(float elapsed)
    {
        IO.DeltaTime = elapsed;
        Input.NewFrame();
        ImGui.NewFrame();
    }

    public void Render()
    {
        ImGui.Render();
        Renderer.Render(ImGui.GetDrawData());
    }

    public void Dispose()
    {
        Renderer.Dispose();
        ImGui.DestroyContext();
    }
}
