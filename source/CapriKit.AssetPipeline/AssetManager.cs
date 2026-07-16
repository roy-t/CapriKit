namespace CapriKit.AssetPipeline;

public class AssetManager
{

    private readonly Dictionary<Type, IAssetEncoder> Encoders = [];
    private readonly Dictionary<Type, IAssetDecoder> Decoders = [];

    public void RegisterTranscoder<T>(IAssetEncoder encoder, IAssetDecoder<T> decoder)
    {
        var typeKey = typeof(T);
        Encoders[typeKey] = encoder;
        Decoders[typeKey] = decoder;
    }

    public T Decode<T>(AssetId id)
    {
        var typeKey = typeof(T);
        var encoder = Encoders[typeKey];
        var decoder = Decoders[typeKey];

        var extension = new string(id.Path.Extension);
        if (encoder.SupportedExtensions.Contains(extension))
        {

        }

        throw new NotImplementedException();
    }
}
