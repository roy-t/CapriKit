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
        // TODO: this doesn't work since startswith expects a directory, not a string/filename!
        // PROBABLY A fault  in the API for StartsWith, maybe we need a generic Path object for this?
        var rotationPolicy = new RotationPolicy(
            _ => $"command-log-{DateTime.Now.Ticks}.log",
            s => s.StartsWith("command-log-") && s.EndsWith(".log"),
            (i, _) => i >= 10
            );

        var (logFile, logStream) = FileRotator.CreateFile(Directory.GetCurrentDirectory(), rotationPolicy);
        var logStreamWriter = new StreamWriter(logStream) { AutoFlush = true };

        return new CommandLogger(logFile, logStream, logStreamWriter);
    }
}
