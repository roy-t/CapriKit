using CapriKit.IO;

namespace CapriKit.Meta.Utilities;


internal sealed class CommandLogger : IDisposable
{
    private readonly Stream LogStream;

    public CommandLogger(FilePath logFile, Stream logStream, StreamWriter logStreamWriter)
    {
        LogStream = logStream;
        File = logFile;
        Writer = logStreamWriter;
    }

    public FilePath File { get; }
    public StreamWriter Writer { get; }

    public void Dispose()
    {
        this.Writer.Dispose();
        this.LogStream.Dispose();
    }

    public static CommandLogger CreateBuildLogger()
    {
        var rotationPolicy = RotationPolicy.FixedCount("command-log", ".log", 10);
        var (logFile, logStream) = FileRotator.CreateFile(Directory.GetCurrentDirectory(), rotationPolicy);
        var logStreamWriter = new StreamWriter(logStream) { AutoFlush = true };

        return new CommandLogger(logFile, logStream, logStreamWriter);
    }
}
