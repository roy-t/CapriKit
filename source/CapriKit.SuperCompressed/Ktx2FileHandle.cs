using System.Runtime.InteropServices;

namespace CapriKit.SuperCompressed;

/// <summary>
/// Owns a native KTX2 file object, created by <c>bt_ktx2_open</c> and freed by
/// <c>bt_ktx2_close</c>. The file data passed to <c>bt_ktx2_open</c> is referenced,
/// not copied, by the native side: the owner of this handle must keep that memory
/// valid until the handle is disposed.
/// </summary>
internal sealed class Ktx2FileHandle : SafeHandle
{
    public Ktx2FileHandle()
        : base(IntPtr.Zero, ownsHandle: true) { }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        NativeMethods.bt_ktx2_close(handle);
        return true;
    }
}
