using CapriKit.Meta.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CapriKit.Meta.Commands;

internal sealed class BumpCommand : Command<BumpCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Increase the major version of the package")]
        [CommandOption("--major <MAJOR>")]
        public bool Major { get; init; }

        [Description("Increase the minor version of the package")]
        [CommandOption("--minor <MINOR>")]
        public bool Minor { get; init; }

        [Description("Increase the patch version of the package")]
        [CommandOption("--patch <PATCH>")]
        public bool Patch { get; init; }

        [Description("Set the prerelease information. A prerelease version MAY be denoted by appending a hyphen and a series of dot separated identifiers immediately following the patch version.Identifiers MUST comprise only ASCII alphanumerics and hyphens [[0-9A-Za-z-]]. Identifiers MUST NOT be empty.Numeric identifiers MUST NOT include leading zeroes.Pre-release versions have a lower precedence than the associated normal version.A pre-release version indicates that the version is unstable and might not satisfy the intended compatibility requirements as denoted by its associated normal version.Examples: 1.0.0-alpha, 1.0.0-alpha.1, 1.0.0-0.3.7, 1.0.0-x.7.z.92, 1.0.0-x-y-z.--.")]
        [CommandOption("--pre <PRERELEASE>")]
        public string? Prerelease { get; init; }

        [Description("Set the build metadata. Build metadata MAY be denoted by appending a plus sign and a series of dot separated identifiers immediately following the patch or pre-release version. Identifiers MUST comprise only ASCII alphanumerics and hyphens [[0-9A-Za-z-]]. Identifiers MUST NOT be empty. Build metadata MUST be ignored when determining version precedence. Thus two versions that differ only in the build metadata, have the same precedence. Examples: 1.0.0-alpha+001, 1.0.0+20130313144700, 1.0.0-beta+exp.sha.5114f85, 1.0.0+21AF26D3----117B344092BD.")]
        [CommandOption("--build <BUILD>")]
        public string? BuildMetaData { get; init; }
    }

    public override int Execute(CommandContext context, Settings bump)
    {
        // Find the path to the .git directory
        var rootDirectory = FileSearchUtilities.SearchDirectoryUp(".git", Environment.CurrentDirectory)
            .FirstOrDefault() ?? throw new Exception($"Not a git repository: {Environment.CurrentDirectory}");

        // The root directory of the repository, that contains the .git directory is one level "up"
        // this is also where the version file should be
        var path = Path.GetFullPath(Path.Combine(rootDirectory, "..", "version.txt"));

        if (File.Exists(path))
        {
            var text = File.ReadAllText(path).Trim();
            var oldVersion = SemVer.Parse(text);
            var newVersion = UpdateVersion(oldVersion, bump);

            WriteVersionToFile(path, newVersion);
            AnsiConsoleExt.InfoMarkupLineInterpolated($"Updating [gray]({path})[/]: [bold gray]{oldVersion}[/] -> [bold green]{newVersion}[/]");
        }
        else
        {
            var version = new SemVer(0, 1, 0);
            WriteVersionToFile(path, version);
            AnsiConsoleExt.WarningMarkupLineInterpolated($"[orangered1]Creating[/] [gray]({path})[/]: [bold gray]{version}[/]");
        }

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (!context.Remaining.Raw.Any())
        {
            return ValidationResult.Error("Command requires at least one option");
        }
        
        return base.Validate(context, settings);
    }

    private static SemVer UpdateVersion(SemVer version, Settings bump)
    {
        if (bump.Major)
        {
            version = version.BumpMajor();
        }

        if (bump.Minor)
        {
            version = version.BumpMinor();
        }

        if (bump.Patch)
        {
            version = version.BumpPatch();
        }

        if (!string.IsNullOrEmpty(bump.Prerelease))
        {
            version = version.WithPreReleaseData(bump.Prerelease);
        }

        if (!string.IsNullOrEmpty(bump.Prerelease))
        {
            version = version.WithBuildMetaData(bump.BuildMetaData);
        }

        return version;
    }

    // TODO: msbuild and NuGet do not support SEMVER, so store version without the prerelease and buildmetadata and use those somewhere else?
    private static void WriteVersionToFile(string path, SemVer version)
    {
        File.WriteAllText(path, version.ToString());
    }
}
