using System.Text;
using System.Text.Json;

namespace CapriKit.IO;

public interface IVirtualFileSystem : IReadOnlyVirtualFileSystem
{
    /// <summary>
    /// Creates a new file if it does not exist, including the entire directory structure. If the file does exists it is truncated.
    /// </summary>
    Stream CreateReadWrite(FilePath file);

    /// <summary>
    /// Opens an existing file and moves the stream to the end of the file. Throws an exception if the file does not exist.
    /// </summary>
    Stream AppendReadWrite(FilePath file);
}

public interface IReadOnlyVirtualFileSystem
{
    /// <summary>
    /// Opens an existing file for reading. Throws an exception if the file does not exist.
    /// </summary>
    Stream OpenRead(FilePath file);

    /// <summary>
    /// Returns true if the file exists, false otherwise.
    /// </summary>
    bool Exists(FilePath file);

    /// <summary>
    /// Returns the file size in bytes. Throws an exception if the file does not exist.
    /// </summary>
    int SizeInBytes(FilePath file);

    /// <summary>
    /// Returns when the file was last changed, in local time. Throws an exception if the file does not exist.
    /// </summary>
    DateTime LastWriteTime(FilePath file);
}

public static class VirtualFileSystemExtensions
{
    public static async Task<string> ReadAllText(this IReadOnlyVirtualFileSystem system, FilePath file, Encoding encoding, CancellationToken cancellationToken = default)
    {
        using var stream = system.OpenRead(file);
        using var reader = new StreamReader(stream, encoding);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task WriteAllText(this IVirtualFileSystem system, FilePath file, Encoding encoding, string text, CancellationToken cancellationToken = default)
    {
        using var stream = system.CreateReadWrite(file);
        using var writer = new StreamWriter(stream, encoding);
        await writer.WriteAsync(text).ConfigureAwait(false);
    }

    public static async Task<T> ReadJsonObject<T>(this IReadOnlyVirtualFileSystem system, FilePath file, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
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
