using System.Buffers;
using System.Runtime.InteropServices;

namespace CapriKit.IO.Streams;

public static class StreamExtensions
{
    extension(Stream stream)
    {
        /// <summary>
        /// Gets the number of bytes you can still read before reaching the end of the stream.
        /// </summary>
        public long Remainder => stream.Length - stream.Position;

        /// <summary>
        /// Blits an array of unmanaged structs. Assumes that struct's
        /// in-memory layout and endianness match the data's layout and endianness.
        /// </summary>
        public async Task<T[]> BlitArrayAsync<T>(int count, CancellationToken cancellationToken = default)
            where T : unmanaged
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count, nameof(count));
            if (stream.Remainder < count)
            {
                throw new ArgumentException($"Trying to read {count} bytes from a stream with only {stream.Remainder} bytes left.", nameof(count));
            }

            var array = GC.AllocateUninitializedArray<T>(count);
            var manager = new ByteMemoryManager<T>(array);
            await stream.ReadExactlyAsync(manager.Memory, cancellationToken);
            return array;
        }
    }

    private sealed class ByteMemoryManager<T>(T[] array) : MemoryManager<byte>
        where T : unmanaged
    {
        public override Span<byte> GetSpan() => MemoryMarshal.AsBytes(array.AsSpan());

        public override unsafe MemoryHandle Pin(int elementIndex = 0)
        {
            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var pointer = (byte*)handle.AddrOfPinnedObject() + elementIndex;
            // Disposing the memory handle unpins and frees the GCHandle.
            return new MemoryHandle(pointer, handle);
        }

        public override void Unpin() { }

        protected override void Dispose(bool disposing) { }
    }
}
