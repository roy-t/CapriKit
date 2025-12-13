namespace CapriKit.Mathematics;

public static class Statistics
{
    /// <summary>
    /// Calculates the mean (average)
    /// </summary>
    public static double Mean(params ReadOnlySpan<double> values)
    {
        var sum = 0.0;
        for (var i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }

        return sum / values.Length;
    }

    /// <summary>
    /// Calculates the standard deviation for a survey that covers the entire population.
    /// For example: the height of all German professional football players.
    /// </summary>
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

    /// <summary>
    /// Calculates the standard deviation for a survey that covers only a sample of a population
    /// For example: in a clinical study.
    /// </summary>
    public static double SampleStandardDeviation(double mean, params ReadOnlySpan<double> values)
    {
        var sum = 0.0;
        for (var i = 0; i < values.Length; i++)
        {
            var difference = values[i] - mean;
            sum += difference * difference;
        }

        return Math.Sqrt(sum / (values.Length - 1.0));

    }

    /// <summary>
    /// Calculates the standard error, also known as standard deviation from the mean
    /// </summary>
    public static double StandardError(double standardDeviation, double count)
    {
        return standardDeviation / Math.Sqrt(count);
    }

}
