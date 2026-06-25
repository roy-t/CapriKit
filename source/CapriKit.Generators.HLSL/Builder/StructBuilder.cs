using CapriKit.Generators.HLSL.Parser;
using static CapriKit.Generators.HLSL.Builder.SourceCodeUtils;
using static CapriKit.Generators.HLSL.Builder.StructLayoutHelper;

namespace CapriKit.Generators.HLSL.Builder;

internal sealed class StructBuilder
{
    private readonly Dictionary<string, StructMetadata> KnownStructs = [];
    private const string ExplicitLayoutKind = "System.Runtime.InteropServices.LayoutKind.Explicit";

    /// <summary>
    /// Registers all structs defined in the list of shaders. Ensure that the list of shaders is sorted
    /// so that a definition of a struct happens earlier than the usage of a struct in another struct.
    /// </summary>
    public void RegisterStructs(IReadOnlyList<(string, ShaderMetadata)> shaders, GeneratorConfiguration config)
    {
        foreach (var (path, shader) in shaders)
        {
            foreach (var @struct in shader.Structures)
            {
                GetMetaData(path, shader, @struct, config);
            }
        }
    }

    /// <summary>
    /// Emits a single explicitly laid out cbuffer and any required supporting structs.
    /// </summary>
    public void WriteCBuffer(SourceCodeBuilder builder, string path, ShaderMetadata shader, ConstantBuffer buffer, GeneratorConfiguration config)
    {
        var meta = LayoutCBuffer(buffer);
        builder.WriteAttribute("System.Runtime.InteropServices.StructLayout", ExplicitLayoutKind, $"Size = {meta.Type.Size}");
        WriteStructDefinition(builder, meta);
    }

    /// <summary>
    /// Emits a single explicitly laid out struct, any required supporting structs and element layouts.
    /// </summary>
    public void WriteStruct(SourceCodeBuilder builder, string path, ShaderMetadata shader, Structure @struct, GeneratorConfiguration config)
    {
        var meta = GetMetaData(path, shader, @struct, config);
        builder.WriteAttribute("System.Runtime.InteropServices.StructLayout", ExplicitLayoutKind, $"Size = {meta.Type.Size}");
        WriteStructDefinition(builder, meta);

        if (@struct.Kind == StructureKind.VertexShaderInput)
        {
            InputElementDescriptionBuilder.WriteInputElementDescription(builder, meta);
        }
    }

    /// <summary>
    /// Writes the struct and any required supporting struct.
    /// </summary>
    private static void WriteStructDefinition(SourceCodeBuilder builder, StructMetadata meta)
    {
        builder.OpenStruct(Modifiers.Public, meta.Type.Name);
        foreach (var member in meta.Members)
        {
            WriteStructMember(builder, meta.Type.Name, member);
        }

        builder.CloseBlock();

        foreach (var member in meta.Members)
        {
            if (member.ElementCount > 1)
            {
                WriteArrayType(builder, meta.Type.Name, member);
            }
        }
    }

    /// <summary>
    /// Emits an individual struct member with the correct field offset.
    /// </summary>
    private static void WriteStructMember(SourceCodeBuilder builder, string ownerName, StructMemberMetadata member)
    {
        builder.WriteSummaryComment(member.Documentation);
        builder.WriteAttribute("System.Runtime.InteropServices.FieldOffset", ToLiteral(member.Offset));
        var type = member.ElementCount > 1 ? ArrayStructName(ownerName, member.Name) : member.Type.Name;
        builder.WriteField(Modifiers.Public, type, member.Name);
    }

    /// <summary>
    /// Emits the helper structs that back an array member: an <c>[InlineArray]</c>
    /// of the element type. If the element's stride is not the same as its size
    /// the element is first wrapped in a struct padded to that size.
    /// </summary>
    private static void WriteArrayType(SourceCodeBuilder builder, string ownerName, StructMemberMetadata member)
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

    private StructMetadata GetMetaData(string path, ShaderMetadata shader, Structure @struct, GeneratorConfiguration config)
    {
        var id = CreateValidTypeIdentifier(@struct.Name);
        var key = $"{path}:{id}";
        if (KnownStructs.TryGetValue(key, out var meta))
        {
            return meta;
        }

        meta = LayoutStruct(@struct);
        KnownStructs.Add(key, meta);
        return meta;
    }

    private StructMetadata LayoutCBuffer(ConstantBuffer buffer)
    {
        var members = new List<StructMemberMetadata>(buffer.Members.Count);
        var offset = 0u;
        foreach (var member in buffer.Members)
        {
            var documentation = DocumentMember(member);
            var type = TranslateType(member.Type);
            var name = CreateValidTypeIdentifier(member.Name);
            var elementCount = Flatten(member.Dimensions);
            var format = PrimitiveTranslator.GetFormat(member.Type);
            var (semantic, semanticIndex) = SplitSemanticInTextAndIndex(member.Semantic);

            if (elementCount > 1)
            {
                // Arrays start a new register and each element is padded to 16 bytes.
                var stride = Align16(type.Size);
                offset = Align16(offset);
                members.Add(new StructMemberMetadata(documentation, type, name, offset, stride, elementCount, format, semantic, semanticIndex));
                offset += stride * elementCount;
            }
            else
            {
                // A scalar or vector may not straddle a 16-byte boundary.
                if ((offset % 16) + type.Size > 16)
                {
                    offset = Align16(offset);
                }
                members.Add(new StructMemberMetadata(documentation, type, name, offset, type.Size, elementCount, format, semantic, semanticIndex));
                offset += type.Size;
            }
        }

        var id = CreateValidTypeIdentifier(buffer.Name);
        var size = Align16(offset);
        return new StructMetadata(new TypeMetadata(id, size), members);
    }

    private StructMetadata LayoutStruct(Structure @struct)
    {
        var members = new List<StructMemberMetadata>(@struct.Members.Count);
        var offset = 0u;
        foreach (var member in @struct.Members)
        {
            var documentation = DocumentMember(member);
            var type = TranslateType(member.Type);
            var name = CreateValidTypeIdentifier(member.Name);
            var elementCount = Flatten(member.Dimensions);
            var format = PrimitiveTranslator.GetFormat(member.Type);
            var (semantic, semanticIndex) = SplitSemanticInTextAndIndex(member.Semantic);

            members.Add(new StructMemberMetadata(documentation, type, name, offset, type.Size, elementCount, format, semantic, semanticIndex));
            offset += type.Size * elementCount;
        }

        var id = CreateValidTypeIdentifier(@struct.Name);
        return new StructMetadata(new TypeMetadata(id, offset), members);
    }

    private TypeMetadata TranslateType(string hlslType)
    {
        if (KnownStructs.TryGetValue(hlslType, out var value))
        {
            return value.Type;
        }

        var type = PrimitiveTranslator.Translate(hlslType);
        var size = PrimitiveTranslator.GetSizeInBytes(hlslType);
        return new TypeMetadata(type, size);
    }
}


