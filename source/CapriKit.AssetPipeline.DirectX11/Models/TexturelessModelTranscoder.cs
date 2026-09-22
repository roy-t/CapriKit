using CapriKit.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace CapriKit.AssetPipeline.DirectX11.Models;

internal class TexturelessModelTranscoder : NoSettingsTranscoder<CKTModel>
{
    public override CKTModel Decode(AssetId id, ref SequenceReader<byte> reader)
    {
        throw new NotImplementedException();
    }

    public override Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }

    public override void HotSwap(CKTModel instance, CKTModel newParts)
    {
        throw new NotImplementedException();
    }
}
