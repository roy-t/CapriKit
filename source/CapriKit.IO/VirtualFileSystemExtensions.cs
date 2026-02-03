using System.Text;
using System.Text.Json;

namespace CapriKit.IO;

public static class VirtualFileSystemExtensions
{
    public static async Task<string> ReadAllText(this IReadOnlyVirtualFileSystem system, FilePath file, Encoding? encoding = default, CancellationToken cancellationToken = default)
    {
        using var stream = system.OpenRead(file);
        using var reader = new StreamReader(stream, encoding);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task WriteAllText(this IVirtualFileSystem system, FilePath file, string text, Encoding? encoding = default, CancellationToken cancellationToken = default)
    {
        using var stream = system.CreateReadWrite(file);
        using var writer = new StreamWriter(stream, encoding);
        await writer.WriteAsync(text).ConfigureAwait(false);
    }

    public static async ValueTask<T> ReadJsonObject<T>(this IReadOnlyVirtualFileSystem system, FilePath file, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var stream = system.OpenRead(file);
        return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException($"Deserializing contents of {file} to {nameof(T)} resulted in null");
    }

    public static async Task WriteJsonObject<T>(this IVirtualFileSystem system, FilePath file, T value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var stream = system.CreateReadWrite(file);
        await JsonSerializer.SerializeAsync<T>(stream, value, options, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<byte[]> ReadAllBytes(this IReadOnlyVirtualFileSystem system, FilePath file, CancellationToken cancellationToken = default)
    {
        using var stream = system.OpenRead(file);
        if (stream is MemoryStream memory)
        {
            return memory.ToArray();
        }

        var size = system.SizeInBytes(file);
        var buffer = new byte[size];
        await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer;
    }

    public static async Task WriteAllBytes(this IVirtualFileSystem system, FilePath file, byte[] bytes, CancellationToken cancellationToken = default)
    {
        using var stream = system.CreateReadWrite(file);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}
