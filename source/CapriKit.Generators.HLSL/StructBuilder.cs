using CapriKit.Generators.HLSL.Parser;
using System.Text;
using static CapriKit.Generators.HLSL.SourceCodeUtils;

namespace CapriKit.Generators.HLSL;

/// <summary>
/// Writes an HLSL struct as an equivalent .NET struct.
/// </summary>
public static class StructBuilder
{
    /// <summary>
    /// Creates a struct that follows the packing rules for constant variables.
    /// The struct uses an explicit layout so every member lands on the same byte
    /// offset the GPU expects. Array members get their own helper structs so each
    /// element occupies its own 16-byte register, as HLSL requires.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-packing-rules"/>
    public static void WriteStruct(SourceCodeBuilder builder, ConstantBuffer buffer)
    {
        var fields = LayOutConstantBuffer(buffer.Members, out var sizeInBytes);
        EmitStruct(builder, CreateValidIdentifier(buffer.Name),
            "System.Runtime.InteropServices.LayoutKind.Explicit", sizeInBytes, fields);
    }

    /// <summary>
    /// Creates a struct that mirrors a regular HLSL struct. Unlike a constant
    /// buffer there are no padding rules: members and array elements are stored
    /// back-to-back. Array members become <c>[InlineArray]</c> helper structs.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-struct"/>
    public static void WriteStruct(SourceCodeBuilder builder, Structure @struct)
    {
        var fields = LayOutStructure(@struct.Members);
        EmitStruct(builder, CreateValidIdentifier(@struct.Name),
            "System.Runtime.InteropServices.LayoutKind.Sequential", explicitSize: null, fields);
    }

    /// <summary>
    /// A member placed in a struct, with the information needed to declare it.
    /// <c>Offset</c> is the explicit byte offset, or <c>null</c> for sequential layout.
    /// <c>PaddedStride</c>, when set, wraps array elements in a struct padded to that
    /// many bytes (constant-buffer packing); when <c>null</c> elements are stored directly.
    /// </summary>
    private sealed record FieldLayout(
        Member Member,
        string ElementType,
        bool IsArray,
        uint ElementCount,
        uint? Offset,
        uint? PaddedStride);

    /// <summary>
    /// Writes a struct and the <c>[InlineArray]</c> helper structs its array
    /// members need. The helpers are emitted as siblings of the struct.
    /// </summary>
    private static void EmitStruct(
        SourceCodeBuilder builder, string name, string layoutKind, uint? explicitSize,
        IReadOnlyList<FieldLayout> fields)
    {
        var layout = explicitSize is uint size
            ? new[] { layoutKind, $"Size = {size}" }
            : [layoutKind];
        builder.WriteAttribute("System.Runtime.InteropServices.StructLayout", layout);
        builder.OpenStruct(Modifiers.Public, name);

        foreach (var field in fields)
        {
            WriteMemberField(builder, name, field);
        }

        builder.CloseBlock();

        foreach (var field in fields.Where(f => f.IsArray))
        {
            WriteArrayType(builder, name, field);
        }
    }

    /// <summary>Assigns every member a byte offset following the HLSL packing rules.</summary>
    private static List<FieldLayout> LayOutConstantBuffer(IReadOnlyList<Member> members, out uint sizeInBytes)
    {
        var fields = new List<FieldLayout>(members.Count);
        var offset = 0u;

        foreach (var member in members)
        {
            var size = TypeTranslator.GetSizeInBytes(member.Type);
            var elementType = TypeTranslator.Translate(member.Type, []).DotNetType;

            if (member.Dimensions.Count > 0)
            {
                // Arrays start a new register and each element is padded to 16 bytes.
                var count = Flatten(member.Dimensions);
                var stride = Align16(size);
                offset = Align16(offset);
                fields.Add(new FieldLayout(member, elementType, true, count, offset, stride));
                offset += stride * count;
            }
            else
            {
                // A scalar or vector may not straddle a 16-byte boundary.
                if (offset % 16 + size > 16)
                {
                    offset = Align16(offset);
                }
                fields.Add(new FieldLayout(member, elementType, false, 1, offset, null));
                offset += size;
            }
        }

        sizeInBytes = Align16(offset);
        return fields;
    }

