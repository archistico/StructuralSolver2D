using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Cli.Tests.Input;

public sealed class ElasticSupportJsonM77Tests
{
    [Fact]
    public void RoundTrip_PreservesElasticLocalSupportAndSettlementCase()
    {
        var model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2, 0))
            .AddMaterial(new StructuralMaterial("M", "steel", 200000))
            .AddSection(new StructuralSection("S", "area", .01, .000001))
            .AddMember(new StructuralMember("BAR", "A", "B", "M", "S", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(new StructuralSupport("SB", "B", false, true, false,
                OrientationDegrees: 15, StiffnessUx: 100, StiffnessRz: 50))
            .AddLoadCase(new StructuralLoadCase("L", "settlement"))
            .AddLoad(StructuralLoad.SupportSettlementLoad("SET", "L", "SA", StructuralLoadDirection.LocalX, -.005));
        var file = new StructuralModelJsonFile("M77", "elastic support", "L", model);
        string original = StructuralModelJsonWriter.Serialize(file);
        string path = Path.Combine(Path.GetTempPath(), $"m77-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, original);
            var read = StructuralModelJsonReader.ReadValidated(path);
            Assert.Equal(100, read.Model.Supports.Single(x => x.Id == "SB").StiffnessUx, 12);
            Assert.Equal(50, read.Model.Supports.Single(x => x.Id == "SB").StiffnessRz, 12);
            Assert.Equal(15, read.Model.Supports.Single(x => x.Id == "SB").OrientationDegrees, 12);
            Assert.Equal(StructuralLoadType.SupportSettlement, Assert.Single(read.Model.Loads).Type);
            Assert.Equal(StructuralLoadTargetType.Support, Assert.Single(read.Model.Loads).TargetType);
            Assert.Equal(original, StructuralModelJsonWriter.Serialize(read));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
