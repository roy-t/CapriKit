using CapriKit.DirectX11;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.DirectX11.Resources.Shaders.Reflection;
using CapriKit.IO;

namespace CapriKit.Tests.DirectX11.Resources.Shaders;

internal class ShaderReflectorTests
{
    [Test]
    public async Task Reflect()
    {
        using var device = new Device();

        var fileSystem = new InMemoryFileSystem();
        var byteCode = ShaderCompiler.CompileVertexShader(fileSystem, ShaderSource, "VS", "ShaderReflectorTests.cs");
        ShaderReflector.Reflect(byteCode);

        // TODO: test CBuffer
        // TODO: test structs used for VS_INPUT (input parameter description)

        await Task.CompletedTask;
    }

    private const string ComputeShaderSource = """
        struct CB_INPUT
        {
            float a;
            float3 b;
        };
        StructuredBuffer<CB_INPUT> gInput  : register(t0);        
        RWStructuredBuffer<float> gOutput : register(u0);        
        
        [numthreads(1, 1, 1)]
        void CS(uint3 dispatchThreadId : SV_DispatchThreadID)
        {
            const uint idx = dispatchThreadId.x;
            if (idx >= 4)
                return;
            
            float v = gInput[idx];
            gOutput[idx] = v.a * (idx + 1) + v.b.x;
        }
        """;

    private const string ShaderSource = """
        struct VS_INPUT
        {
            float3 position : POSITION;
        };

        struct PS_INPUT
        {
            float4 position : SV_POSITION;    
        };

        struct OUTPUT
        {
            float4 color : SV_Target0;
        };

        cbuffer Constants : register(b0)
        {    
            float4x4 WorldViewProjection; 
            float4 Color;
        };

        PS_INPUT VS(VS_INPUT input, uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
        {
            PS_INPUT output;

            float4 position = float4(input.position, 1.0f);
            output.position = mul(WorldViewProjection, position);

            return output;
        }

        OUTPUT PS(PS_INPUT input)
        {
            OUTPUT output;

            output.color = Color;
            return output;
        }
        """;
}
