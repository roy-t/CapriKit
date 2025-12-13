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
    }

    [Test]
    public async Task ComputeTwoTailedProbabilityOfT_ReturnsP()
    {        
        await Assert.That(StudentTTest.ComputeTwoTailedProbabilityOfT(1.4142, 4)).IsEqualTo(0.230).Within(0.001);
    }

    [Test]
    public async Task ForIndepentnSamples_ReturnsT()
    {
        var mean = 3.0;        
        var sd = 1.581;
        var count = 5;

        var altMean = 2.0;
        var altSd = 1.345;
        var altCount = 7;
        await Assert.That(StudentTTest.ForIndependentSamples(mean, sd, count, altMean, altSd, altCount))
            .IsEqualTo(1.148).Within(0.001);
    }

    [Test]
    public async Task GetDegreesOfFreedom_ForTwoPairedTest_ReturnsDF()
    {
        // TODO: this should use the GetDegreesOfFreedom test
        Assert.Fail("TODO: use the new DoF calculator!");
    }
}
