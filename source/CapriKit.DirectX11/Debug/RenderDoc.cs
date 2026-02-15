using System.Runtime.InteropServices;
using System.Text;

namespace CapriKit.DirectX11.Debug;

/// <summary>
/// In-application RenderDoc API
/// https://renderdoc.org/docs/in_application_api.html
/// </summary>
public sealed unsafe class RenderDoc : IDisposable
{
    private nint _module;
    private RENDERDOC_API_1_6_0 _api;

    private RenderDoc() { }

    public static RenderDoc? TryLoad()
    {
        var instance = new RenderDoc();
        if (instance.Load())
        {
            return instance;
        }
        return null;
    }

    public void StartFrameCapture(nint device, nint window = default)
    {
        _api.StartFrameCapture((void*)device, (void*)window);
    }

    public bool EndFrameCapture(nint device, nint window = default)
    {
        return _api.EndFrameCapture((void*)device, (void*)window) != 0;
    }

    public void TriggerCapture()
    {
        _api.TriggerCapture();
    }

    public uint GetNumCaptures()
    {
        return _api.GetNumCaptures();
    }

    public string GetCapture(uint index)
    {
        uint pathLength;
        ulong timeStamp;
        var result = _api.GetCapture(index, null, &pathLength, &timeStamp);
        if (result == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        fixed (char* fileName = new char[pathLength])
        {
            _api.GetCapture(index, fileName, &pathLength, &timeStamp);
            return Encoding.UTF8.GetString((byte*)fileName, (int)pathLength - 1); // skip the '\0' character
        }
    }

    public uint LaunchReplayUI(string args)
    {
        int utf8ByteCount = Encoding.UTF8.GetByteCount(args);
        byte* utf8Bytes = stackalloc byte[utf8ByteCount + 1];
        fixed (char* argsPtr = args)
        {
            int encoded = Encoding.UTF8.GetBytes(argsPtr, args.Length, utf8Bytes, utf8ByteCount);
            utf8Bytes[encoded] = 0;
        }

        return _api.LaunchReplayUI(1, utf8Bytes);
    }

    private bool Load()
    {
        var path = GetPathToLibrary();
        if (!NativeLibrary.TryLoad(path, out _module))
        {
            return false;
        }

        if (!NativeLibrary.TryGetExport(_module, "RENDERDOC_GetAPI", out var proc))
        {
            return false;
        }

        var getApi = Marshal.GetDelegateForFunctionPointer<pRENDERDOC_GetAPI>(proc);
        void* apiPtr = null;
        var result = getApi(RENDERDOC_Version.API_VERSION_1_6_0, &apiPtr);

        if (result == 0 || apiPtr == null)
        {
            return false;
        }

        _api = Marshal.PtrToStructure<RENDERDOC_API_1_6_0>((nint)apiPtr);
        return true;
    }

    public void Dispose()
    {
        if (_module != 0)
        {
            NativeLibrary.Free(_module);
            _module = 0;
            _api = default;
        }

        GC.SuppressFinalize(this);
    }

    private static string GetPathToLibrary()
    {
        var programFiles = Environment.GetEnvironmentVariable("ProgramFiles") ?? string.Empty;
        var systemInstallPath = Path.Combine(programFiles, "RenderDoc", "renderdoc.dll");
        if (File.Exists(systemInstallPath))
        {
            return systemInstallPath;
        }

        return "renderdoc.dll"; // Hope its available on the PATH
    }
}

