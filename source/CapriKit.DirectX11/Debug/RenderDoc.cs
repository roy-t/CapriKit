using System.Runtime.InteropServices;

namespace CapriKit.DirectX11.Debug;

// TODO: use types from RenderDocNative instead!

/// <summary>
/// In-application RenderDoc API
/// https://renderdoc.org/docs/in_application_api.html
/// </summary>
public sealed unsafe class RenderDoc : IDisposable
{
    private nint _module;
    private RenderDocApi* _api;

    private RenderDoc() { }

    public static bool TryLoad(out RenderDoc instance)
    {
        instance = new RenderDoc();
        return instance.Load();
    }

    public void StartFrameCapture(nint device, nint window = default)
    {
        _api->StartFrameCapture(device, window);
    }

    public bool EndFrameCapture(nint device, nint window = default)
    {
        return _api->EndFrameCapture(device, window) != 0;
    }

    public void TriggerCapture()
    {
        _api->TriggerCapture();
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

    private bool Load()
    {
        var path = GetPathToLibrary();

        if (NativeLibrary.TryLoad(path, out _module) && NativeLibrary.TryGetExport(_module, "RENDERDOC_GetAPI", out var proc))
        {
            delegate* unmanaged[Cdecl]<uint, void**, int> getApi = (delegate* unmanaged[Cdecl]<uint, void**, int>)proc;

            void* apiPtr = null;
            int result = getApi(RENDERDOC_API_VERSION, &apiPtr);
            if (result != 0 && apiPtr != null)
            {
                _api = (RenderDocApi*)apiPtr;
                return true;
            }
        }

        return false;
    }

    // Matches the beginning of RENDERDOC_API_1_6_0
    // We only define the functions we use.
    // Layout must match native exactly.

    [StructLayout(LayoutKind.Sequential)]
    private struct RenderDocApi
    {
        private readonly nint GetAPIVersion;
        private readonly nint SetCaptureOptionU32;
        private readonly nint SetCaptureOptionF32;
        private readonly nint GetCaptureOptionU32;
        private readonly nint GetCaptureOptionF32;

        private readonly nint SetCaptureKeys;
        private readonly nint GetOverlayBits;
        private readonly nint MaskOverlayBits;

        private readonly nint RemoveHooks;
        private readonly nint UnloadCrashHandler;

        private readonly nint SetCaptureFilePathTemplate;
        private readonly nint GetCaptureFilePathTemplate;

        private readonly nint GetNumCaptures;
        private readonly nint GetCapture;

        private readonly nint TriggerMultiFrameCapture;
        public readonly delegate* unmanaged[Cdecl]<void> TriggerCapture;

        public readonly delegate* unmanaged[Cdecl]<nint, nint, void> StartFrameCapture;
        public readonly delegate* unmanaged[Cdecl]<nint, nint, uint> EndFrameCapture;

        private readonly nint IsFrameCapturing;
    }

    public void Dispose()
    {
        if (_module != 0)
        {
            NativeLibrary.Free(_module);
            _module = 0;
        }

        _api = null;
        GC.SuppressFinalize(this);
    }
}

