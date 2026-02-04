using CapriKit.IO;

namespace CapriKit.Meta;

internal static class Config
{
    public static DirectoryPath SolutionDirectory = IOUtilities.SearchForDirectoryWithMarker(Environment.CurrentDirectory, "CapriKit.sln") ??
        throw new Exception($"Could not find CapriKit.sln in {Environment.CurrentDirectory} or any of its parent directories.");

    public static FilePath SolutionPath = SolutionDirectory.Append("CapriKit.sln");

    public static FilePath VersionPath = SolutionDirectory.Append("version.txt");

    public static class Outputs
    {
        public static DirectoryPath OutputDirectory => SolutionDirectory.Append([".build"]);
        public static DirectoryPath PackageDirectory => OutputDirectory.Append(["pkg"]);
        public static DirectoryPath TestDirectory => OutputDirectory.Append(["tst"]);

        public static FilePath TestResultsFileName => "test-report.trx";

        public static FilePath TestResultsPath => TestDirectory.Append(TestResultsFileName);

        public static DirectoryPath BenchmarkDirectory => TestDirectory.Append(["results"]);
    }

    public static class Assets
    {
        public static DirectoryPath BenchmarkDirectory => SolutionDirectory.Append(["benchmarks"]);
    }
}
