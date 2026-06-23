using CapriKit.Generators.HLSL.Parser;
using System.Text;
using static CapriKit.Generators.HLSL.SourceCodeUtils;

namespace CapriKit.Generators.HLSL;

internal sealed class StructTranslator
{
    private readonly StructScope Scope;
    private readonly Dictionary<string, DotNetStruct> KnownStructures;
    private readonly HashSet<string> StructuresInProgress;

    public StructTranslator()
        : this(StructScope.Empty)
    {
    }

    public StructTranslator(StructScope scope)
    {
        Scope = scope;
        KnownStructures = [];
        StructuresInProgress = [];
    }

    public DotNetStruct LayoutConstantBuffer(ConstantBuffer constantBuffer)
    {
        var members = new List<DotNetStructMember>(constantBuffer.Members.Count);
        var offset = 0u;
        foreach (var member in constantBuffer.Members)
        {
            var documentation = DocumentMember(member);
            var type = TranslateType(member.Type, Scope.CurrentFile.Path);
            var name = CreateValidTypeIdentifier(member.Name);
            var elementCount = Flatten(member.Dimensions);

            if (elementCount > 1)
            {
                // Arrays start a new register and each element is padded to 16 bytes.
                var stride = Align16(type.Size);
                offset = Align16(offset);
                members.Add(new DotNetStructMember(documentation, type, name, offset, stride, elementCount));
                offset += stride * elementCount;
            }
            else
            {
                // A scalar or vector may not straddle a 16-byte boundary.
                if ((offset % 16) + type.Size > 16)
                {
                    offset = Align16(offset);
                }
                members.Add(new DotNetStructMember(documentation, type, name, offset, type.Size, elementCount));
                offset += type.Size;
            }
        }

        var size = Align16(offset);
        var structType = new DotNetType(CreateValidTypeIdentifier(constantBuffer.Name), size);
        return new DotNetStruct(structType, members);
    }

    public DotNetStruct LayoutStruct(Structure @struct)
    {
        return LayoutStruct(Scope.GetDefinition(@struct));
    }

    private DotNetStruct LayoutStruct(ScopedStructure definition)
    {
        var key = GetStructureKey(definition);
        if (KnownStructures.TryGetValue(key, out var existing))
        {
            return existing;
        }

        if (!StructuresInProgress.Add(key))
        {
            throw new NotSupportedException($"Cannot layout recursive struct '{@definition.Structure.Name}'.");
        }

        try
        {
            var members = new List<DotNetStructMember>(definition.Structure.Members.Count);
            var offset = 0u;
            foreach (var member in definition.Structure.Members)
            {
                var documentation = DocumentMember(member);
                var type = TranslateType(member.Type, definition.Path);
                var name = CreateValidTypeIdentifier(member.Name);
                var elementCount = Flatten(member.Dimensions);

                members.Add(new DotNetStructMember(documentation, type, name, offset, type.Size, elementCount));
                offset += type.Size * elementCount;
            }

            var structType = new DotNetType(CreateValidTypeIdentifier(definition.Structure.Name), offset);
            var dotNetStruct = new DotNetStruct(structType, members);
            KnownStructures[key] = dotNetStruct;
            return dotNetStruct;
        }
        finally
        {
            StructuresInProgress.Remove(key);
        }
    }

    private DotNetType TranslateType(string hlslType, string originPath)
    {
        if (Scope.TryResolve(hlslType, originPath, out var structure))
        {
            return LayoutStruct(structure).Type;
        }

        var type = TypeTranslator.Translate(hlslType);
        return new DotNetType(type.DotNetType, type.SizeInBytes);
    }

    private static string GetStructureKey(ScopedStructure structure)
        => $"{structure.Path}|{structure.Structure.Name}";

    private static string DocumentMember(Member member)
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
}


internal sealed record DotNetStruct
(
    DotNetType Type,
    List<DotNetStructMember> Members
);

internal sealed record DotNetStructMember
(
    string Documentation,
    DotNetType Type,
    string Name,
    uint Offset,
    uint Stride,
    uint ElementCount
);

internal sealed record DotNetType
(
    string Name,
    uint Size
);
