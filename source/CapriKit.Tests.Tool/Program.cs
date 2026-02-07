using CapriKit.Win32;

namespace CapriKit.Tests.Tool;

public class Program
{
    [STAThread]
    static void Main()
    {
        var window = Win32Application.Initialize("CapriKit.Tests.Tool");

        var isRunning = true;
        while (isRunning)
        {
            isRunning = Win32Application.PumpMessages();
        }
    }
}
