using CapriKit.Generators.HLSL.Parser;
using System.Collections.Immutable;

namespace CapriKit.Generators.HLSL;

// TODO: Lacking tests
internal sealed class StructBookKeeper
{
    private record ShaderIdentity(string Path, ShaderMetadata Shader);

    private readonly GeneratorConfiguration Config;
    private readonly Dictionary<string, ShaderIdentity> Lookup;

    public StructBookKeeper(ImmutableArray<(string, ShaderMetadata?)> shaders, GeneratorConfiguration config)
    {
        Config = config;
        Lookup = [];
        foreach (var (path, shader) in shaders)
        {
            if (shader != null)
            {
                var identity = GetIdentity(path, config);
                Lookup.Add(identity, new ShaderIdentity(path, shader));
            }
        }
    }
    public void RegisterStructs(StructTranslator translator, string path, ShaderMetadata shader)
    {
        // By having one StructBookKeeper, but many StructTranslators that have to do
        // this registration step, we might do a lot of double work. But it saves StructTranslator
        // needing to know what includes are. Basically this class keeps track of what is in scope.
        var current = new ShaderIdentity(path, shader);
        RegisterStructs(translator, current);
    }

    private void RegisterStructs(StructTranslator translator, ShaderIdentity current)
    {
        foreach (var include in current.Shader.Includes)
        {
            if (include.Kind == IncludeKind.Local)
            {
                var directory = Path.GetDirectoryName(current.Path);
                var includePath = Path.Combine(directory, include.Path);
                var identity = GetIdentity(includePath, Config);

                if (Lookup.TryGetValue(identity, out var parent))
                {
                    RegisterStructs(translator, parent);
                }
            }
        }

        foreach (var @struct in current.Shader.Structures)
        {
            translator.LayoutStruct(@struct);
        }
    }

    private static string GetIdentity(string path, GeneratorConfiguration config)
    {
        var @namespace = ShaderClassBuilder.GetNamespace(path, config);
        var @class = ShaderClassBuilder.GetClassName(path);
        return $"{@namespace}.{@class}";
    }
}
