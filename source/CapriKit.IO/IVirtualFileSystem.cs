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
    long SizeInBytes(FilePath file);

    /// <summary>
    /// Returns when the file was last changed, in local time. Throws an exception if the file does not exist.
    /// </summary>
    DateTime LastWriteTime(FilePath file);
}
