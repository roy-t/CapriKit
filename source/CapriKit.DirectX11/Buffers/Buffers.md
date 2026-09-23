# Buffers

Buffers for uploading, downloading and transferring data between the CPU, GPU and shaders. The following table shows you which buffer to use in your situation.

## Buffer types

| Buffer | DirectX 11 role | Usage | CPU read | CPU write | Shader read | Shader write |
| --- | --- | --- | --- | --- | --- | --- |
| `ConstantBuffer<T>` | `cbuffer` | Dynamic | no | yes | yes, via a constant buffer slot | no |
| `StructuredBuffer<T>` | `StructuredBuffer<T> : register(t)` | Dynamic | no | yes | yes, via an SRV | no |
| `RWStructuredBuffer<T>` | `RWStructuredBuffer<T> : register(u)` | Default | no | no | yes, via an SRV | yes, via a UAV |
| `StagingBuffer<T>` | Copy GPU only data to the CPU | Staging | yes | no | no | no |
| `StagingBuffer2D<T>` | Copy GPU only texture data to the CPU | Staging | yes | no | no | no |
| `VertexBuffer<T>` | Vertex buffer (input assembler) | Dynamic | no | yes | yes, via the input layout | no |
| `IndexBuffer<T>` | Index buffer (input assembler) | Dynamic | no | yes | yes, as indices | no |
| `ImmutableStructuredBuffer<T>` | `StructuredBuffer<T> : register(t)` | Immutable | no | at creation only | yes, via an SRV | no |
| `ImmutableVertexBuffer<T>` | Vertex buffer (input assembler) | Immutable | no | at creation only | yes, via the input layout | no |
| `ImmutableIndexBuffer<T>` | Index buffer (input assembler) | Immutable | no | at creation only | yes, as indices | no |

## Gotchas

- Elements in a constant buffer must follow the [HLSL packing rules](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-packing-rules) and include padding to 16 bytes.
- Staging buffers must match the format and dimensions of the resource they are copying exactly.
