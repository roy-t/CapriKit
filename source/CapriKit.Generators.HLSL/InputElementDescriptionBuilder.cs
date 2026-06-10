using CapriKit.Generators.HLSL.Parser;
using static CapriKit.Generators.HLSL.SourceCodeUtils;

namespace CapriKit.Generators.HLSL;

/// <summary>
/// Generates a default input element description, note that only the shader source code
/// does not give enough information to create a perfect one. So users need an option
/// to provide their own one if they do things like packing the elements of a float4 in a
/// R8G8B8A8_UNorm or if they are reading from more than 1 vertex buffer.
/// </summary>
internal static class InputElementDescriptionBuilder
{
    private const string InputElementDescriptionArrayType = "Vortice.Direct3D11.InputElementDescription[]";
    private const string PerVertexDataInputClassification = "Vortice.Direct3D11.InputClassification.PerVertexData";

    public static void WriteInputElementDescription(SourceCodeBuilder builder, EntryPoint entryPoint, IReadOnlyList<Structure> structs)
    {
        if (entryPoint.Kind != EntryPointKind.VertexShader)
            return;

        // Assume the first custom structure with at least one user semantic is the complete vertex shader input .
        Structure? target = default;
        foreach (var argument in entryPoint.Arguments)
        {
            var @struct = structs.FirstOrDefault(s => s.Name.Equals(argument.Type, StringComparison.OrdinalIgnoreCase));
            if (@struct != default && @struct.Members.Any(m => IsUserSemantic(m.Semantic)))
            {
                target = @struct;
            }
        }

        if (target == null)
            return;

        var name = $"{CreateValidTypeIdentifier(entryPoint.Name)}InputElementDescription";
        builder.WriteField(Modifiers.Public | Modifiers.Static | Modifiers.ReadOnly,
            InputElementDescriptionArrayType, name, b => WriteArrayContents(b, target));
    }

    private static void WriteArrayContents(SourceCodeBuilder builder, Structure @struct)
    {
        builder.WriteRaw($"new {InputElementDescriptionArrayType}\r\n");
        builder.WriteLine("{");
        var memberLayout = StructBuilder.LayOutStructure(@struct.Members);
        foreach (var layout in memberLayout)
        {
            var member = layout.Member;
            var (semantic, semanticIndex) = SplitSemanticInTextAndIndex(member.Semantic);
            var format = TypeTranslator.GetFormat(member.Type); // use the HLSL type here
            var offset = layout.Offset ?? 0u;
            var slot = 0u;
            var classification = PerVertexDataInputClassification;
            var stepRate = 0u;
            builder.WriteLine($"    new({ToLiteral(semantic)}, {ToLiteral(semanticIndex)}, {format}, {ToLiteral(offset)}, {ToLiteral(slot)}, {classification}, {ToLiteral(stepRate)}),");
        }

        builder.WriteLine("};");
    }

    private static (string, uint) SplitSemanticInTextAndIndex(string semantic)
    {
        var split = semantic.Length;
        while (split > 0 && char.IsDigit(semantic[split - 1]))
        {
            split--;
        }

        var text = semantic.Substring(0, split);
        var index = split < semantic.Length ? uint.Parse(semantic.Substring(split)) : 0u;
        return (text, index);
    }

    private static bool IsUserSemantic(string semantic)
    {
        return !string.IsNullOrWhiteSpace(semantic) && !semantic.StartsWith("SV_");
    }
}
