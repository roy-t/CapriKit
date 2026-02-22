using System.Diagnostics;
using System.Text;

namespace CapriKit.Tests.Tool;

public partial class Program
{
    public class DebugOutputTextWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        public override void WriteLine(string? value) =>
            Debug.WriteLine(value);
        public override void Write(char value) =>
            Debug.Write(value);
    }
}
