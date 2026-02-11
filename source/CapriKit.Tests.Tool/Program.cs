using CapriKit.DirectX11;
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
    static void Main()
    {
#if DEBUG // Ensure writes to console are redirected to Visual Studio
        Console.SetOut(new DebugOutputTextWriter());
#endif
        Win32Application.Initialize("CapriKit.Tests.Tool");

        var window = Win32Application.Window;
        var mouse = Win32Application.Mouse;
        var keyboard = Win32Application.Keyboard;

        using var device = new Device(window.Handle, window.Width, window.Height);

        while (Win32Application.PumpMessages())
        {
            if (keyboard.Pressed(VirtualKeyCode.VK_ESCAPE))
            {
                return;
            }

            if (!window.IsMinimized && (window.Width != device.Width || window.Height != device.Height))
            {
                device.Resize(window.Width, window.Height);
            }

            // TODO: switch to borderless window?
            device.Clear();
            device.Present();
        }
    }
}
