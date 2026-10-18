using CapriKit.DirectX11;
using CapriKit.Geometry;
using CapriKit.Geometry.Blender;
using CapriKit.IO;
using System.Buffers;
using System.Diagnostics;

namespace CapriKit.AssetPipeline.DirectX11.Models;

// TODO: what if this transcoder just imports a .obj and .mtl file and converts it to a serialized version of a CKTModel?
// it would fit much cleaner in the design and if we get rid of the LODDING .obj and .mtl have all the info we need.
// MiniEngine already has a serviceable .obj/mtl importer. For Blender we could write a plugin that auto exports on save
// and does something per collection or something like that. Then later a 'LoddedModel<T>' or something like that could
// grab multiple assets and do the lodding administration.
public sealed class CKTModelTranscoder(Device device, BlenderConfiguration config) :
    NoSettingsTranscoder<CKTModel>(Guid.Parse("{0EAB33E4-853C-4659-A26C-CCC86B8E7EF0}"), 2)
{
    public override async Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        // HACK: open the file for reading to get it in the dependency list
        await using (fileSystem.OpenRead(id.Path)) { }
        var blendFile = fileSystem.GetAbsolutePath(id.Path);

        var workDirectory = Directory.CreateTempSubdirectory("CapriKit.Blender.");
        try
        {
            var outputFile = Path.Join(workDirectory.FullName, "output.ckt");
            await RunExportScript(blendFile, id.Key, outputFile);
            var bytes = await File.ReadAllBytesAsync(outputFile);

            // Validate once while encoding, so decoding can skip the expensive checks
            await CKTModelParser.Parse(new MemoryStream(bytes), validateData: true);
            writer.Write(bytes);
        }
        finally
        {
            workDirectory.Delete(recursive: true);
        }
    }

    private async Task RunExportScript(string blendFile, string collection, string outputFile)
    {
        // The asset handle is relative to the Blender content root, which is copied to the output directory
        var script = Path.Combine(AppContext.BaseDirectory, "Blender", ExportCkt.py);
        var startInfo = new ProcessStartInfo(config.PathToBlenderExecutable)
        {
            // --factory-startup ignores user add-ons and preferences so exports are reproducible
            ArgumentList = { "--background", "--factory-startup", blendFile, "--python-exit-code", "1", "--python", script, "--", collection, outputFile },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo) ?? throw new Exception($"Could not start {startInfo.FileName}");

        // Drain both pipes while waiting, Blender blocks when a pipe buffer is full
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Export of collection '{collection}' failed with exit code {process.ExitCode}:\n{await output}\n{await error}");
        }
    }

    public override CKTModel Decode(AssetId id, ref SequenceReader<byte> reader)
    {
        var data = CKTModelParser.Parse(ref reader);
        return new CKTModel(device, data);
    }

    public override void HotSwap(CKTModel instance, CKTModel newParts)
    {
        instance.HotSwap(newParts);
    }
}
