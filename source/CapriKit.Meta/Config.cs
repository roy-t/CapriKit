using CapriKit.IO;

namespace CapriKit.Meta;

internal static class Config
{
    public static DirectoryPath SolutionDirectory = IOUtilities.SearchForDirectoryWithMarker(Environment.CurrentDirectory, "CapriKit.sln") ??
        throw new Exception($"Could not find CapriKit.sln in {Environment.CurrentDirectory} or any of its parent directories.");

    public static FilePath SolutionPath = SolutionDirectory.Join("CapriKit.sln");

    public static FilePath VersionPath = SolutionDirectory.Join("version.txt");

    public static class Outputs
    {
        public static DirectoryPath OutputDirectory => SolutionDirectory.Join([".build"]);
        public static DirectoryPath PackageDirectory => OutputDirectory.Join(["pkg"]);
        public static DirectoryPath TestDirectory => OutputDirectory.Join(["tst"]);

        public static FilePath TestResultsFileName => "test-report.trx";

        public static FilePath TestResultsPath => TestDirectory.Join(TestResultsFileName);

        public static DirectoryPath BenchmarkDirectory => TestDirectory.Join(["results"]);
    }

    public static class Assets
    {
        public static DirectoryPath BenchmarkDirectory => SolutionDirectory.Join(["benchmarks"]);
    }
}
