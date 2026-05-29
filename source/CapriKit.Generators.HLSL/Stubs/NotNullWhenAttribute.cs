namespace System.Diagnostics.CodeAnalysis;

// netstandard2.0 does not expose this attribute, but the compiler recognizes it by name.
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class NotNullWhenAttribute : Attribute
{
    public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
    public bool ReturnValue { get; }
}
