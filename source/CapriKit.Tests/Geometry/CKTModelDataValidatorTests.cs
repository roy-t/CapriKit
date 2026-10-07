using CapriKit.Geometry;

namespace CapriKit.Tests.Geometry;

internal class CKTModelDataValidatorTests
{
    [Test]
    public async Task Validate()
    {
        // Quick smoke check that the validator and generated cube are in sync
        var model = CKTModelGenerator.CreateUnitCube();
        CKTModelDataValidator.Validate(model);

        await Task.CompletedTask;
    }
}
