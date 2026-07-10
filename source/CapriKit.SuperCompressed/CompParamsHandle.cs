using System.Runtime.InteropServices;

namespace CapriKit.SuperCompressed;

/// <summary>
/// Owns a native compression parameters object, created by <c>bu_new_comp_params</c>
/// and freed by <c>bu_delete_comp_params</c>.
/// </summary>
internal sealed class CompParamsHandle : SafeHandle
{
    public CompParamsHandle()
        : base(IntPtr.Zero, ownsHandle: true) { }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.bu_delete_comp_params(handle);
    }
}
