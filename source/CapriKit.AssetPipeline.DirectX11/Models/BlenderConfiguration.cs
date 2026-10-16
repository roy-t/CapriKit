using CapriKit.IO;
using Microsoft.Win32;
using System.Diagnostics;

namespace CapriKit.AssetPipeline.DirectX11.Models;

/// <summary>
/// Location of the Blender installation that is used to run export scripts.
/// </summary>
public sealed record BlenderConfiguration(DirectoryPath PathToBlenderDirectory)
{
    private const string HomeVariable = "BLENDER_HOME";
    private static readonly FilePath Executable = new("blender.exe");

    public FilePath PathToBlenderExecutable => PathToBlenderDirectory.Append(Executable);

    /// <summary>
    /// Uses the folder in the <c>BLENDER_HOME</c> environment variable when it is set. Otherwise returns
    /// the newest Blender found in the Windows uninstall registry (MSI and Steam installs),
    /// <c>Program Files\Blender Foundation</c> and the <c>PATH</c>.
    /// </summary>
    /// <exception cref="FileNotFoundException">When no Blender executable was found.</exception>
    public static BlenderConfiguration AutoDetect()
    {
        var home = Environment.GetEnvironmentVariable(HomeVariable);
        if (!string.IsNullOrWhiteSpace(home))
        {
            var configuration = new BlenderConfiguration(home);
            if (!File.Exists(configuration.PathToBlenderExecutable))
            {
                throw new FileNotFoundException($"{HomeVariable} is set to '{home}' but that folder does not contain {Executable}", configuration.PathToBlenderExecutable);
            }
            return configuration;
        }

        var latest = FindRegistryInstalls()
            .Concat(FindProgramFilesInstalls())
            .Concat(FindPathInstalls())
            .Where(folder => !string.IsNullOrWhiteSpace(folder))
            .Select(folder => Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(folder => File.Exists(Path.Combine(folder, Executable)))
            .MaxBy(folder => GetVersion(Path.Combine(folder, Executable)));

        return latest != null
            ? new BlenderConfiguration(latest)
            : throw new FileNotFoundException($"Could not find Blender, set {HomeVariable} to the folder that contains {Executable}");
    }

    private static IEnumerable<string> FindRegistryInstalls()
    {
        // Blender is 64-bit only, so the WOW6432Node uninstall keys are skipped
        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var uninstall = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstall == null)
            {
                continue;
            }

            foreach (var name in uninstall.GetSubKeyNames())
            {
                using var app = uninstall.OpenSubKey(name);
                if (app?.GetValue("DisplayName") is string displayName &&
                    displayName.StartsWith("Blender", StringComparison.OrdinalIgnoreCase) &&
                    app.GetValue("InstallLocation") is string location)
                {
                    yield return location;
                }
            }
        }
    }

    private static IEnumerable<string> FindProgramFilesInstalls()
    {
        var foundation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Blender Foundation");
        return Directory.Exists(foundation) ? Directory.EnumerateDirectories(foundation) : [];
    }

    private static IEnumerable<string> FindPathInstalls()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static Version GetVersion(string executable)
    {
        try
        {
            // FileVersion is only "major.minor", the individual parts also contain the patch number
            var info = FileVersionInfo.GetVersionInfo(executable);
            return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart);
        }
        catch (Exception)
        {
            // App execution aliases (Microsoft Store) are reparse points without version information
            return new Version(0, 0, 0);
        }
    }
}
