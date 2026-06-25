using CapriKit.Generators.HLSL.Parser;
using static CapriKit.Generators.HLSL.Builder.SourceCodeUtils;

namespace CapriKit.Generators.HLSL.Builder;

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

    public static void WriteInputElementDescription(SourceCodeBuilder builder, StructMetadata meta)
    {
        var name = $"{CreateValidTypeIdentifier(meta.Type.Name)}ElementDescription";
        builder.WriteField(Modifiers.Public | Modifiers.Static | Modifiers.ReadOnly,
            InputElementDescriptionArrayType, name, b => WriteArrayContents(b, meta));
    }

    private static void WriteArrayContents(SourceCodeBuilder builder, StructMetadata meta)
    {
        builder.WriteRaw($"new {InputElementDescriptionArrayType}\r\n");
        builder.WriteLine("{");
        for (var i = 0; i < meta.Members.Count; i++)
        {
            var member = meta.Members[i];
            var semantic = member.Semantic;
            var semanticIndex = member.SemanticIndex;
            var format = member.Format;
            var offset = member.Offset;
            var slot = 0u;
            var classification = PerVertexDataInputClassification;
            var stepRate = 0u;
            builder.WriteLine($"    new({ToLiteral(semantic)}, {ToLiteral(semanticIndex)}, {format}, {ToLiteral(offset)}, {ToLiteral(slot)}, {classification}, {ToLiteral(stepRate)}),");
        }

        builder.WriteLine("};");
    }
}
