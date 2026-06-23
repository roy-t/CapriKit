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
    /// All files directly and indirectly included in the given shader, returned in the order that they should be resolved.
    /// </summary>    
    public IReadOnlyList<(string, ShaderMetadata)> GetIncludedFiles(string path, ShaderMetadata shader)
    {
        var seen = new HashSet<string>();
        var stack = new Stack<(string, ShaderMetadata)>();
        CollectIncludedFiles(seen, stack, path, shader);

        return stack.ToList();
    }

    private void CollectIncludedFiles(HashSet<string> seen, Stack<(string, ShaderMetadata)> stack, string key, ShaderMetadata metadata)
    {
        foreach (var include in metadata.Includes)
        {
            if (TryResolveInclude(key, include, out var found))
            {
                seen.Add(found.path);
                stack.Push(found);
                CollectIncludedFiles(seen, stack, found.path, found.metadata);
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
