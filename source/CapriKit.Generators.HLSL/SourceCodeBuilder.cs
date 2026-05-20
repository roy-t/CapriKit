using System.Text;

namespace CapriKit.Generators.HLSL;

[Flags]
internal enum Modifiers
{
    None,
    Private = 1,
    Protected = 2,
    Internal = 4,
    Public = 8,
    Static = 16,
    Unsafe = 32,
    Sealed = 64,
    ReadOnly = 128,
    Const = 256,
    Fixed = 512
}

internal sealed class SourceCodeBuilder
{
    private readonly StringBuilder builder = new();
    private int level = 0;

    public void WriteUsingStatic(string @namespace, string @class)
    {
        WriteLine($"using static {@namespace}.{@class};");
    }

    public void WriteNamespace(string @namespace)
    {
        WriteLine($"namespace {@namespace};");
    }

    public void OpenClass(Modifiers modifiers, string name)
    {
        var modifiersText = GetModifiers(modifiers);
        WriteLine(string.Join(" ", [modifiersText, "class", name]));
        OpenBlock();
    }

    public void OpenStruct(Modifiers modifiers, string name)
    {
        var modifiersText = GetModifiers(modifiers);
        WriteLine(string.Join(" ", [modifiersText, "struct", name]));
        OpenBlock();
    }

    public void WriteField(Modifiers modifiers, string type, string name, string? value = null)
    {
        var modifiersText = GetModifiers(modifiers);
        Write($"{string.Join(" ", [modifiersText, type, name])}");
        if (string.IsNullOrEmpty(value))
        {
            builder.AppendLine(";");
        }
        else
        {
            builder.AppendLine($" = {value};");
        }
    }

    public void WriteAttribute(string name, params string[] values)
    {
        WriteLine($"[{name}({string.Join(", ", values)})]");
    }

    public void OpenBlock()
    {
        WriteLine("{");
        level++;
    }

    public void CloseBlock()
    {
        level--;
        WriteLine("}");
    }

    public string Build()
    {
        while (level > 0)
        {
            CloseBlock();
        }

        return builder.ToString();
    }

    public void WriteSummaryComment(string lines)
    {
        WriteLine("/// <summary>");
        foreach (var line in lines.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            WriteLine($"/// {line}");
        }
        WriteLine("/// </summary>");
    }

    public override string ToString()
    {
        return builder.ToString();
    }

    private void WriteLine(string text)
    {
        builder.Append(' ', level * 4)
               .AppendLine(text);
    }

    private void Write(string text)
    {
        builder.Append(' ', level * 4)
               .Append(text);
    }

    private string GetModifiers(Modifiers modifiers)
    {
        var type = typeof(Modifiers);
        var list = new List<string>(1);
        foreach (var value in Enum.GetValues(type).Cast<Modifiers>())
        {
            if (value != Modifiers.None && modifiers.HasFlag(value))
            {
                list.Add(Enum.GetName(type, value).ToLowerInvariant());
            }
        }

        return string.Join(" ", list);
    }
}
