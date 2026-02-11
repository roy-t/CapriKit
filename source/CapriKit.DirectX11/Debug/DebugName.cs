using System.Runtime.CompilerServices;

namespace CapriKit.DirectX11.Debug;

internal static class DebugName
{
    private static int NextSequenceId = 1;

    public static string For<T>(string? hint = null, [CallerMemberName] string? caller = null)
        => For(typeof(T), hint, caller);

    public static string For(Type type, string? hint = null, [CallerMemberName] string? caller = null)
    {
        var typeName = type.Name;
        var id = Interlocked.Increment(ref NextSequenceId);
        return string.Concat(
            typeName,
            string.IsNullOrWhiteSpace(hint) ? "" : $":{hint}",
            string.IsNullOrWhiteSpace(caller) ? "" : $"@{caller}",
            "#", id);
    }
}
