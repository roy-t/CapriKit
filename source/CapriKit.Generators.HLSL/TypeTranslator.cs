namespace CapriKit.Generators.HLSL;

internal readonly record struct HlslTypeDescriptor(string DotNetType, uint SizeInBytes, string Format);

internal static class TypeTranslator
{
    private const string UnknownFormat = "Vortice.DXGI.Format.Unknown";

    public static HlslTypeDescriptor Translate(string hlslType)
    {
        if (TryTranslate(hlslType, out var type))
        {
            return type;
        }

        throw new NotSupportedException($"Cannot translate HLSL type '{hlslType}' as a primitive type.");
    }

    public static bool TryTranslate(string hlslType, out HlslTypeDescriptor type)
    {
        type = hlslType switch
        {
            "bool" => new HlslTypeDescriptor("bool", 4, UnknownFormat),

            "int" => new HlslTypeDescriptor("int", 4, "Vortice.DXGI.Format.R32_SInt"),
            "int2" => new HlslTypeDescriptor("Vortice.Mathematics.Int2", 8, "Vortice.DXGI.Format.R32G32_SInt"),
            "int3" => new HlslTypeDescriptor("Vortice.Mathematics.Int3", 12, "Vortice.DXGI.Format.R32G32B32_SInt"),
            "int4" => new HlslTypeDescriptor("Vortice.Mathematics.Int4", 16, "Vortice.DXGI.Format.R32G32B32A32_SInt"),

            "uint" or "dword" => new HlslTypeDescriptor("uint", 4, "Vortice.DXGI.Format.R32_UInt"),
            "uint2" => new HlslTypeDescriptor("Vortice.Mathematics.UInt2", 8, "Vortice.DXGI.Format.R32G32_UInt"),
            "uint3" => new HlslTypeDescriptor("Vortice.Mathematics.UInt3", 12, "Vortice.DXGI.Format.R32G32B32_UInt"),
            "uint4" => new HlslTypeDescriptor("Vortice.Mathematics.UInt4", 16, "Vortice.DXGI.Format.R32G32B32A32_UInt"),

            "double" => new HlslTypeDescriptor("double", 8, UnknownFormat),
            "float" => new HlslTypeDescriptor("float", 4, "Vortice.DXGI.Format.R32_Float"),
            "float2" => new HlslTypeDescriptor("System.Numerics.Vector2", 8, "Vortice.DXGI.Format.R32G32_Float"),
            "float3" => new HlslTypeDescriptor("System.Numerics.Vector3", 12, "Vortice.DXGI.Format.R32G32B32_Float"),
            "float4" => new HlslTypeDescriptor("System.Numerics.Vector4", 16, "Vortice.DXGI.Format.R32G32B32A32_Float"),
            "float4x4" => new HlslTypeDescriptor("System.Numerics.Matrix4x4", 64, UnknownFormat),

            _ => default
        };

        return !string.IsNullOrEmpty(type.DotNetType);
    }

    public static uint GetSizeInBytes(string hlslType) => Translate(hlslType).SizeInBytes;

    public static string GetFormat(string hlslType)
        => TryTranslate(hlslType, out var type) ? type.Format : UnknownFormat;
}
