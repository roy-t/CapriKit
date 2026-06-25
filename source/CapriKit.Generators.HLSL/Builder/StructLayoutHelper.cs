using CapriKit.Generators.HLSL.Parser;
using System.Text;

namespace CapriKit.Generators.HLSL.Builder;

internal static class StructLayoutHelper
{
    public static string ElementStructName(string ownerName, string memberName) => $"{ownerName}{memberName}Element";
    public static string ArrayStructName(string ownerName, string memberName) => $"{ownerName}{memberName}Array";

    public static string DocumentMember(Member member)
    {
        var summary = new StringBuilder();
        summary.AppendLine($"Original Name: {member.Name}");
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

        return summary.ToString();
    }

    /// <summary>Multidimensional HLSL arrays are flattened to a single element count. While regular fields are count 1</summary>
    public static uint Flatten(IReadOnlyList<uint> dimensions)
    {
        if (dimensions.Any())
        {
            return dimensions.Aggregate(1u, (a, b) => a * b);
        }
        return 1;
    }

    /// <summary>Rounds up to the next multiple of 16.</summary>
    public static uint Align16(uint value) => (value + 15u) & ~15u;

    /// <summary>Converts a semantic like "POSITION4" to ("POSITION", 4u), returns ("", 0u) if empty</summary>
    public static (string, uint) SplitSemanticInTextAndIndex(string semantic)
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
