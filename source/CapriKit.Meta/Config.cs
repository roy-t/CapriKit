using CapriKit.Meta.Utilities;

namespace CapriKit.Meta;

// TODO: uses types to clearly show the difference between folder paths, folder names, file paths, file names, etc...

internal static class Config
{
    public static string SolutionPath => FileSearchUtilities.SearchFileUp("*.sln").FirstOrDefault()
            ?? throw new FileNotFoundException($"Could not find *.sln file in {Environment.CurrentDirectory} or parent directories");

    public static string SolutionDirectory = Path.GetDirectoryName(SolutionPath) ?? Environment.CurrentDirectory;

    public static string VersionPath = Path.Combine(SolutionPath, "version.txt");

    public static class Outputs
    {
        public static string OutputDirectory => Path.Combine(SolutionDirectory, ".build");
        public static string PackageDirectory => Path.Combine(OutputDirectory, "pkg");
        public static string TestDirectory => Path.Combine(OutputDirectory, "tst");

        public static string TestResultsFileName => "test-report.trx";

        public static string TestResultsPath => Path.Combine(TestDirectory, TestResultsFileName);

        public static string BenchmarkDirectory => Path.Combine(TestDirectory, "results");
    }

    public static class Assets
    {
        public static string BenchmarkDirectory => Path.Combine(SolutionDirectory, "benchmarks");
    }
}
