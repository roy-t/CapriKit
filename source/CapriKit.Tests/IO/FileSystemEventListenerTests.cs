using CapriKit.IO;

namespace CapriKit.Tests.IO;

internal class FileSystemEventListenerTests
{
    private DirectoryPath TempDirectory = null!;

    [Before(Test)]
    public void TestSetup()
    {
        var id = Path.GetRandomFileName();
        var path = Path.Combine(Path.GetTempPath(), $"{nameof(FileSystemEventListenerTests)}.{id}");
        TempDirectory = new DirectoryPath(path);
        Directory.CreateDirectory(TempDirectory);
    }

    [After(Test)]
    public void TestTeardown()
    {
        var info = new DirectoryInfo(TempDirectory);
        info.Delete(true);
    }

    [Test]
    public async Task OnFileChanged()
    {
        const string fileName = "sut.tmp";
        var created = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var deleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var fileSystem = new ScopedFileSystem(TempDirectory);
        using var watcher = fileSystem.Watch(new DirectoryPath("."));
        watcher.OnFileChanged += (s, e) =>
        {
            if (e.reason == FileSystemChangeKind.Created && e.target.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                created.TrySetResult();
            }

            if (e.reason == FileSystemChangeKind.Changed && e.target.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                changed.TrySetResult();
            }

            if (e.reason == FileSystemChangeKind.Deleted && e.target.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                deleted.TrySetResult();
            }
        };

        using (var create = fileSystem.CreateReadWrite(fileName)) { }
        using (var append = fileSystem.AppendWrite(fileName)) { append.WriteByte(123); }
        fileSystem.Delete(fileName);

        // Instead of a regular assert we have wait for the three events or timeout.
        try
        {
            await Task.WhenAll(created.Task, changed.Task, deleted.Task)
                .WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            Assert.Fail("Test timed out befor receiving all file system events: " +
                $"created:{created.Task.IsCompleted}, " +
                $"changed:{changed.Task.IsCompleted}, " +
                $"deleted:{deleted.Task.IsCompleted}.");
        }
    }
}