    /// <summary>Lays out members back-to-back, the way a regular HLSL struct is packed.</summary>
    private static List<FieldLayout> LayOutStructure(IReadOnlyList<Member> members)
    {
        var fields = new List<FieldLayout>(members.Count);

        foreach (var member in members)
        {
            var elementType = TypeTranslator.Translate(member.Type, []).DotNetType;
            var isArray = member.Dimensions.Count > 0;
            var count = isArray ? Flatten(member.Dimensions) : 1u;
            fields.Add(new FieldLayout(member, elementType, isArray, count, Offset: null, PaddedStride: null));
        }

        return fields;
    }

    private static void WriteMemberField(SourceCodeBuilder builder, string ownerName, FieldLayout field)
    {
        WriteFieldSummary(builder, field.Member);

        if (field.Offset is uint offset)
        {
            builder.WriteAttribute("System.Runtime.InteropServices.FieldOffset", offset.ToString());
        }

        var name = CreateValidIdentifier(field.Member.Name);
        var type = field.IsArray ? ArrayStructName(ownerName, name) : field.ElementType;
        builder.WriteField(Modifiers.Public, type, name);
    }

    /// <summary>
    /// Emits the helper structs that back an array member: an <c>[InlineArray]</c>
    /// of the element type. When <see cref="FieldLayout.PaddedStride"/> is set the
    /// element is first wrapped in a struct padded to that size, giving the array
    /// the 16-byte stride that constant buffers require.
    /// </summary>
    private static void WriteArrayType(SourceCodeBuilder builder, string ownerName, FieldLayout field)
    {
        var name = CreateValidIdentifier(field.Member.Name);
        var inlineElement = field.ElementType;

        if (field.PaddedStride is uint stride)
        {
            var elementName = ElementStructName(ownerName, name);
            builder.WriteAttribute(
                "System.Runtime.InteropServices.StructLayout",
                "System.Runtime.InteropServices.LayoutKind.Sequential",
                $"Size = {stride}");
            builder.OpenStruct(Modifiers.Public, elementName);
            builder.WriteField(Modifiers.Public, field.ElementType, "Value");
            builder.CloseBlock();
            inlineElement = elementName;
        }

        builder.WriteAttribute("System.Runtime.CompilerServices.InlineArray", field.ElementCount.ToString());
        builder.OpenStruct(Modifiers.Public, ArrayStructName(ownerName, name));
        builder.WriteField(Modifiers.Private, inlineElement, "element0");
        builder.CloseBlock();
    }

    private static void WriteFieldSummary(SourceCodeBuilder builder, Member member)
    {
        var summary = new StringBuilder();
        if (!string.IsNullOrEmpty(member.Semantic))
        {
            summary.AppendLine($"Original semantic: {member.Semantic}");
        }
        if (member.Dimensions.Count > 0)
        {
            summary.AppendLine($"Original dimensions: [{string.Join("][", member.Dimensions)}]");
        }
        if (summary.Length > 0)
        {
            builder.WriteSummaryComment(summary.ToString());
        }
    }

    private static string ElementStructName(string ownerName, string memberName) => $"{ownerName}{memberName}Element";

    private static string ArrayStructName(string ownerName, string memberName) => $"{ownerName}{memberName}Array";

    /// <summary>Multidimensional HLSL arrays are flattened to a single element count.</summary>
    private static uint Flatten(IReadOnlyList<uint> dimensions) => dimensions.Aggregate(1u, (a, b) => a * b);

    /// <summary>Rounds <paramref name="value"/> up to the next multiple of 16.</summary>
    private static uint Align16(uint value) => (value + 15u) & ~15u;
}
