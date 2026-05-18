namespace CapriKit.Generators.HLSL;

/// <summary>
/// The result of translating an HLSL type to a .NET type.
/// </summary>
/// <param name="DotNetType">The fully qualified .NET type.</param>
/// <param name="FixedSize">
/// When set, the field is a fixed-size buffer with this many (flattened) elements.
/// </param>
/// <param name="OriginalDimensions">The array dimensions as declared in HLSL.</param>
public readonly record struct TranslatedType(string DotNetType, uint FixedSize, IReadOnlyList<uint> OriginalDimensions)
{
    public bool IsFixed => FixedSize > 0u;
}

public static class TypeTranslator
{
    public static TranslatedType Translate(string hlslType, IReadOnlyList<uint> dimensions)
    {
        var dotNetType = MapPrimitive(hlslType);

        if (dimensions.Count == 0)
        {
            return new TranslatedType(dotNetType, 0, dimensions);
        }

        // HLSL arrays become C# fixed-size buffers; multidimensional arrays are flattened.
        var total = dimensions.Aggregate(1u, (a, b) => a * b);
        return new TranslatedType(dotNetType, total, dimensions);
    }

    public static uint GetSizeInBytes(string hlslType) => hlslType switch
    {
        "bool" or "int" or "uint" or "dword" or "float" => 4,
        "double" => 8,
        "float2" => 8,
        "float3" => 12,
        "float4" => 16,
        "float4x4" => 64,
        _ => throw new NotSupportedException($"Cannot compute size of {hlslType}")
    };

    private static string MapPrimitive(string primitiveType) => primitiveType switch
    {
        "bool" => "bool",
        "int" => "int",
        "uint" or "dword" => "uint",
        "float" => "float",
        "double" => "double",
        "float2" => "System.Numerics.Vector2",
        "float3" => "System.Numerics.Vector3",
        "float4" => "System.Numerics.Vector4",
        "float4x4" => "System.Numerics.Matrix4x4",
        _ => SourceCodeUtils.CreateValidIdentifier(primitiveType)
    };
}
