using System.Text;

namespace CapriKit.Generators.HLSL;

[Flags]
public enum Modifiers
{
    None,
    Private,
    Protected,
    Internal,
    Public,
    Static,
    Sealed,
    ReadOnly,
    Const,
}

public sealed class SourceCodeBuilder
{
    private readonly StringBuilder builder = new();
    private int indent = 0;

    public void WriteUsing(string @namespace)
    {
        builder.AppendLine($"using {@namespace};");
    }

    public void WriteUsingStatic(string @namespace, string @class)
    {
        builder.AppendLine($"using static {@namespace}.{@class};");
    }

    public void WriteNamespace(string @namespace)
    {
        builder.AppendLine($"namespace {@namespace};");
    }

    public void OpenClass(Modifiers modifiers, string name)
    {
        var modifiersText = GetModifiers(modifiers);
        builder.AppendLine(string.Join(" ", [modifiersText, "class", name]));
        OpenBlock();
    }

    private void OpenBlock()
    {
        builder.AppendLine("{");
        indent++;
    }

    private void CloseBlock()
    {
        builder.AppendLine("}");
        indent--;
    }

    public string Build()
    {
        while (indent > 0)
        {
            CloseBlock();
        }

        return builder.ToString();
    }

    public override string ToString()
    {
        return builder.ToString();
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
