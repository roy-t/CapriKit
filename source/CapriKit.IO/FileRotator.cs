using static CapriKit.IO.RotationPolicy;

namespace CapriKit.IO;

/// <summary>
/// Policy for maintaing a set of rotating log files
/// </summary>
/// <param name="Generator">Generates names for new log files</param>
/// <param name="Matcher">Matches log files that belong to this set</param>
/// <param name="Executor">Determines if a file should be deleted based on the number of files in this set and the name of the file. The fate of the file with the oldest last write date is decided first.</param>
public record RotationPolicy(GenerateFileName Generator, IsFileSubjectToPolicy Matcher, ShouldDeleteFile Executor)
{
    public delegate FilePath GenerateFileName(int currentFileCount);
    public delegate bool IsFileSubjectToPolicy(FilePath file);
    public delegate bool ShouldDeleteFile(int currentFileCount, FilePath file);

    /// <summary>
    /// A rotation policy that maintains a set of `count` files named `prefix-{TIMESTAMP}.extension`
    /// </summary>
    public static RotationPolicy FixedCount(string prefix, string extension, int count = 10)
    {
        return new RotationPolicy(
            _ => $"{prefix}-{DateTime.Now.Ticks}.{extension}",
            s => s.StartsWith(prefix) && s.EndsWith(extension),
            (i, _) => i >= count
            );
    }
}

public static class FileRotator
{
    public static (FilePath, Stream) CreateFile(IVirtualFileSystem fileSystem, DirectoryPath directory, RotationPolicy policy)
    {
        var candidates = fileSystem.List(directory)
            .Where(f => policy.Matcher(f))
            .OrderBy(f => fileSystem.LastWriteTime(f))
            .ToList();

        var retainedCount = candidates.Count;

        foreach (var file in candidates)
        {
            if (policy.Executor(retainedCount, file))
            {
                fileSystem.Delete(file);
                retainedCount--;
            }
        }

        var newFile = policy.Generator(retainedCount);
        return (newFile, fileSystem.CreateReadWrite(newFile));
    }

    public static (FilePath, Stream) CreateFile(DirectoryPath directory, RotationPolicy policy)
    {
        var fileSystem = new ScopedFileSystem(directory);
        var (file, stream) = CreateFile(fileSystem, directory, policy);
        return (file.ToAbsolute(directory), stream);
    }
}
