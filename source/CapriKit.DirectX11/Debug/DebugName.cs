using System.Runtime.CompilerServices;
using System.Text;

namespace CapriKit.DirectX11.Debug;

internal static class DebugName
{
    private static int NextSequenceId = 1;

    public static string For<T>(string? hint = null, [CallerMemberName] string? caller = null)
        => For(typeof(T), hint, caller);

    public static string For(Type type, string? hint = null, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null)
    {
        var id = Interlocked.Increment(ref NextSequenceId);
        var typeName = type.Name;
        var builder = new StringBuilder();

        builder.Append(typeName);
        builder.Append(':');
        builder.Append(hint);

        if (string.IsNullOrEmpty(caller) && !string.IsNullOrEmpty(callerFile))
        {
            builder.Append('@');
            builder.Append(Path.GetFileNameWithoutExtension(callerFile));
        }
        else if (!string.IsNullOrEmpty(caller) && string.IsNullOrEmpty(callerFile))
        {
            builder.Append('@');
            builder.Append(caller);
        }
        else if (!string.IsNullOrEmpty(caller) && !string.IsNullOrEmpty(callerFile))
        {
            builder.Append('@');
            builder.Append(Path.GetFileNameWithoutExtension(callerFile));
            builder.Append('.');
            builder.Append(caller);
        }

        builder.Append('#');
        builder.Append(id);

        return builder.ToString();
    }
}
