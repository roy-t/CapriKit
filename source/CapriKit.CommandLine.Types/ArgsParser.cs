namespace CapriKit.CommandLine.Types;


public class ArgsParser
{
    private const StringComparison ArgStringComparison = StringComparison.InvariantCulture;

    private readonly LinkedList<string> arguments;
    public ArgsParser(params string[] args)
    {
        this.arguments = new LinkedList<string>(args);
    }

    public IEnumerable<string> GetUnmatchedArguments()
    {
        return arguments;
    }

    public bool PopVerb(string verb)
    {
        if (arguments.Count > 0 && arguments.First.Value.Equals(verb, ArgStringComparison))
        {
            arguments.RemoveFirst();
            return true;
        }

        return false;
    }

    public bool PopFlag<T>(string flag, out T result)
    {
        if (arguments.Count > 0)
        {
            var current = arguments.First;
            var next = current.Next;
            while (current != null && next != null)
            {                
                var currentValue = current.Value;
                var nextValue = next.Value;
                if (currentValue.Equals(flag, ArgStringComparison))
                {
                    
                    arguments.Remove(currentValue);
                    arguments.Remove(nextValue);

                    result = (T)Parse<T>(nextValue);
                    return true;
                }

                current = next;
            }
        }

        result = default!;
        return false;
    }

    public bool PopBoolFlag(string flag)
    {
        if (arguments.Count > 0)
        {
            var current = arguments.First;            
            while (current != null)
            {
                var currentValue = current.Value;                
                if (currentValue.Equals(flag, ArgStringComparison))
                {

                    arguments.Remove(currentValue);
                    return true;
                }

                current = current.Next;
            }
        }

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
