namespace CapriKit.CommandLine.Types;

public static class ArgsParser
{
    private static readonly StringComparison ArgStringComparison = StringComparison.InvariantCulture;

    public static bool IsVerb(string verb, params string[] args)
    {
        return args.Length > 0 && args[0].Equals(verb, ArgStringComparison);
    }

    public static bool TryParseFlag<T>(string flag, out T? result, params string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(flag, ArgStringComparison))
            {
                result = (T)Parse<T>(args[i + 1]);
                return true;
            }
        }

        result = default;
        return false;
    }

    public static object Parse<T>(string argument)
    {
        var type = typeof(T);
        var code = Type.GetTypeCode(type);
        switch (code)
        {
            case TypeCode.Boolean:
                return bool.Parse(argument);                
            case TypeCode.Byte:
                return byte.Parse(argument);
            case TypeCode.Char:
                return char.Parse(argument);
            case TypeCode.DateTime:
                return DateTime.Parse(argument);            
            case TypeCode.Decimal:
                return decimal.Parse(argument);
            case TypeCode.Double:
                return double.Parse(argument);            
            case TypeCode.Int16:
                return short.Parse(argument);
            case TypeCode.Int32:
                return int.Parse(argument);
            case TypeCode.Int64:
                return long.Parse(argument);            
            case TypeCode.SByte:
                return sbyte.Parse(argument);
            case TypeCode.Single:
                return float.Parse(argument);
            case TypeCode.String:
                return argument;
            case TypeCode.UInt16:
                return ushort.Parse(argument);
            case TypeCode.UInt32:
                return uint.Parse(argument);
            case TypeCode.UInt64:
                return ulong.Parse(argument);
            default:
                throw new NotSupportedException($"Could not parse type: {type.FullName}");
        }
    }
}
