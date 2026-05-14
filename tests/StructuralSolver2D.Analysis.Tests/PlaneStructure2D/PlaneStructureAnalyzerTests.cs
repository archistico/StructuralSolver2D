using StructuralSolver2D.Analysis;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.PlaneStructure2D;

/// <summary>
/// Validation tests for the mixed plane-structure analyzer.
/// </summary>
public sealed class PlaneStructureAnalyzerTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;

    [Fact]
    public void Analyze_PureFrameModel_ShouldMatchFrame2DAnalyzer()
    {
        StructuralModel model = CreateSimpleSupportedBeamModel();

        var frameResult = new Frame2DAnalyzer().Analyze(model, "LC1");
        var mixedResult = new PlaneStructureAnalyzer().Analyze(model, "LC1");

        Assert.Equal(frameResult.GetReaction("SA").Fy, mixedResult.GetReaction("SA").Fy, precision: 9);
        Assert.Equal(frameResult.GetReaction("SB").Fy, mixedResult.GetReaction("SB").Fy, precision: 9);
        Assert.Equal(frameResult.GetDisplacement("B").Uy, mixedResult.GetDisplacement("B").Uy, precision: 12);
        Assert.Equal(frameResult.MemberEndForces.Single().StartMoment, mixedResult.MemberEndForces.Single().StartMoment, precision: 9);
    }

    [Fact]
    public void Analyze_PureTrussModel_ShouldMatchTruss2DAnalyzer()
    {
        StructuralModel model = CreateAxialTrussBarModel();

        var trussResult = new Truss2DAnalyzer().Analyze(model, "LC1");
        var mixedResult = new PlaneStructureAnalyzer().Analyze(model, "LC1");

        Assert.Equal(trussResult.GetReaction("SA").Fx, mixedResult.GetReaction("SA").Fx, precision: 9);
        Assert.Equal(trussResult.GetDisplacement("B").Ux, mixedResult.GetDisplacement("B").Ux, precision: 12);
        Assert.Equal(trussResult.MemberEndForces.Single().EndAxial, mixedResult.MemberEndForces.Single().EndAxial, precision: 9);
    }

    [Fact]
    public void Analyze_MixedPortalWithTrussBrace_ShouldCompleteAndSatisfyGlobalEquilibrium()
    {
        StructuralModel model = CreateMixedBracedPortalModel()
            .AddLoad(StructuralLoad.NodalForce("H1", "LC1", "D", StructuralLoadDirection.GlobalX, 20.0));

        var result = new PlaneStructureAnalyzer().Analyze(model, "LC1");

        double sumFx = result.Reactions.Sum(reaction => reaction.Fx) + 20.0;
        double sumFy = result.Reactions.Sum(reaction => reaction.Fy);
        double braceNormalForce = result.MemberEndForces.Single(force => force.MemberId == "T1").EndAxial;

        Assert.Equal(0.0, sumFx, precision: 6);
        Assert.Equal(0.0, sumFy, precision: 6);
        Assert.NotEqual(0.0, Math.Round(braceNormalForce, 6));
    }

    [Fact]
    public void Analyze_MixedModelWithMemberLoadOnTruss_ShouldThrowClearException()
    {
        StructuralModel model = CreateMixedBracedPortalModel()
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "T1", StructuralLoadDirection.GlobalY, -5.0));

        var analyzer = new PlaneStructureAnalyzer();

        var exception = Assert.Throws<StructuralAnalysisException>(() => analyzer.Analyze(model, "LC1"));
        Assert.Contains("Frame2D", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Truss2D", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static StructuralModel CreateBaseModel() =>
        new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddLoadCase(new StructuralLoadCase("LC1", "Main load case"));

    private static StructuralModel CreateSimpleSupportedBeamModel() =>
        CreateBaseModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 5.0, 0.0))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10.0));

    private static StructuralModel CreateAxialTrussBarModel() =>
        CreateBaseModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 5.0, 0.0))
            .AddMember(new StructuralMember("T1", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(new StructuralSupport("SB", "B", false, true, false, SupportType.Custom, "Vertical restraint"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalX, 100.0));

    private static StructuralModel CreateMixedBracedPortalModel() =>
        CreateBaseModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 4.0, 0.0))
            .AddNode(new StructuralNode("C", 0.0, 3.0))
            .AddNode(new StructuralNode("D", 4.0, 3.0))
            .AddMember(new StructuralMember("F1", "A", "C", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("F2", "C", "D", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("F3", "B", "D", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("T1", "A", "D", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(StructuralSupport.Fixed("SB", "B"));
}
