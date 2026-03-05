using CapriKit.IO;
using System.Text;

namespace CapriKit.DirectX11.Resources.Shaders.Reflection;

internal sealed class ShaderReflector
{
    private enum ShaderType
    {
        VertexShader,
        PixelShader,
        ComputeShader
    }

    private record ShaderEntryPoint(string Name, ShaderType Type);

    public async Task Reflect(IReadOnlyVirtualFileSystem fileSystem, FilePath fileName)
    {
        var text = await fileSystem.ReadAllText(fileName, Encoding.UTF8);

    }

    // TODO: this sounds fun but needs quite a bit of work
    //public List<ShaderEntryPoint> FindEntryPoints(string text)
    //{
    //    var start = 0;
    //    var length = 0;

    //    for (var i = 1; i < text.Length; i++)
    //    {
    //        length = i - start;

    //        if (IsValidFunctionName(text.AsSpan(start, length)))
    //        {

    //        }
    //        else
    //        {

    //        }
    //    }
    //}

    //private static bool IsValidFunctionName(ReadOnlySpan<char> name)
    //{
    //    if (name.Length == 0)
    //    {
    //        return false;
    //    }

    //    if (!IsValidHlslIdentifierStart(name[0]))
    //    {
    //        return false;
    //    }

    //    for (var i = 1; i < name.Length; i++)
    //    {
    //        if (!IsValidHlslIdentifierChar(name[i]))
    //            return false;
    //    }

    //    return true;
    //}

    //private static bool IsValidHlslIdentifierChar(char c)
    //{
    //    return char.IsLetterOrDigit(c) || c == '_';
    //}

    //private static bool IsValidHlslIdentifierStart(char c)
    //{
    //    return char.IsLetter(c) || c == '_';
    //}
}
