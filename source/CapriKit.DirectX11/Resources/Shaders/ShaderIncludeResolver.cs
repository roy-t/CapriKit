using CapriKit.IO;
using SharpGen.Runtime;
using System.Text;
using Vortice.Direct3D;

namespace CapriKit.DirectX11.Resources.Shaders;

internal sealed class ShaderIncludeResolver : CallbackBase, Include
{
    private sealed class ShaderStream(FilePath source, byte[] buffer) : MemoryStream(buffer, false)
    {
        public FilePath Source { get; } = source;
    }

    private readonly IReadOnlyVirtualFileSystem FileSystem;

    public ShaderIncludeResolver(IReadOnlyVirtualFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }

    public Stream Open(IncludeType type, string fileName, Stream? parentStream)
    {
        // If this file is included by a file we previously opened
        // make sure that the we handle relative paths correctly.
        if (parentStream is ShaderStream includer)
        {
            fileName = includer.Source.Directory.Append(fileName);
        }

        // The stream might be UTF-8 or UTF-16 even if the contents
        // are only ASCII characters. Read the full file and convert
        // it before passing it to DirectX.
        using var fileStream = FileSystem.OpenRead(fileName);
        using var reader = new StreamReader(fileStream);
        var text = reader.ReadToEnd();
        var bytes = Encoding.ASCII.GetBytes(text);

        return new ShaderStream(fileName, bytes);
    }

    public void Close(Stream stream)
    {
        stream.Close();
    }
}
