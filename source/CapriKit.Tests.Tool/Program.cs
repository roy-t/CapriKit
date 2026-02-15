using CapriKit.DirectX11;
using CapriKit.DirectX11.Debug;
using CapriKit.Win32;
using CapriKit.Win32.Input;
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

        var running = true;
        while (running)
        {
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

            // TODO: switch to borderless window?
            device.Clear();
            device.Present();

            running &= Win32Application.PumpMessages();
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
