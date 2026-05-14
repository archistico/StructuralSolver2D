using StructuralSolver2D.Analysis;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Truss2D;

/// <summary>
/// Validation tests for the first Truss2D axial-only solver.
/// </summary>
public sealed class Truss2DAnalyzerTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;
    private const double AxialRigidity = ElasticModulus * Area;

    [Fact]
    public void Analyze_HorizontalAxialBar_ShouldMatchClosedFormElongationReactionAndNormalForce()
    {
        const double length = 5.0;
        const double force = 100.0;

        StructuralModel model = CreateBaseModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", length, 0.0))
            .AddMember(new StructuralMember("T1", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(new StructuralSupport("SB", "B", false, true, false, SupportType.Custom, "Vertical roller"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalX, force));

        Truss2DAnalyzer analyzer = new();

        var result = analyzer.Analyze(model, "LC1");

        double expectedElongation = force * length / AxialRigidity;
        var memberForce = result.MemberEndForces.Single();

        Assert.Equal(-force, result.GetReaction("SA").Fx, precision: 6);
        Assert.Equal(expectedElongation, result.GetDisplacement("B").Ux, precision: 12);
        Assert.Equal(0.0, result.GetDisplacement("B").Uy, precision: 12);
        Assert.Equal(0.0, result.GetDisplacement("B").Rz, precision: 12);
        Assert.Equal(-force, memberForce.StartAxial, precision: 6);
        Assert.Equal(force, memberForce.EndAxial, precision: 6);
    }

    [Fact]
    public void Analyze_SymmetricTriangularTruss_ShouldMatchClosedFormSupportReactions()
    {
        StructuralModel model = CreateSymmetricTriangularTruss()
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "C", StructuralLoadDirection.GlobalY, -30.0));

        Truss2DAnalyzer analyzer = new();

        var result = analyzer.Analyze(model, "LC1");

        const double expectedBottomTension = 10.0;
        const double bottomChordLength = 4.0;
        double expectedTopHorizontalDisplacement = expectedBottomTension * bottomChordLength / AxialRigidity / 2.0;

        Assert.Equal(15.0, result.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(15.0, result.GetReaction("SB").Fy, precision: 6);
        Assert.Equal(0.0, result.GetReaction("SA").Fx, precision: 6);
        Assert.Equal(expectedTopHorizontalDisplacement, result.GetDisplacement("C").Ux, precision: 12);
    }

    [Fact]
    public void Analyze_SymmetricTriangularTruss_ShouldReturnExpectedAxialForces()
    {
        StructuralModel model = CreateSymmetricTriangularTruss()
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "C", StructuralLoadDirection.GlobalY, -30.0));

        Truss2DAnalyzer analyzer = new();

        var result = analyzer.Analyze(model, "LC1");

        double diagonalLength = Math.Sqrt(13.0);
        double expectedDiagonalCompression = -(30.0 * diagonalLength / 6.0);
        const double expectedBottomTension = 10.0;

        Assert.Equal(expectedDiagonalCompression, GetNormalForce(result, "AC"), precision: 6);
        Assert.Equal(expectedDiagonalCompression, GetNormalForce(result, "BC"), precision: 6);
        Assert.Equal(expectedBottomTension, GetNormalForce(result, "AB"), precision: 6);
    }

    [Fact]
    public void AnalyzeCombination_ShouldApplyManualLoadFactorsToTrussReactions()
    {
        StructuralModel model = CreateSymmetricTriangularTruss()
            .AddLoadCase(new StructuralLoadCase("Q1", "Variable load"))
            .AddLoad(StructuralLoad.NodalForce("G", "LC1", "C", StructuralLoadDirection.GlobalY, -10.0))
            .AddLoad(StructuralLoad.NodalForce("Q", "Q1", "C", StructuralLoadDirection.GlobalY, -20.0))
            .AddLoadCombination(new StructuralLoadCombination(
                "ULS1",
                "Manual ULS",
                new[]
                {
                    new StructuralLoadCombinationTerm("LC1", 1.35),
                    new StructuralLoadCombinationTerm("Q1", 1.50),
                }));

        Truss2DAnalyzer analyzer = new();

        var result = analyzer.AnalyzeCombination(model, "ULS1");

        double totalFactoredLoad = (10.0 * 1.35) + (20.0 * 1.50);
        Assert.Equal(totalFactoredLoad / 2.0, result.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(totalFactoredLoad / 2.0, result.GetReaction("SB").Fy, precision: 6);
    }

    [Fact]
    public void Analyze_TrussWithMemberDistributedLoad_ShouldThrowClearException()
    {
        StructuralModel model = CreateBaseModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 5.0, 0.0))
            .AddMember(new StructuralMember("T1", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(new StructuralSupport("SB", "B", false, true, false))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "T1", StructuralLoadDirection.GlobalY, -10.0));

        Truss2DAnalyzer analyzer = new();

        var exception = Assert.Throws<StructuralAnalysisException>(() => analyzer.Analyze(model, "LC1"));
        Assert.Contains("nodal force", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FrameSampler_TrussResult_ShouldReturnConstantNormalForceAndZeroShearMoment()
    {
        StructuralModel model = CreateBaseModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 5.0, 0.0))
            .AddMember(new StructuralMember("T1", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(new StructuralSupport("SB", "B", false, true, false))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalX, 100.0));

        Truss2DAnalyzer analyzer = new();
        var result = analyzer.Analyze(model, "LC1");
        var sampler = new Frame2DInternalForceSampler();

        var diagram = sampler.SampleMember(model, result, "T1", sampleCount: 5);

        Assert.All(diagram.Samples, sample =>
        {
            Assert.Equal(100.0, sample.NormalForce, precision: 6);
            Assert.Equal(0.0, sample.ShearForce, precision: 12);
            Assert.Equal(0.0, sample.BendingMoment, precision: 12);
        });
    }

    private static double GetNormalForce(StructuralSolver2D.Analysis.Results.StructuralAnalysisResult result, string memberId)
    {
        var force = result.MemberEndForces.Single(item => item.MemberId == memberId);
        return force.EndAxial;
    }

    private static StructuralModel CreateBaseModel() =>
        new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic truss section", Area, Inertia))
            .AddLoadCase(new StructuralLoadCase("LC1", "Main load case"));

    private static StructuralModel CreateSymmetricTriangularTruss() =>
        CreateBaseModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 4.0, 0.0))
            .AddNode(new StructuralNode("C", 2.0, 3.0))
            .AddMember(new StructuralMember("AC", "A", "C", "MAT", "SEC", MemberType.Truss2D))
            .AddMember(new StructuralMember("BC", "B", "C", "MAT", "SEC", MemberType.Truss2D))
            .AddMember(new StructuralMember("AB", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
}
