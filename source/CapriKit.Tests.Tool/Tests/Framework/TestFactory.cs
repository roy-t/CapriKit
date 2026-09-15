using CapriKit.AssetPipeline;
using Microsoft.Extensions.DependencyInjection;

namespace CapriKit.Tests.Tool.Tests.Framework;

internal interface ITestFactory : IDisposable
{
    public bool TryCreate(IServiceProvider provider, List<ITestScreen> tests);

    public string Name { get; }
    public int Total { get; }
    public int Loaded { get; }
    public AssetId? LastCompletedItem { get; }
}

internal sealed class TestFactory<TTest>(string name) : ITestFactory
    where TTest : ITestScreen
{
    private static readonly ObjectFactory<TTest> Activate =
      ActivatorUtilities.CreateFactory<TTest>([]);

    private bool created;
    public string Name { get; } = name;
    public int Total => 0;
    public int Loaded => 0;
    public AssetId? LastCompletedItem { get; }

    public bool TryCreate(IServiceProvider provider, List<ITestScreen> tests)
    {
        if (created) { return true; }
        var instance = Activate(provider, []);
        tests.Add(instance);
        created = true;
        return created;
    }

    public void Dispose() { }
}

internal sealed class TestFactory<TTest, TBundle> : ITestFactory
    where TTest : ITestScreen
    where TBundle : class
{
    private static readonly ObjectFactory<TTest> Activate =
        ActivatorUtilities.CreateFactory<TTest>([typeof(TBundle)]);

    private readonly AssetBundle<TBundle> Bundle;
    private bool created;

    public TestFactory(string name, AssetBundle<TBundle> bundle)
    {
        Bundle = bundle;
        Name = name;
    }

    public string Name { get; }
    public int Total => Bundle.Total;
    public int Loaded => Bundle.Loaded;
    public AssetId? LastCompletedItem => Bundle.LastCompletedItem;

    public bool TryCreate(IServiceProvider provider, List<ITestScreen> tests)
    {
        if (created) { return true; }

        if (Bundle.IsReady(out var contents))
        {
            var test = Activate(provider, [contents]);
            tests.Add(test);
            created = true;
        }

        return created;
    }

    public void Dispose()
    {
        Bundle.Dispose();
    }
}
