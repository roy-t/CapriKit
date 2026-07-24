using CapriKit.IO;
using CapriKit.IO.Watchers;
using CapriKit.Tests.TestUtilities;

namespace CapriKit.Tests.IO.Watchers;

internal class FileSystemEventListenerTests
{
    private DirectoryPath TempDirectory = null!;

    [Before(Test)]
    public void TestSetup()
    {
        TempDirectory = FileSystemUtilities.CreateTemporaryDirectory();
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

        var fileSystem = new FileSystem();
        var scopedFileSystem = new ScopedFileSystem(fileSystem, TempDirectory);
        var watcher = scopedFileSystem.Watch(TempDirectory, false);
        watcher.OnFileChanged += (s, e) =>
        {
            if (e.Kind == FileSystemChangeKind.Created && e.File.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                created.TrySetResult();
            }

            if (e.Kind == FileSystemChangeKind.Changed && e.File.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                changed.TrySetResult();
            }

            if (e.Kind == FileSystemChangeKind.Deleted && e.File.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                deleted.TrySetResult();
            }
        };

        using (var create = scopedFileSystem.CreateReadWrite(fileName)) { }
        using (var append = scopedFileSystem.AppendWrite(fileName)) { append.WriteByte(123); }
        scopedFileSystem.Delete(fileName);

        // Instead of a regular assert we have wait for the three events or timeout.
        try
        {
            await Task.WhenAll(created.Task, changed.Task, deleted.Task)
                .WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            Assert.Fail("Test timed out before receiving all file system events: " +
                $"created:{created.Task.IsCompleted}, " +
                $"changed:{changed.Task.IsCompleted}, " +
                $"deleted:{deleted.Task.IsCompleted}.");
        }
    }
}
