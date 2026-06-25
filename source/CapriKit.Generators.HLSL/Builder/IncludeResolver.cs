using CapriKit.Generators.HLSL.Parser;
using System.Collections.Immutable;

namespace CapriKit.Generators.HLSL.Builder;

internal sealed class IncludeResolver
{
    private readonly Dictionary<string, ShaderMetadata> Shaders;

    public IncludeResolver(ImmutableArray<(string, ShaderMetadata?)> shaders)
    {
        Shaders = [];
        foreach (var (path, shader) in shaders)
        {
            if (!string.IsNullOrEmpty(path) && shader != null)
            {
                Shaders.Add(NormalizePath(path), shader);
            }
        }
    }

    /// <summary>
    /// Retrieves all files directly and indirectly included in the given shader, returned in the order that they should be resolved.
    /// </summary>
    public IReadOnlyList<(string, ShaderMetadata)> GetIncludedFiles(string path, ShaderMetadata shader)
    {
        var seen = new HashSet<string>();
        var ordered = new List<(string, ShaderMetadata)>();
        CollectIncludedFiles(seen, ordered, path, shader);

        return ordered;
    }

    private void CollectIncludedFiles(HashSet<string> seen, List<(string, ShaderMetadata)> ordered, string key, ShaderMetadata metadata)
    {
        foreach (var include in metadata.Includes)
        {
            // seen.Add returns false once a file has been visited, which breaks include cycles
            // and collapses diamonds to a single entry. Recurse before adding so that a file is
            // always emitted after the files it depends on.
            if (TryResolveInclude(key, include, out var found) && seen.Add(found.path))
            {
                CollectIncludedFiles(seen, ordered, found.path, found.metadata);
                ordered.Add(found);
            }
        }
    }


    private bool TryResolveInclude(string shaderPath, Include include, out (string path, ShaderMetadata metadata) shader)
    {
        if (include.Kind != IncludeKind.Local)
        {
            shader = default;
            return false;
        }

        var directory = Path.GetDirectoryName(shaderPath);
        var includePath = NormalizePath(Path.Combine(directory, include.Path));
        if (Shaders.TryGetValue(includePath, out var value))
        {
            shader = (includePath, value);
            return true;
        }

        shader = default;
        return false;
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }
}
