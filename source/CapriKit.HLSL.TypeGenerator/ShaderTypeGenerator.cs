using Microsoft.CodeAnalysis;

namespace CapriKit.HLSL.TypeGenerator;

[Generator]
public class ShaderTypeGenerator
{
    // TODO: find entrypoints based on convention. For example any methods that ends with _vs is a vertex shader entrypoint
    // TODO: compile shaders without any optimizations for fast turn-around
    // TODO: use test tool project for testing. Any AdditionalFile that ends with hlsl is fair game. Files without an entrypoint are probably include files and can be ignored (they will be included later)
    // TODO: figure out how to generate types for includes files only once. Maybe by figuring out an EQUALS for structs? Use 'nested types' for structs that are used only once or conflict. Use a bigger namespace for shared structs.
}
