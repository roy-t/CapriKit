namespace CapriKit.Generators.HLSL;

internal static class TypeTranslator
{
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

    public static string Translate(string primitiveType) => primitiveType switch
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
