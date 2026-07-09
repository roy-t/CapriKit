using System.Runtime.InteropServices;

namespace CapriKit.SuperCompressed;

/// <summary>
/// An opened KTX2 texture, created by <see cref="Ktx2Transcoder.Open"/> and passed to the
/// other <see cref="Ktx2Transcoder"/> methods. Dispose it to free the native reader and
/// the file data it references.
/// </summary>
public sealed class Ktx2FileHandle : SafeHandle
{
    internal Ktx2FileHandle(byte[] fileData)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        // The native KTX2 reader references the file data instead of copying it. Storing the
        // pinned buffer here keeps it reachable for at least as long as any native call can
        // read through this handle: the interop marshaller keeps the handle (and with it this
        // field) alive for the duration of every call the handle is passed to.
        FileDate = fileData;
    }

    internal byte[] FileDate { get; }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        NativeMethods.bt_ktx2_close(handle);
        return true;
    }
}
