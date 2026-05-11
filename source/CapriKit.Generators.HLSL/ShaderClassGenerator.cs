using System.Runtime.Serialization;

namespace CapriKit.Generators.HLSL;

public static class ShaderClassGenerator
{

}

/*
public sealed class SomeShader
{
    private readonly CapriKit.DirectX11.Device Device;

    public static string SourceFile => "SomePath.hlsl";

    public const uint SomeRegister = 0;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SomeType
    {
        public float Value;
    }


    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public struct SomeCBufferType
    {
        [System.Runtime.InteropServices.FieldOffset(0)]
        public float Value;
    }

    // We don't need CreateInputLayout as IVertexShader takes care of that

    public CapriKit.DirectX11.Resources.Shaders.IVertexShader EntryPointA { get; }
    public CapriKit.DirectX11.Resources.Shaders.IPixelShader EntryPointB { get; }

    public SomeShader.Binding CreateBinding() => new SomeShader.Binding(Device);

    public sealed class Binding : System.IDisposable
    {
        public Binding(CapriKit.DirectX11.Device device)
        {
             CBuffer = new CapriKit.DirectX11.Buffers.ConstantBuffer<SomeCBufferType>(device, "CBuffer");
        }

        private readonly CapriKit.DirectX11.Buffers.ConstantBuffer<SomeCBufferType> CBuffer;

        public void MapConstantBuffer(CapriKit.DirectX11.Contexts.DeviceContext context, float value)
        {
            var constants = new SomeCBufferType
            {
                Value = value,
            };

            CBuffer.Write(context, [constants]);
        }

        public void Dispose()
        {
            CBuffer.Dispose();
        }
    }
}
*/
