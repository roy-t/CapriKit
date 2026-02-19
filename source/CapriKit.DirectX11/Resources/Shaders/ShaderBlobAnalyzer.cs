using System.Text;

namespace CapriKit.DirectX11.Resources.Shaders;

public static class ShaderBlobAnalyzer
{
    public static void ThrowOnWarningOrError(ReadOnlySpan<byte> errorBlob, params string[] ignores)
    {
        var output = Encoding.ASCII.GetString(errorBlob)[..(errorBlob.Length - 1)];
        var messages = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var error = new StringBuilder();
        foreach (var message in messages)
        {
            if (!IsIgnored(message, ignores))
            {
                error.AppendLine(message);
            }
        }

        if (error.Length > 0)
        {
            throw new Exception(error.ToString());
        }
    }

    private static bool IsIgnored(string message, params string[] ignores)
    {
        foreach (var ignore in ignores)
        {
            if (message.Contains(ignore, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
