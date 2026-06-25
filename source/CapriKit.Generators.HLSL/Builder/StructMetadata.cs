namespace CapriKit.Generators.HLSL.Builder;

internal sealed record StructMetadata(TypeMetadata Type, List<StructMemberMetadata> Members);
internal sealed record StructMemberMetadata(string Documentation, TypeMetadata Type, string Name, uint Offset, uint Stride, uint ElementCount, string Format, string Semantic, uint SemanticIndex);
internal sealed record TypeMetadata(string Name, uint Size);
