using CapriKit.Generators.HLSL.Parser;
using System.Text;
using static CapriKit.Generators.HLSL.SourceCodeUtils;

namespace CapriKit.Generators.HLSL;


/// <summary>
/// Writes an HLSL struct as an equivalent .NET struct.
/// </summary>
public static class StructBuilder
{
    public static void WriteStruct(SourceCodeBuilder builder, Structure @struct)
    {
        var modifiers = Modifiers.Public;
        if (@struct.Members.Any(m => m.Dimensions.Any()))
        {
            // Fixed-size buffers may only be used inside an unsafe context.
            modifiers |= Modifiers.Unsafe;
        }

        builder.WriteAttribute("System.Runtime.InteropServices.StructLayout", "System.Runtime.InteropServices.LayoutKind.Sequential");
        builder.OpenStruct(modifiers, CreateValidIdentifier(@struct.Name));

        foreach (var member in @struct.Members)
        {
            var type = TypeTranslator.Translate(member.Type, member.Dimensions);
            WriteField(builder, member, type);
        }

        builder.CloseBlock();
    }

    private static void WriteField(SourceCodeBuilder builder, Member member, TranslatedType type)
    {
        var summaryBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(member.Semantic))
        {
            summaryBuilder.AppendLine($"Original semantic: {member.Semantic}");
        }

        if (type.OriginalDimensions.Count > 1)
        {
            summaryBuilder.AppendLine($"Original dimensions: [{string.Join("][", type.OriginalDimensions)}]");
        }

        if (summaryBuilder.Length > 0)
        {
            builder.WriteSummaryComment(summaryBuilder.ToString());
        }

        var name = CreateValidIdentifier(member.Name);
        if (type.IsFixed)
        {
            builder.WriteFixedField(Modifiers.Public, type.DotNetType, name, type.FixedSize);
        }
        else
        {
            builder.WriteField(Modifiers.Public, type.DotNetType, name);
        }
    }
}
