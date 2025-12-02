using System.Text;

namespace CapriKit.Meta.Utilities;

// TODO: move to IO library and use IVirtualFileSystem
public static class FileRotator
{
    public static FileInfo CreateFile(string folderPath, string fileNamePrefix, string fileExtension, int fileCount)
    {
        var directory = new DirectoryInfo(folderPath);
        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException(folderPath);
        }

        var files = directory.GetFiles($"{fileNamePrefix}-*{fileExtension}");
        if (files.Length >= fileCount)
        {
            var oldestFile = files.OrderBy(f => f.LastWriteTimeUtc).First();
            oldestFile.Delete();
        }

        var time = DateTime.Now.ToString("o");
        var targetFileName = CreateValidFileName($"{fileNamePrefix}-{time}{fileExtension}");

        var targetFile = new FileInfo(Path.Combine(folderPath, targetFileName));
        using var stream = targetFile.Create();
        return targetFile;
    }

    private static string CreateValidFileName(string fileName, char replacement = '_')
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var validFileName = new StringBuilder();
        foreach (var c in fileName)
        {
            if (invalidCharacters.Contains(c))
            {
                validFileName.Append(replacement);
            }
            else
            {
                validFileName.Append(c);
            }
        }

        return validFileName.ToString();
    }
}
