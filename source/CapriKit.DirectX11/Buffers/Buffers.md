# Buffers

Every buffer in this folder wraps one `ID3D11Buffer`. Which operations a buffer supports is fixed at
creation by its `BufferDescription`, so the type you pick decides how data gets in and out of it.

## Buffer types

| Buffer | DirectX 11 role | Usage | CPU read | CPU write | Shader read | Shader write |
| --- | --- | --- | --- | --- | --- | --- |
| `ConstantBuffer<T>` | Constant buffer (`cbuffer`) | Dynamic | no | yes | yes, via a constant buffer slot | no |
| `StructuredBuffer<T>` | Structured buffer (`StructuredBuffer<T>`, `t` register) | Dynamic | no | yes | yes, via an SRV | no |
| `RWStructuredBuffer<T>` | Read/write structured buffer (`RWStructuredBuffer<T>`, `u` register) | Default | yes | not by mapping | yes, via an SRV | yes, via a UAV |
| `StagingBuffer<T>` | None, cannot be bound to the pipeline | Staging | yes | no | no | no |
| `VertexBuffer<T>` | Vertex buffer (input assembler) | Dynamic | no | yes | yes, via the input layout | no |
| `IndexBuffer<T>` | Index buffer (input assembler) | Dynamic | no | yes | yes, as indices | no |
| `ImmutableStructuredBuffer<T>` | Structured buffer (`StructuredBuffer<T>`, `t` register) | Immutable | no | at creation only | yes, via an SRV | no |
| `ImmutableVertexBuffer<T>` | Vertex buffer (input assembler) | Immutable | no | at creation only | yes, via the input layout | no |
| `ImmutableIndexBuffer<T>` | Index buffer (input assembler) | Immutable | no | at creation only | yes, as indices | no |

Index buffers are created through the `IndexBuffers` factory, which picks the matching `Format`
(`R16_UInt` or `R32_UInt`) for you. Index data must be `ushort` or `uint`; no other type is valid.

## How the access actually happens

- **CPU write** maps the buffer with `MAP_WRITE_DISCARD`, which DirectX only allows on `Dynamic`
  buffers. Writing replaces the whole contents; there is no partial update. Use `Write` for a
  one-shot write or `OpenWriter` when you write to the same buffer several times per frame.
- **CPU read** maps the buffer with `MAP_READ`, which needs `CpuAccessFlags.Read`. Only
  `StagingBuffer<T>` and `RWStructuredBuffer<T>` have it.
- **"At creation only"** means the data is handed to `ID3D11Device::CreateBuffer` as initial data.
  Nothing can write to the buffer afterwards, not even the GPU. This needs no `DeviceContext`, so
  immutable buffers can be built on any thread, which is what the asset pipeline uses them for.
- **Shader read for vertex, index and constant buffers** goes through a fixed-function slot, not
  through a shader resource view. Asking those buffers for an SRV fails: DirectX only creates one for
  a buffer that was bound with `BindFlags.ShaderResource`.
- `StagingBuffer<T>` is the general read-back path: it cannot be bound to the pipeline, it only
  receives a `CopyResource` from another buffer and is then mapped for reading.

## Marker interfaces

`BufferTypes.cs` declares one interface per trait. They carry no members; they exist so the extension
methods in `BufferExtensions.cs` are only offered on buffers that can actually support them.

| Interface | Unlocks | Implemented by |
| --- | --- | --- |
| `IImmutableDeviceBuffer<T>` | `Length`, `Name`, the native handle | every buffer |
| `IDeviceBuffer<T>` | `Capacity`, `EnsureCapacity`, `SetCapacity` | every mutable buffer |
| `ICpuWriteToBuffer<T>` | `Write`, `OpenWriter` | `ConstantBuffer`, `StructuredBuffer`, `VertexBuffer`, `IndexBuffer`, `RWStructuredBuffer` |
| `ICpuReadFromBuffer<T>` | `Read`, `OpenReader` | `StagingBuffer`, `RWStructuredBuffer` |
| `IShaderReadFromBuffer<T>` | `CreateShaderResourceView` | `StructuredBuffer`, `RWStructuredBuffer`, `ImmutableStructuredBuffer` |
| `IShaderWriteToBuffer<T>` | `CreateUnorderedAccessView` | `RWStructuredBuffer` |
| `IVertexBuffer<T>` | `IA.SetVertexBuffer` | `VertexBuffer`, `ImmutableVertexBuffer` |
| `IIndexBuffer<T>` | `IA.SetIndexBuffer` | `IndexBuffer`, `ImmutableIndexBuffer` |

Marking a buffer with an interface its description cannot back turns a compile-time promise into a
runtime `E_INVALIDARG`, so keep the two in step.

## Gotchas

- A constant buffer's size must be a multiple of 16 bytes, so `T` must be padded to a multiple of 16.
  See the [HLSL packing rules](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-packing-rules).
- A buffer cannot be zero bytes. Mutable buffers reject a capacity below one, immutable buffers reject
  an empty span.
- `CopyResource` needs both buffers to have exactly the same byte width, which is why
  `CopyResourceToStagingBuffer` matches the source's `Capacity` and not its `Length`. A mismatch does
  not throw: the copy is silently dropped and only the debug layer reports it.
- Resizing a mutable buffer discards its contents; `EnsureCapacity` and `SetCapacity` create a new
  `ID3D11Buffer` rather than preserving the old data.
- No usage supports both an unordered access view and a CPU map-write: `Dynamic` cannot carry
  `BindFlags.UnorderedAccess`, and `Staging` cannot carry any bind flags at all. `RWStructuredBuffer<T>`
  is marked `ICpuWriteToBuffer<T>` but throws from `Write` and `OpenWriter` because of this; the CPU can
  only fill it with `UpdateSubresource` or by copying into it from another buffer.
