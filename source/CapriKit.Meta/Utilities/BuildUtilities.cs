namespace CapriKit.Meta.Utilities;


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
        var logStream = logFile.OpenWrite();
        var logStreamWriter = new StreamWriter(logStream);

        return new BuildLogger(logFile, logStream, logStreamWriter);
    }
}
