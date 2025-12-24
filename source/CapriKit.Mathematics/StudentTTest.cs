using CapriKit.PrecisionVariants;
using MathNet.Numerics.Distributions;

namespace CapriKit.Mathematics;

/// <summary>
/// Use Student's t-test to calculate if there is a significant difference between the means of two groups.
/// For all tests the null hypothesis is that there is no difference (t is close to zero). The alternative
/// hypothesis is that there is a difference (t becomes larger).
/// </summary>
public static partial class StudentTTest
{
    /// <summary>
    /// Determines if the results of a survey are likely given a known value. 
    /// For example: to compare the results of weighing 50 items to a manufacturer's claimed average weight.
    /// </summary>
    /// <returns>
    /// The t-score
    /// </returns>
    //[GenerateFloatVariant]
    public static double ForOneSample(double mean, double standardDeviation, int count, double referenceMean)
    {
        var standardError = Statistics.StandardError(standardDeviation, count);
        return (mean - referenceMean) / standardError;
    }

    /// <summary>
    /// Determines if it is likely that two surveys with unrelated subjects have similar properties.
    /// Uses the Welch's t-tests, which assumes the variances are not equal or unknown.
    /// For example: to compare the effectiveness of two different schools by measuring final exam scores.
    /// </summary>
    /// <returns>
    /// The t-score
    /// </returns>
    //[GenerateFloatVariant]
    public static double ForIndependentSamples(
        double meanA, double standardDeviationA, int countA,
        double meanB, double standardDeviationB, int countB)
    {
        var top = meanA - meanB;
        var errorA = (standardDeviationA * standardDeviationA) / countA;
        var errorB = (standardDeviationB * standardDeviationB) / countB;
        var bottom = Math.Sqrt(errorA + errorB);

        return top / bottom;
    }

    /// <summary>
    /// Determines if it is likely that the results of two dependent surveys are smilar.
    /// The before and after samples need to be exactly the same length (you are surveying the same subjects)
    /// For example: to compare the weight of a group of people before and after a diet.
    /// </summary>
    /// <returns>
    /// The t-score
    /// </returns>
    //[GenerateFloatVariant]
    public static double ForPairedSamples(ReadOnlySpan<double> before, ReadOnlySpan<double> after)
    {
        if (before.Length != after.Length)
        {
            throw new Exception("In a Student's t-test for dependent samples both the before and after group need to have the same number of samples");
        }

        var differences = new double[before.Length];
        for (var i = 0; i < before.Length; i++)
        {
            differences[i] = before[i] - after[i];
        }

        var mean = Statistics.Mean(differences);
        var standardDeviation = Statistics.SampleStandardDeviation(mean, differences);
        var error = Statistics.StandardError(standardDeviation, before.Length);
        return mean / error;
    }

    /// <summary>
    /// Calculates the degrees of freedom for a one-sample or paired t-test.
    /// </summary>
    public static int GetDegreesOfFreedom(int count)
    {
        return count - 1;
    }

    /// <summary>
    /// Calculates the degrees of freedom for Wekch's two-sample t-test in which the variances are not equal or unknown.
    /// Uses the Welch-Satterthwaite equation.
    /// </summary>
    //[GenerateFloatVariant]
    public static double GetDegreesOfFreedom(double standardDeviationA, int countA, double standardDeviationB, int countB)
    {
        double varianceA = standardDeviationA * standardDeviationA;
        double varianceB = standardDeviationB * standardDeviationB;

        double termA = varianceA / countA;
        double termB = varianceB / countB;

        double numerator = Math.Pow(termA + termB, 2);

        double denominator =
            (Math.Pow(termA, 2) / (countA - 1)) +
            (Math.Pow(termB, 2) / (countB - 1));

        return numerator / denominator;
    }    

    /// <summary>
    /// Computes the probability of seeing this t-value if the H0 hypothesis is true
    /// Usually you reject the H0 hypothesis in favor of the H1 hypothesis
    /// if this function returns a value smaller than 0.05
    /// </summary>    
    public static double ComputeTwoTailedProbabilityOfT(double tValue, double degreesOfFreedom)
    {
        var cdf = StudentT.CDF(0.0, 1.0, degreesOfFreedom, Math.Abs(tValue));
        return 2 - 2 * cdf;
    }
}
