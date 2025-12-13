using CapriKit.Mathematics;

namespace CapriKit.Tests.Mathematics;

internal class StatisticsTests
{
    [Test]
    public async Task Mean_OfArray_ReturnsAverage()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];        
        await Assert.That(Statistics.Mean(values)).IsEqualTo(3.0);        
    }

    [Test]
    public async Task PopulationStandardDeviation_OfArray_ReturnsSD()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        var mean = Statistics.Mean(values);
        double sd = 1.414;
        await Assert.That(Statistics.PopulationStandardDeviation(mean, values)).IsEqualTo(sd).Within(0.001);
    }

    [Test]
    public async Task SampleStandardDeviation_OfArray_ReturnsSD()
    {
        double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];
        var mean = Statistics.Mean(values);
        double sd = 1.581;
        await Assert.That(Statistics.SampleStandardDeviation(mean, values)).IsEqualTo(sd).Within(0.001);
    }

    [Test]
    public async Task StandardError_ReturnsSE()
    {
        double sd = 1.581;
        double se = 0.707;
        await Assert.That(Statistics.StandardError(sd, 5.0)).IsEqualTo(se).Within(0.001);
    }
}
