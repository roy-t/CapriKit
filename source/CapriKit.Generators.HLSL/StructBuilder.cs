using CapriKit.Generators.HLSL.Parser;
using System.Text;
using static CapriKit.Generators.HLSL.SourceCodeUtils;

namespace CapriKit.Generators.HLSL;

/// <summary>
/// Creates a C# struct that has the same memory layout as the given HLSL struct.
/// </summary>
internal static class StructBuilder
{
    private const string ExplicitLayoutKind = "System.Runtime.InteropServices.LayoutKind.Explicit";
    private const string SequentialLayoutKind = "System.Runtime.InteropServices.LayoutKind.Sequential";

    /// <summary>
    /// Creates a struct that follows the explicit layout rules for constant buffers with
    /// C# types that map to the corresponding HLSL types.
    /// Fixed size arrays are supported via helper structs that are padded to 16 bytes, placed
    /// in an <c>[InlineArray]</c>.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-packing-rules"/>
    public static void WriteStruct(SourceCodeBuilder builder, StructTranslator translator, ConstantBuffer buffer)
    {
        var dotNetStruct = translator.LayoutConstantBuffer(buffer);
        WriteStruct(builder, dotNetStruct);
    }

    /// <summary>
    /// Creates sequentially laid out struct with C# types that map to the corresponding HLSL types.
    /// Fixed size arrays are supported via <c>[InlineArray]</c>.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-struct"/>
    public static void WriteStruct(SourceCodeBuilder builder, StructTranslator translator, Structure @struct)
    {
        var dotNetStruct = translator.LayoutStruct(@struct);
        WriteStruct(builder, dotNetStruct);
    }

    /// <summary>
    /// Writes a struct and the <c>[InlineArray]</c> helper structs its array
    /// members need. The helpers are emitted as siblings of the struct.
    /// </summary>
    private static void WriteStruct(SourceCodeBuilder builder, DotNetStruct @struct)
    {
        builder.WriteAttribute("System.Runtime.InteropServices.StructLayout", ExplicitLayoutKind, $"Size = {@struct.Type.Size}");
        builder.OpenStruct(Modifiers.Public, @struct.Type.Name);

        foreach (var member in @struct.Members)
        {
            WriteMemberField(builder, @struct.Type.Name, member);
        }

        builder.CloseBlock();

        foreach (var member in @struct.Members.Where(m => m.ElementCount > 1))
        {
            WriteArrayType(builder, @struct.Type.Name, member);
        }
    }
   
    private static void WriteMemberField(SourceCodeBuilder builder, string ownerName, DotNetStructMember member)
    {
        builder.WriteSummaryComment(member.Documentation);
        builder.WriteAttribute("System.Runtime.InteropServices.FieldOffset", ToLiteral(member.Offset));
        var type = member.ElementCount > 1 ? ArrayStructName(ownerName, member.Name) : member.Type.Name;
        builder.WriteField(Modifiers.Public, type, member.Name);
    }

    /// <summary>
    /// Emits the helper structs that back an array member: an <c>[InlineArray]</c>
    /// of the element type. When <see cref="Layout.Stride"/> is set the
    /// element is first wrapped in a struct padded to that size, giving the array
    /// the 16-byte stride that constant buffers require.
    /// </summary>
    private static void WriteArrayType(SourceCodeBuilder builder, string ownerName, DotNetStructMember member)
    {
        var inlineElement = member.Type.Name;

        if (member.Type.Size != member.Stride)
        {
            var elementName = ElementStructName(ownerName, member.Name);
            builder.WriteAttribute(
                "System.Runtime.InteropServices.StructLayout",
                "System.Runtime.InteropServices.LayoutKind.Sequential",
                $"Size = {member.Stride}");
            builder.OpenStruct(Modifiers.Public, elementName);
            builder.WriteField(Modifiers.Public, member.Type.Name, "Value");
            builder.CloseBlock();
            inlineElement = elementName;
        }

        builder.WriteAttribute("System.Runtime.CompilerServices.InlineArray", member.ElementCount.ToString());
        builder.OpenStruct(Modifiers.Public, ArrayStructName(ownerName, member.Name));
        builder.WriteField(Modifiers.Private, inlineElement, "element0");
        builder.CloseBlock();
    }

    private static void WriteFieldSummary(SourceCodeBuilder builder, Member member)
    {
        var summary = new StringBuilder();
        if (!string.IsNullOrEmpty(member.Semantic))
        {
            summary.AppendLine($"Semantic: {member.Semantic}");
        }
        if (member.Dimensions.Count > 0)
        {
            summary.AppendLine($"Dimensions: [{string.Join("][", member.Dimensions)}]");
        }
        if (member.Modifiers.Count > 0)
        {
            summary.AppendLine($"Modifiers: {string.Join(", ", member.Modifiers)}");
        }
        if (summary.Length > 0)
        {
            builder.WriteSummaryComment(summary.ToString());
        }
    }

    private static string ElementStructName(string ownerName, string memberName) => $"{ownerName}{memberName}Element";

    private static string ArrayStructName(string ownerName, string memberName) => $"{ownerName}{memberName}Array";

    /// <summary>Multidimensional HLSL arrays are flattened to a single element count. While regular fields are count 1</summary>
    private static uint Flatten(IReadOnlyList<uint> dimensions)
    {
        if (dimensions.Any())
        {
            return dimensions.Aggregate(1u, (a, b) => a * b);
        }
        return 1;
    }

    /// <summary>Rounds up to the next multiple of 16.</summary>
    private static uint Align16(uint value) => (value + 15u) & ~15u;

    internal sealed record Layout(
        Member Member,
        string DotNetType,
        uint ElementCount,
        // Offset from the start of the struct
        uint? Offset = null,
        // Size to allocate for this member in the struct, potentially larger than the size this member actually requires
        // to accomplish alignment on certain byte-multiples in arrays.
        uint? Stride = null)
    {
        public bool IsArray => ElementCount > 1;
    }
}
