using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Cli.Tests.Input;

public sealed class SelfWeightJsonM76Tests
{
    [Fact]
    public void StandardJson_RoundTripKeepsExplicitSelfWeightAndUnitWeight()
    {
        var model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddMaterial(new StructuralMaterial("steel", "Steel", 210_000_000, 78.5))
            .AddSection(new StructuralSection("sec", "section", 0.005, 1.0e-5))
            .AddMember(new StructuralMember("beam", "A", "B", "steel", "sec"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoadCase(new StructuralLoadCase("G", "Weight"))
            .AddLoad(StructuralLoad.SelfWeightLoad("SW", "G"));
        var file = new StructuralModelJsonFile("Self-weight", "M76", "G", model);
        var json = StructuralModelJsonWriter.Serialize(file);
        string path = Path.Combine(Path.GetTempPath(), $"m76-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, json);
            var restored = StructuralModelJsonReader.ReadValidated(path);
            Assert.Equal(78.5, restored.Model.Materials.Single().UnitWeight!.Value, 12);
            Assert.Equal(StructuralLoadType.SelfWeight, restored.Model.Loads.Single().Type);
            Assert.Equal(1.0, restored.Model.Loads.Single().Value, 12);
            Assert.True(new StructuralModelValidator().Validate(restored.Model).IsValid);
            Assert.Equal(json, StructuralModelJsonWriter.Serialize(restored));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
