using CapriKit.DirectX11;
using CapriKit.DirectX11.Debug;
using CapriKit.Win32;
using CapriKit.Win32.Input;
using ImGuiNET;
using System.Diagnostics;
using System.Text;

namespace CapriKit.Tests.Tool;

public class Program
{
    public class DebugOutputTextWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        public override void WriteLine(string? value) =>
            Debug.WriteLine(value);
        public override void Write(char value) =>
            Debug.WriteLine(value);
    }

    private const double DELTA_TIME = 1.0 / 60.0; // constant tick rate of simulation

    [STAThread]
    static void Main() // TODO: main loop is getting a bit cluttered
    {
#if DEBUG // Ensure writes to console are redirected to Visual Studio
        Console.SetOut(new DebugOutputTextWriter());
#endif
        Win32Application.Initialize("CapriKit.Tests.Tool");

        var window = Win32Application.Window;
        var mouse = Win32Application.Mouse;
        var keyboard = Win32Application.Keyboard;

        using var rd = RenderDoc.TryLoad();

        using var device = new Device(window);
        using var imgui = new ImGuiController(device, window, keyboard, mouse);

        var running = true;
        var elapsed = DELTA_TIME;
        var stopwatch = Stopwatch.StartNew();
        while (running)
        {
            imgui.NewFrame((float)elapsed);
            if (keyboard.Pressed(VirtualKeyCode.VK_ESCAPE))
            {
                running = false;
            }

            if (keyboard.Pressed(VirtualKeyCode.VK_F1))
            {
                rd?.TriggerCapture();
            }

            if (keyboard.Pressed(VirtualKeyCode.VK_F11))
            {
                if (window.IsBorderlessFullScreen)
                {
                    window.SwitchToWindowed();
                }
                else
                {
                    window.SwitchToBorderlessFullScreen();
                }
            }

            if (!window.IsMinimized && (window.Width != device.Width || window.Height != device.Height))
            {
                device.Resize(window.Width, window.Height);
                Console.WriteLine($"{window.Width}x{window.Height}");
            }

            device.Clear();
            ImGui.ShowDemoWindow();
            imgui.Render();
            device.Present();

            running &= Win32Application.PumpMessages();

            elapsed = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();
        }

        // Open RenderDoc to analyze the last taken capture
        if (rd != null)
        {
            var numCaptures = rd.GetNumCaptures();
            if (numCaptures > 0)
            {
                var capture = rd.GetCapture(numCaptures - 1);
                rd.LaunchReplayUI(capture);
            }
        }
    }
}
