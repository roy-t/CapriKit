using static CapriKit.IO.RotationPolicy;

namespace CapriKit.IO;

public record RotationPolicy(GenerateFileName Generator, IsFileSubjectToPolicy Matcher, ShouldDeleteFile Executor)
{
    public delegate FilePath GenerateFileName(int currentFileCount);
    public delegate bool IsFileSubjectToPolicy(FilePath file);
    public delegate bool ShouldDeleteFile(int currentFileCount, FilePath file);
}

public static class FileRotator
{
    public static (FilePath, Stream) CreateFile(IVirtualFileSystem fileSystem, DirectoryPath directory, RotationPolicy policy)
    {
        var candidates = fileSystem.List(directory)
            .Where(f => policy.Matcher(f))
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
