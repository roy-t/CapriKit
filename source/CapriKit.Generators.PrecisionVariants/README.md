# CapriKit.Generators.PrecisionVariants

A C# source generator that generates overloads of methods with different numeric types:

Currently it only supports generating a `float` overload for a method that expects or returns a `double`, is in a partial class, and is annotated with `[GenerateFloatVariant]` .

## Example

```csharp
public static partial class Statistics
{
    /// <summary>
    /// Calculates the standard deviation for a survey that covers the entire population.
    /// For example: the height of all German professional football players.
    /// </summary>
    [GenerateFloatVariant]
    public static double PopulationStandardDeviation(double mean, params ReadOnlySpan<double> values)
    {        
        var sum = 0.0;
        for (var i = 0; i < values.Length; i++)
        {
            var difference = values[i] - mean;
            sum += difference * difference;
        }

        return Math.Sqrt(sum / values.Length);
    }
}
```

Generates the following overload

```csharp
partial class Statistics
{
    /// <summary>
    /// Calculates the standard deviation for a survey that covers the entire population.
    /// For example: the height of all German professional football players.
    /// </summary>
    [global::CapriKit.PrecisionVariants.GenerateFloatVariant]
    public static float PopulationStandardDeviation(float mean, params global::System.ReadOnlySpan<float> values)
    {
        var sum = 0F;
        for (var i = 0; i < values.Length; i++)
        {
            var difference = values[i] - mean;
            sum += difference * difference;
        }

        return global::System.MathF.Sqrt(sum / values.Length);
    }
}
```
