using CapriKit.Meta.Utilities;

namespace CapriKit.Meta.Builds;


internal sealed class BuildLogger : IDisposable
{
    private readonly Stream LogStream;

    public BuildLogger(FileInfo logFile, Stream logStream, StreamWriter logStreamWriter)
    {
        LogStream = logStream;
        File = logFile;
        Writer = logStreamWriter;
    }

    public FileInfo File { get; }
    public StreamWriter Writer { get; }

    public void Dispose()
    {
        this.Writer.Dispose();
        this.LogStream.Dispose();
    }

    public static BuildLogger CreateBuildLogger()
    {
        var logFile = FileRotator.CreateFile(Directory.GetCurrentDirectory(), "release", ".log", 10);
        var logStream = logFile.Open(FileMode.Append, FileAccess.Write, FileShare.Read);
        var logStreamWriter = new StreamWriter(logStream) { AutoFlush = true };

        return new BuildLogger(logFile, logStream, logStreamWriter);
    }
}
