using CapriKit.DirectX11.Contexts;
using CapriKit.Tests.Tool.Tests.Framework;
using CapriKit.Win32;
using ImGuiNET;

namespace CapriKit.Tests.Tool.Tests;

internal sealed class WindowStatesTest(Win32Window window) : ITestScreen
{
    public string Title => "Window States";

    public void Render(DeviceContext _)
    {
        if (ImGui.Begin(Title))
        {
            ImGui.BeginDisabled(window.IsBorderlessFullScreen);
            if (ImGui.Button("Borderless Fullscreen"))
            {
                window.SwitchToBorderlessFullScreen();
            }
            ImGui.EndDisabled();

            ImGui.BeginDisabled(!window.IsBorderlessFullScreen);
            if (ImGui.Button("Windowed"))
            {
                window.SwitchToWindowed();
            }
            ImGui.EndDisabled();
            ImGui.End();
        }
    }

    public void Dispose() { }
}
