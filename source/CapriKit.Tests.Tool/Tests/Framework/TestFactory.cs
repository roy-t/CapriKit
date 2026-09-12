using CapriKit.AssetPipeline;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Metadata;

namespace CapriKit.Tests.Tool.Tests.Framework;

internal interface ITestFactory : IDisposable
{
    public bool TryCreate(IServiceProvider provider, List<ITestScreen> tests);

    public string Name { get; }
    public int Total { get; }
    public int Loaded { get; }
    public AssetId? LastCompletedItem { get; }
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

internal sealed class ShaderTestFactory
{


    private readonly AssetBundle<ShaderTestBundle> Bundle;

    public bool Resolve(IServiceProvider provider, List<ITestScreen> screens)
    {
        ObjectFactory <


        if (Bundle.IsReady(out var contents))
        {
            var test = ActivatorUtilities.CreateInstance<ShaderTest>(provider, contents);
            screens.Add(test);
            return true;
        }

        return false;
    }
}
