using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace CapriKit.Tests.Tool;

// Prototype to see how to do async/multi-threaded loading work.
// Remember that ID3D11Device is free threaded (unless if we create it with D3D11_CREATE_DEVICE_SINGLETHREADED)
// things that require a ID3D11Context are not!
// (Each thread can have its own deferred context and sync with the main threads immediate context)

internal record LoadedShader(byte[] bytes);
internal sealed class ShaderProducerConsumer
{
    private readonly BlockingCollection<string> work = new();
    private readonly Channel<LoadedShader> results =
        Channel.CreateUnbounded<LoadedShader>(new() { SingleReader = true, SingleWriter = false });

    public ShaderProducerConsumer(int concurrency)
    {
        for (var i = 0; i < concurrency; i++)
            new Thread(Run) { IsBackground = true, Name = $"ShaderLoad {i}" }.Start();
    }

    public void Enqueue(string source) => work.Add(source);
    public void CompleteAdding() => work.CompleteAdding();      // happy-path shutdown
    public bool TryDequeue([NotNullWhen(true)] out LoadedShader? shader) => results.Reader.TryRead(out shader);

    private void Run()// If we want to do async IO we can also do Parallel.ForEachAsync
    {
        foreach (var source in work.GetConsumingEnumerable())  // ends after CompleteAdding + drain
        {
            try { results.Writer.TryWrite(DoMagic(source)); }
            catch (Exception ex) { /* route failure to results/log */ }
        }
    }

    private static LoadedShader DoMagic(string source)
    {
        return new LoadedShader([]);
    }
}
