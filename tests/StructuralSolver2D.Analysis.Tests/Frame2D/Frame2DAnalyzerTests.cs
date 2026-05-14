using StructuralSolver2D.Analysis;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

public sealed class Frame2DAnalyzerTests
{
    [Fact]
    public void Analyze_SimplySupportedBeamWithUniformLoad_ShouldReturnExpectedVerticalReactions()
    {
        StructuralModel model = CreateBaseBeamModel()
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10.0));

        Frame2DAnalyzer analyzer = new();

        var result = analyzer.Analyze(model, "LC1");

        Assert.Equal(25.0, result.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(25.0, result.GetReaction("SB").Fy, precision: 6);
        Assert.Equal(0.0, result.GetReaction("SA").Fx, precision: 6);
        Assert.Equal(0.0, result.GetReaction("SA").Mz, precision: 6);
        Assert.Equal(0.0, result.GetDisplacement("A").Uy, precision: 12);
        Assert.Equal(0.0, result.GetDisplacement("B").Uy, precision: 12);
    }

    [Fact]
    public void Analyze_CantileverWithTipLoad_ShouldReturnExpectedFixedReaction()
    {
        StructuralModel model = CreateBaseBeamModel()
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalY, -10.0));

        Frame2DAnalyzer analyzer = new();

        var result = analyzer.Analyze(model, "LC1");

        Assert.Equal(10.0, result.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(50.0, result.GetReaction("SA").Mz, precision: 6);
        Assert.True(result.GetDisplacement("B").Uy < 0);
    }

    [Fact]
    public void Analyze_UnstableModel_ShouldThrowStructuralAnalysisException()
    {
        StructuralModel model = CreateBaseBeamModel()
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalY, -10.0));

        Frame2DAnalyzer analyzer = new();

        StructuralAnalysisException exception = Assert.Throws<StructuralAnalysisException>(() => analyzer.Analyze(model, "LC1"));
        Assert.Contains("singular", exception.Message.ToLowerInvariant());
    }

    private static StructuralModel CreateBaseBeamModel() =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 5.0, 0.0))
            .AddMaterial(new StructuralMaterial("S235", "Steel S235", 210_000_000.0))
            .AddSection(new StructuralSection("SEC1", "Generic section", 0.003, 0.00002))
            .AddMember(new StructuralMember("M1", "A", "B", "S235", "SEC1", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("LC1", "Test load case"));
}
