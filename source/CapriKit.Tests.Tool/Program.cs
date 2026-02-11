using CapriKit.Win32;
using CapriKit.Win32.Input;

namespace CapriKit.Tests.Tool;

public class Program
{
    [STAThread]
    static void Main()
    {
        Win32Application.Initialize("CapriKit.Tests.Tool");

        var window = Win32Application.Window;
        var mouse = Win32Application.Mouse;
        var keyboard = Win32Application.Keyboard;
        var isRunning = true;
        while (isRunning)
        {
            isRunning = Win32Application.PumpMessages();

            if (keyboard.Pressed(VirtualKeyCode.VK_ESCAPE))
            {
                return;
            }
        }
    }
}
