using CapriKit.Mathematics;

namespace CapriKit.Tests.Mathematics;

internal class StudentTTestTests
{
    [Test]
    public async Task ForOneSample_ReturnsT()
    {        
        var mean = 3.0;
        var sd = 1.581;
        var count = 5;
        var altMean = 2.0;
        await Assert.That(StudentTTest.ForOneSample(mean, sd, count, altMean)).IsEqualTo(1.414).Within(0.001);
        
        // P: 0,23019964
    }

    [Test]
    public async Task ComputeTwoTailedProbabilityOfT_ReturnsP()
    {        
        await Assert.That(StudentTTest.ComputeTwoTailedProbabilityOfT(1.4142, 4)).IsEqualTo(0.230).Within(0.001);
    }

    [Test]
    public async Task ForIndepentnSamples_ReturnsT()
    {
        // TODO: results depent on variance being equal or not see: 
        // https://www.omnicalculator.com/statistics/t-test
        var mean = 3.0;        
        var sd = 1.581;
        var count = 5;

        var altMean = 2.0;
        var altSd = 1.345;
        var altCount = 7;
        await Assert.That(StudentTTest.ForIndependentSamples(mean, sd, count, altMean, altSd, altCount))
            .IsEqualTo(1.148).Within(0.001);
    }
}
