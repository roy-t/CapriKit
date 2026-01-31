using CapriKit.Mathematics;

namespace CapriKit.Tests.Mathematics;

// Numbers verified using https://www.omnicalculator.com/statistics/t-test and https://www.omnicalculator.com/statistics/degrees-of-freedom

internal class StudentTTestTests
{
    [Test]
    public async Task ForOneSample()
    {
        var mean = 3.0;
        var sd = 1.581;
        var count = 5;
        var altMean = 2.0;

        var t = StudentTTest.ForOneSample(mean, sd, count, altMean);
        await Assert.That(t).IsEqualTo(1.414).Within(0.001);

        var dof = StudentTTest.GetDegreesOfFreedom(5);
        await Assert.That(dof).IsEqualTo(4);

        var p = StudentTTest.ComputeTwoTailedProbabilityOfT(t, dof);
        await Assert.That(p).IsEqualTo(0.230).Within(0.001);
    }

    [Test]
    public async Task ForIndependentSamples()
    {
        var mean = 3.0;
        var sd = 1.581;
        var count = 5;

        var altMean = 2.0;
        var altSd = 1.345;
        var altCount = 7;

        var t = StudentTTest.ForIndependentSamples(mean, sd, count, altMean, altSd, altCount);
        await Assert.That(t).IsEqualTo(1.148).Within(0.001);

        var dof = StudentTTest.GetDegreesOfFreedom(1.581, 5, 1.345, 7);
        await Assert.That(dof).IsEqualTo(7.813).Within(0.001);

        var p = StudentTTest.ComputeTwoTailedProbabilityOfT(t, dof);
        await Assert.That(p).IsEqualTo(0.284).Within(0.001);
    }

    [Test]
    public async Task ForPairedSamples()
    {
        double[] before = [1.0, 2.0, 3.0, 4.0, 5.0];
        double[] after = [2.0, 2.1, 2.2, 2.3, 2.4];

        var t = StudentTTest.ForPairedSamples(before, after);
        await Assert.That(t).IsEqualTo(1.257).Within(0.001);

        var dof = StudentTTest.GetDegreesOfFreedom(5);
        await Assert.That(dof).IsEqualTo(4);

        var p = StudentTTest.ComputeTwoTailedProbabilityOfT(t, dof);
        await Assert.That(p).IsEqualTo(0.277).Within(0.001);
    }

    [Test]
    public async Task GetDegreesOfFreedom_ForOneSampleOrPairedSamples()
    {
        var dof = StudentTTest.GetDegreesOfFreedom(5);
        await Assert.That(dof).IsEqualTo(4);
    }

    [Test]
    public async Task GetDegreesOfFreedom_ForTwoPairedTest()
    {
        var dof = StudentTTest.GetDegreesOfFreedom(1.581, 5, 1.345, 7);
        await Assert.That(dof).IsEqualTo(7.813).Within(0.001);
    }
}
