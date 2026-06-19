using CapriKit.Generators.HLSL.Parser;
using static CapriKit.Generators.HLSL.SourceCodeUtils;

namespace CapriKit.Generators.HLSL;

/// <summary>
/// Generates a default input element description for a struct marked with <c>#pragma Input</c>.
/// Note that the shader source code alone does not give enough information to create a perfect one.
/// So users need an option to provide their own one if they do things like packing the elements of a
/// float4 in a R8G8B8A8_UNorm or if they are reading from more than 1 vertex buffer.
/// </summary>
internal static class InputElementDescriptionBuilder
{
    private const string InputElementDescriptionArrayType = "Vortice.Direct3D11.InputElementDescription[]";
    private const string PerVertexDataInputClassification = "Vortice.Direct3D11.InputClassification.PerVertexData";

    public static void WriteInputElementDescription(SourceCodeBuilder builder, StructTranslator translator, Structure @struct)
    {
        var name = $"{CreateValidTypeIdentifier(@struct.Name)}ElementDescription";
        builder.WriteField(Modifiers.Public | Modifiers.Static | Modifiers.ReadOnly,
            InputElementDescriptionArrayType, name, b => WriteArrayContents(b, translator, @struct));
    }

    private static void WriteArrayContents(SourceCodeBuilder builder, StructTranslator translator, Structure @struct)
    {
        builder.WriteRaw($"new {InputElementDescriptionArrayType}\r\n");
        builder.WriteLine("{");
        var dotNetStruct = translator.LayoutStruct(@struct);
        for (var i = 0; i < @struct.Members.Count; i++)
        {
            var member = @struct.Members[i];

            var (semantic, semanticIndex) = SplitSemanticInTextAndIndex(member.Semantic);
            var format = TypeTranslator.GetFormat(member.Type); // use the HLSL type here
            var offset = dotNetStruct.Members[i].Offset;
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
}
