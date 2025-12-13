using MathNet.Numerics.Statistics;
using MathNet.Numerics.Distributions;

namespace CapriKit.Mathematics;

/// <summary>
/// Use Student's t-test to calculate if there is a significant difference between the means of two groups.
/// For all tests the null hypothesis is that there is no difference (t is close to zero). The alternative
/// hypothesis is that there is a difference (t becomes larger).
/// </summary>
public static class StudentTTest
{
    /// <summary>
    /// Determines if the results of a survey are significantly different from a known value. 
    /// For example: to compare the results of weighing 50 items to a manufacturer's claimed average weight.
    /// </summary>    
    public static double ForOneSample(double mean, double standardDeviation, int count, double referenceMean)
    {
        var standardError = Statistics.StandardError(standardDeviation, count);
        return (mean - referenceMean) / standardError;
    }

    /// <summary>
    /// Determines if two surveys with unrelated subjects are significantly different.
    /// For example: to compare the effectiveness of two different schools by measuring final exam scores.
    /// </summary>
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
    /// Determines if two dependent surveys are significantly different. The before and after samples
    /// need to be exactly the same length.
    /// For example: to compare the weight of exactly the same people before and after a diet.
    /// </summary>    
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

    public static int GetDegreesOfFreedom(int count)
    {
        return count - 1;
    }

    public static int GetDegreesOfFreedom(int countA, int countB)
    {
        return countA + countB  - 2;
    }    

    /// <summary>
    /// Computes the probability of seeing this t-value if the means of the two surveys are equal.
    /// </summary>    
    public static double ComputeTwoTailedProbabilityOfT(double tValue, int degreesOfFreedom)
    {
        var cdf = StudentT.CDF(0.0, 1.0, degreesOfFreedom, Math.Abs(tValue));
        return 2 - 2 * cdf;
    }
}
