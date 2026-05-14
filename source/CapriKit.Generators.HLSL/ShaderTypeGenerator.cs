using Microsoft.CodeAnalysis;

namespace CapriKit.Generators.HLSL;

[Generator]
public class ShaderTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var shaders = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase));
        var provider = shaders.Select(static (text, cancellationToken) => (text.Path, text.GetText(cancellationToken)));

        // TODO: We need to handle includes but right now we only have a list of files
        // parsing files is expensive and we need to be careful that it does not lead
        // to all shader types being regenerated because the incremental values provider
        // loses track of which files are generated from which inputs.
        // There is also the question of how includes should be namespaced. (If we should
        // because technically an include means the entire text is just copied into the file).        

        context.RegisterSourceOutput(provider, static (outputContext, file) =>
        {
            var (path, text) = file;
            if (ShaderClassGenerator.TryGenerateShader(text, out var result))
            {
                outputContext.AddSource(path, result);
            }
        });
    }
}
