using Microsoft.CodeAnalysis.CSharp;
using System.Text;

namespace CapriKit.Generators.HLSL.Builder;

internal static class SourceCodeUtils
{
    public static string ToLiteral(string value) => SymbolDisplay.FormatLiteral(value, true);
    public static string ToLiteral(uint value) => SymbolDisplay.FormatPrimitive(value, true, false) ?? throw new Exception($"Failed to format literal, type: {value.GetType().FullName}, value: {value}.");
    public static string ToLiteral(int value) => SymbolDisplay.FormatPrimitive(value, true, false) ?? throw new Exception($"Failed to format literal, type: {value.GetType().FullName}, value: {value}.");

    public static string CreateValidVariableIdentifier(string name)
    {
        return ToLowerCamelCase(CreateValidIdentifier(name));
    }

    public static string CreateValidTypeIdentifier(string name)
    {
        return ToUpperCamelCase(CreateValidIdentifier(name));
    }

    private static string CreateValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "_";
        }

        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            // C# identifiers allow Unicode letters, digits and underscore.
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        // The first character may not be a digit.
        if (char.IsDigit(sb[0]))
        {
            sb.Insert(0, '_');
        }
        return sb.ToString();
    }

    public static string CreateValidNamespace(string path)
     => string.Join(".", path
         .Split(['.', '/', '\\'], StringSplitOptions.RemoveEmptyEntries)
         .Select(CreateValidTypeIdentifier));

    // Path.GetRelativePath would be better if you can access a higher .net version
    public static string GetRelativePath(string basePath, string fullPath)
    {
        // Trailing separator makes Uri treat basePath as a directory, not a file.
        if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            basePath += Path.DirectorySeparatorChar;

        var relative = new Uri(basePath).MakeRelativeUri(new Uri(fullPath));
        return Uri.UnescapeDataString(relative.ToString());
    }

    public static string ToLowerCamelCase(string name)
                 => LowerCaseFirstLetter(ToUpperCamelCase(name));

    public static string ToUpperCamelCase(string name)
    {
        var builder = new StringBuilder(name.Length);
        var upperCase = false;
        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            // Start with capital letter
            upperCase |= i == 0;

            // Default lowercase
            upperCase |= i > 0 && char.IsLower(name[i - 1]) && char.IsUpper(current);

            // Camel case
            if (upperCase)
            {
                current = char.ToUpper(current);
                upperCase = false;
            }
            else
            {
                current = char.ToLower(current);
            }

            // snake case to camel case
            if (current == '_')
            {
                upperCase = true;
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    public static string UpperCaseFirstLetter(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        if (s.Length == 1)
            return s.ToUpper();
        return s.Remove(1).ToUpper() + s.Substring(1);
    }


    public static string LowerCaseFirstLetter(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        if (s.Length == 1)
            return s.ToLower();
        return s.Remove(1).ToLower() + s.Substring(1);
    }
}
