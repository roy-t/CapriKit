namespace CapriKit.Generators.HLSL;

/// <summary>
/// The result of translating an HLSL type to a .NET type.
/// </summary>
/// <param name="DotNetType">The fully qualified .NET type.</param>
/// <param name="FixedSize">
/// When set, the field is a fixed-size buffer with this many (flattened) elements.
/// </param>
/// <param name="OriginalDimensions">The array dimensions as declared in HLSL.</param>
internal readonly record struct TranslatedType(string DotNetType, uint FixedSize, IReadOnlyList<uint> OriginalDimensions)
{
    public bool IsFixed => FixedSize > 0u;
}

internal static class TypeTranslator
{
    // TODO: Everyone uses just the .DotNetType name as the whole dimension stuff is fixed somewhere else
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
        "int2" => 8,
        "int3" => 12,
        "int4" => 16,
        "uint2" => 8,
        "uint3" => 12,
        "uint4" => 16,
        "double" => 8,
        "float2" => 8,
        "float3" => 12,
        "float4" => 16,
        "float4x4" => 64,
        _ => throw new NotSupportedException($"Cannot compute size of {hlslType}")
    };

    public static string GetFormat(string hlslType) => hlslType switch
    {
        "int" => "Vortice.DXGI.Format.R32_SInt",
        "int2" => "Vortice.DXGI.Format.R32G32_SInt",
        "int3" => "Vortice.DXGI.Format.R32G32B32_SInt",
        "int4" => "Vortice.DXGI.Format.R32G32B32A32_SInt",

        "uint" or "dword" => "Vortice.DXGI.Format.R32_UInt",
        "uint2" => "Vortice.DXGI.Format.R32G32_UInt",
        "uint3" => "Vortice.DXGI.Format.R32G32B32_UInt",
        "uint4" => "Vortice.DXGI.Format.R32G32B32A32_UInt",

        "float" => "Vortice.DXGI.Format.R32_Float",
        "float2" => "Vortice.DXGI.Format.R32G32_Float",
        "float3" => "Vortice.DXGI.Format.R32G32B32_Float",
        "float4" => "Vortice.DXGI.Format.R32G32B32A32_Float",

        _ => "Vortice.DXGI.Format.Unknown"
    };

    private static string MapPrimitive(string primitiveType) => primitiveType switch
    {
        "bool" => "bool",

        "int" => "int",
        "int2" => "Vortice.Mathematics.Int2",
        "int3" => "Vortice.Mathematics.Int3",
        "int4" => "Vortice.Mathematics.Int4",

        "uint" or "dword" => "uint",
        "uint2" => "Vortice.Mathematics.UInt2",
        "uint3" => "Vortice.Mathematics.UInt3",
        "uint4" => "Vortice.Mathematics.UInt4",

        "double" => "double",
        "float" => "float",
        "float2" => "System.Numerics.Vector2",
        "float3" => "System.Numerics.Vector3",
        "float4" => "System.Numerics.Vector4",
        "float4x4" => "System.Numerics.Matrix4x4",
        _ => SourceCodeUtils.CreateValidTypeIdentifier(primitiveType)
    };
}
