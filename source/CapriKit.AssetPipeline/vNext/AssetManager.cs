namespace CapriKit.AssetPipeline.vNext;

public sealed partial class AssetManager : IDisposable
{
    // TODO: register and then get transcoders
    private readonly AssetCache Cache = new(); 

    public Promise<TAsset> Load<TAsset, TSetting>(AssetId id, TSetting settings)
        where TAsset : class
    {
        var promise = new Promise<TAsset>();

        // TODO: capture the promise and set the .Value property as soon as the asset finishes loading
        // like in the OnCompleted capture of Task.FireAndForget, also do something with failures.
        // Ideally failures pop-up as soon as the promise is use to create the bundle

        throw new NotImplementedException();
    }

    public void Update()
    {
        
    }


    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
