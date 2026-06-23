using CapriKit.Generators.HLSL.Parser;
using System.Collections.Immutable;

namespace CapriKit.Generators.HLSL;

internal sealed class ShaderIndex
{
    private readonly Dictionary<string, ShaderFile> Files;

    public ShaderIndex(ImmutableArray<(string Path, ShaderMetadata? Shader)> shaders)
    {
        Files = new Dictionary<string, ShaderFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, shader) in shaders)
        {
            if (shader != null)
            {
                var normalizedPath = NormalizePath(path);
                Files[normalizedPath] = new ShaderFile(normalizedPath, shader);
            }
        }
    }

    public StructScope CreateStructScope(string path, ShaderMetadata shader)
    {
        var normalizedPath = NormalizePath(path);
        if (!Files.TryGetValue(normalizedPath, out var current))
        {
            current = new ShaderFile(normalizedPath, shader);
        }

        var visibleFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            current.Path
        };
        var visibleFiles = new List<ShaderFile>
        {
            current
        };
        var includedFilesByPath = new Dictionary<string, IReadOnlyList<ShaderFile>>(StringComparer.OrdinalIgnoreCase);

        var currentIncludes = GetIncludedFiles(current);
        foreach (var include in currentIncludes)
        {
            if (visibleFilePaths.Add(include.Path))
            {
                visibleFiles.Add(include);
            }
        }
        includedFilesByPath[current.Path] = currentIncludes;

        foreach (var file in visibleFiles)
        {
            if (!includedFilesByPath.ContainsKey(file.Path))
            {
                includedFilesByPath[file.Path] = GetIncludedFiles(file);
            }
        }

        return new StructScope(current, currentIncludes, visibleFiles, includedFilesByPath);
    }

    private IReadOnlyList<ShaderFile> GetIncludedFiles(ShaderFile file)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            file.Path
        };
        var result = new List<ShaderFile>();
        CollectIncludedFiles(file, visited, result);
        return result;
    }

    private void CollectIncludedFiles(ShaderFile file, HashSet<string> visited, List<ShaderFile> result)
    {
        foreach (var include in file.Shader.Includes)
        {
            if (TryResolveLocalInclude(file, include, out var includedFile) && visited.Add(includedFile.Path))
            {
                CollectIncludedFiles(includedFile, visited, result);
                result.Add(includedFile);
            }
        }
    }

    private bool TryResolveLocalInclude(ShaderFile file, Include include, out ShaderFile includedFile)
    {
        includedFile = default!;
        if (include.Kind != IncludeKind.Local)
        {
            return false;
        }

        var directory = Path.GetDirectoryName(file.Path) ?? string.Empty;
        var includePath = NormalizePath(Path.Combine(directory, include.Path));
        return Files.TryGetValue(includePath, out includedFile);
    }

    internal static string NormalizePath(string path) => Path.GetFullPath(path);
}

internal sealed record ShaderFile(string Path, ShaderMetadata Shader);

internal sealed record ScopedStructure(string Path, Structure Structure);

internal sealed class StructScope
{
    public static StructScope Empty { get; } = FromLocalStructures([]);

    private readonly IReadOnlyList<ShaderFile> Files;
    private readonly Dictionary<string, IReadOnlyList<ShaderFile>> IncludedFilesByPath;

    public StructScope(
        ShaderFile currentFile,
        IReadOnlyList<ShaderFile> includedFiles,
        IReadOnlyList<ShaderFile> files,
        Dictionary<string, IReadOnlyList<ShaderFile>> includedFilesByPath)
    {
        CurrentFile = currentFile;
        IncludedFiles = includedFiles;
        Files = files;
        IncludedFilesByPath = includedFilesByPath;
    }

    public ShaderFile CurrentFile { get; }

    public IReadOnlyList<ShaderFile> IncludedFiles { get; }

    public static StructScope FromLocalStructures(IReadOnlyList<Structure> structures)
    {
        var metadata = new ShaderMetadata([], [], structures, [], []);
        var file = new ShaderFile(string.Empty, metadata);
        return new StructScope(file, [], [file], new Dictionary<string, IReadOnlyList<ShaderFile>>
        {
            [file.Path] = []
        });
    }

    public ScopedStructure GetDefinition(Structure structure)
    {
        foreach (var file in Files)
        {
            foreach (var candidate in file.Shader.Structures)
            {
                if (ReferenceEquals(candidate, structure))
                {
                    return new ScopedStructure(file.Path, candidate);
                }
            }
        }

        return new ScopedStructure(CurrentFile.Path, structure);
    }

    public bool TryResolve(string hlslType, string originPath, out ScopedStructure structure)
    {
        if (TryResolveInFile(originPath, hlslType, out structure))
        {
            return true;
        }

        if (IncludedFilesByPath.TryGetValue(originPath, out var includedFiles))
        {
            foreach (var file in includedFiles)
            {
                if (TryResolveInFile(file.Path, hlslType, out structure))
                {
                    return true;
                }
            }
        }

        structure = default!;
        return false;
    }

    private bool TryResolveInFile(string path, string hlslType, out ScopedStructure structure)
    {
        foreach (var file in Files)
        {
            if (!string.Equals(file.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var candidate in file.Shader.Structures)
            {
                if (candidate.Name == hlslType)
                {
                    structure = new ScopedStructure(file.Path, candidate);
                    return true;
                }
            }
        }

        structure = default!;
        return false;
    }
}
