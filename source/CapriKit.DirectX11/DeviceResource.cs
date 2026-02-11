namespace CapriKit.DirectX11;

public abstract class DeviceResource
{
    private static int NextSequenceId = 1;

    public readonly int SequenceId;

    public DeviceResource()
    {
        SequenceId = Interlocked.Increment(ref NextSequenceId);
        System.Diagnostics.Debug.Assert(SequenceId > 0, "Sequence id overflowed");
    }
}
