using System.Runtime.CompilerServices;
using System.Text;

namespace CapriKit.DirectX11.Debug;

/// <summary>
/// Creates names for native objects that show up in debug layer messages and tools like RenderDoc,
/// formatted as Type[:label]@File[.Member]:Line. In release builds names are empty.
/// Public methods that create native objects should take [Caller...] parameters and forward them,
/// so that the name points to the user's code instead of to this library.
/// </summary>
internal static class DebugName
{
    public static string For<T>(string? label = null, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        return For(typeof(T), label, caller, callerFile, callerLine);
    }

    public static string For(object instance, string? label = null, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        return For(instance.GetType(), label, caller, callerFile, callerLine);
    }

    private static string For(Type type, string? label, string? caller, string? callerFile, int callerLine)
    {
#if DEBUG
        var builder = new StringBuilder();
        AppendTypeName(builder, type);

        if (!string.IsNullOrEmpty(label))
        {
            builder.Append(':');
            builder.Append(label);
        }

        builder.Append('@');
        builder.Append(Path.GetFileNameWithoutExtension(callerFile));

        // Skips .ctor and .cctor, the file name already identifies the class
        if (!string.IsNullOrEmpty(caller) && !caller.StartsWith('.'))
        {
            builder.Append('.');
            builder.Append(caller);
        }

        builder.Append(':');
        builder.Append(callerLine);

        return builder.ToString();
#else
        return string.Empty;
#endif
    }

    // Writes List<int> instead of List`1
    private static void AppendTypeName(StringBuilder builder, Type type)
    {
        var tick = type.Name.IndexOf('`');
        if (tick < 0)
        {
            builder.Append(type.Name);
            return;
        }

        builder.Append(type.Name, 0, tick);
        builder.Append('<');
        var arguments = type.GetGenericArguments();
        for (var i = 0; i < arguments.Length; i++)
        {
            if (i > 0) { builder.Append(", "); }
            AppendTypeName(builder, arguments[i]);
        }
        builder.Append('>');
    }
}
