using CapriKit.Build;
using System.Text.Json;

namespace CapriKit.Meta.Benchmarks;

internal record BenchmarkEntry(SemVer Version, DateTime Timestamp, string Id, double Mean, double StandardError, double StandardDeviation, int SampleCount);

internal static class BenchmarkRepository
{
    public static IReadOnlyList<BenchmarkEntry> Load(SemVer version)
    {
        var file = GetFilePath(version);
        if (!file.Exists)
        {
            throw new FileNotFoundException(null, file.FullName);
        }

        using var stream = file.OpenRead();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<IReadOnlyList<BenchmarkEntry>>(json)
            ?? throw new Exception($"Failed to deserialize benchmark entries in {file.FullName}");
    }

    public static bool Exists(SemVer version)
    {
        var file = GetFilePath(version);
        return file.Exists;
    }

    public static void Save(SemVer version, IReadOnlyList<BenchmarkEntry> entries)
    {
        if (entries.Any(e => e.Version != version))
        {
            throw new Exception($"Benchmark entries should match version {version}");
        }

        var file = GetFilePath(version);
        file.Directory?.Create();
        using var stream = file.Create();
        JsonSerializer.Serialize(stream, entries);
    }

    public static IOrderedEnumerable<SemVer> List()
    {
        var versions = new List<SemVer>();
        var directory = new DirectoryInfo(Config.Assets.BenchmarkDirectory);
        if (directory.Exists)
        {

            foreach (var file in directory.EnumerateFiles("*.json"))
            {
                try
                {
                    var version = SemVer.Parse(file.Name[0..^5]);
                    versions.Add(version);
                }
                catch { }
            }
        }

        return versions.Order();
    }

    private static FileInfo GetFilePath(SemVer version)
        => new(Path.Combine(Config.Assets.BenchmarkDirectory, $"{version}.json"));
}
