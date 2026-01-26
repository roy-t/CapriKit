namespace CapriKit.Meta.Utilities;


internal record BuildInputs(string SolutionPath, string PackagePath, string TestResultsDirectory, string TestResultsFileName, string BenchmarkResultsDirectory, string DocumentationDirectory);

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
}

internal static class BuildUtilities
{
    public static BuildInputs GatherBuildInputs()
    {
        var solutionPath = FileSearchUtilities.SearchFileUp("*.sln").FirstOrDefault()
            ?? throw new FileNotFoundException($"Could not find *.sln file in {Environment.CurrentDirectory} or parent directories");

        var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        var documentationDirectory = Path.Combine(solutionDirectory, "documentation");
        var packagePath = Path.Combine(solutionDirectory, ".build", "pkg");
        var testResultsDirectory = Path.Combine(solutionDirectory, ".build", "tst");
        var testResultsFileName = "test-report.trx";

        var benchmarkResultsDirectory = Path.Combine(testResultsDirectory, "results");
        return new BuildInputs(solutionPath, packagePath, testResultsDirectory, testResultsFileName, benchmarkResultsDirectory, documentationDirectory);
    }

    public static BuildLogger CreateBuildLogger()
    {
        var logFile = FileRotator.CreateFile(Directory.GetCurrentDirectory(), "release", ".log", 10);
        var logStream = logFile.OpenWrite();
        var logStreamWriter = new StreamWriter(logStream);

        return new BuildLogger(logFile, logStream, logStreamWriter);
    }
}
