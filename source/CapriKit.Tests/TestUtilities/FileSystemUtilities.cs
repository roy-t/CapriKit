using CapriKit.IO;
using CapriKit.Tests.IO.Watchers;

namespace CapriKit.Tests.TestUtilities;

internal static class FileSystemUtilities
{
    public static DirectoryPath CreateTemporaryDirectory()
    {
        var id = Path.GetRandomFileName();
        var path = Path.Combine(Path.GetTempPath(), $"{nameof(FileSystemEventListenerTests)}.{id}");
        var directory = new DirectoryPath(path);
        Directory.CreateDirectory(directory);

        return directory;
    }
}
