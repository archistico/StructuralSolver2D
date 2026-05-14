using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

/// <summary>
/// Validates post-processing of internal forces along 2D frame members.
/// </summary>
public sealed class Frame2DInternalForceSamplerTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;

    [Fact]
    public void SampleMember_SimplySupportedBeamWithUniformLoad_ShouldReturnExpectedShearAndMomentValues()
    {
        const double length = 5.0;
        const double load = 10.0;

        StructuralModel model = CreateSingleMemberBeamModel(length)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -load));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagram = new Frame2DInternalForceSampler().SampleMember(model, result, "M1", sampleCount: 11);

        var start = diagram.GetClosestSample(0.0);
        var middle = diagram.GetClosestSample(0.5);
        var end = diagram.GetClosestSample(1.0);

        Assert.Equal(length, diagram.Length, precision: 12);
        Assert.Equal(25.0, start.ShearForce, precision: 6);
        Assert.Equal(0.0, start.BendingMoment, precision: 6);
        Assert.Equal(0.0, middle.ShearForce, precision: 6);
        Assert.Equal(31.25, middle.BendingMoment, precision: 6);
        Assert.Equal(-25.0, end.ShearForce, precision: 6);
        Assert.Equal(0.0, end.BendingMoment, precision: 6);
        Assert.Equal(31.25, diagram.MaxAbsBendingMoment, precision: 6);
    }

    [Fact]
    public void SampleMember_CantileverWithUniformLoad_ShouldReturnHoggingMomentAtFixedEndAndZeroAtFreeEnd()
    {
        const double length = 5.0;
        const double load = 10.0;

        StructuralModel model = CreateSingleMemberBeamModel(length)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -load));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagram = new Frame2DInternalForceSampler().SampleMember(model, result, "M1", sampleCount: 11);

        var fixedEnd = diagram.GetClosestSample(0.0);
        var freeEnd = diagram.GetClosestSample(1.0);

        Assert.Equal(50.0, fixedEnd.ShearForce, precision: 6);
        Assert.Equal(-125.0, fixedEnd.BendingMoment, precision: 6);
        Assert.Equal(0.0, freeEnd.ShearForce, precision: 6);
        Assert.Equal(0.0, freeEnd.BendingMoment, precision: 6);
        Assert.Equal(125.0, diagram.MaxAbsBendingMoment, precision: 6);
    }

    [Fact]
    public void SampleMember_CantileverWithTipForce_ShouldReturnLinearMomentDiagram()
    {
        const double length = 5.0;
        const double force = 10.0;

        StructuralModel model = CreateSingleMemberBeamModel(length)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalY, -force));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagram = new Frame2DInternalForceSampler().SampleMember(model, result, "M1", sampleCount: 11);

        var fixedEnd = diagram.GetClosestSample(0.0);
        var middle = diagram.GetClosestSample(0.5);
        var freeEnd = diagram.GetClosestSample(1.0);

        Assert.Equal(10.0, fixedEnd.ShearForce, precision: 6);
        Assert.Equal(-50.0, fixedEnd.BendingMoment, precision: 6);
        Assert.Equal(10.0, middle.ShearForce, precision: 6);
        Assert.Equal(-25.0, middle.BendingMoment, precision: 6);
        Assert.Equal(10.0, freeEnd.ShearForce, precision: 6);
        Assert.Equal(0.0, freeEnd.BendingMoment, precision: 6);
        Assert.Equal(50.0, diagram.MaxAbsBendingMoment, precision: 6);
    }

    [Fact]
    public void SampleMember_AxiallyLoadedMember_ShouldReturnConstantPositiveTension()
    {
        const double length = 5.0;
        const double force = 100.0;

        StructuralModel model = CreateSingleMemberBeamModel(length)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(new StructuralSupport("SB", "B", false, true, true, SupportType.Custom, "Transverse and rotational restraint"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalX, force));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagram = new Frame2DInternalForceSampler().SampleMember(model, result, "M1", sampleCount: 5);

        Assert.All(diagram.Samples, sample =>
        {
            Assert.Equal(force, sample.NormalForce, precision: 6);
            Assert.Equal(0.0, sample.ShearForce, precision: 6);
            Assert.Equal(0.0, sample.BendingMoment, precision: 6);
        });
    }

    [Fact]
    public void SampleAllMembers_ShouldReturnOneDiagramPerAnalyzedMember()
    {
        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 3.0, 0.0))
            .AddNode(new StructuralNode("C", 6.0, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("M2", "B", "C", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("LC1", "First load case"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SC", "C"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalY, -12.0));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagrams = new Frame2DInternalForceSampler().SampleAllMembers(model, result, sampleCount: 7);

        Assert.Equal(2, diagrams.Count);
        Assert.Contains(diagrams, diagram => diagram.MemberId == "M1" && diagram.Samples.Count == 7);
        Assert.Contains(diagrams, diagram => diagram.MemberId == "M2" && diagram.Samples.Count == 7);
    }


    [Fact]
    public void SampleMember_SimplySupportedBeamWithEccentricPointLoad_ShouldReturnExpectedShearJumpAndMaximumMoment()
    {
        const double length = 8.0;
        const double force = 12.0;
        const double normalizedPosition = 0.25;

        StructuralModel model = CreateSingleMemberBeamModel(length)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.PointLoadOnMember("P1", "LC1", "M1", StructuralLoadDirection.GlobalY, -force, normalizedPosition));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagram = new Frame2DInternalForceSampler().SampleMember(model, result, "M1", sampleCount: 5);

        var start = diagram.GetClosestSample(0.0);
        var loadPosition = diagram.GetClosestSample(normalizedPosition);
        var threeQuarter = diagram.GetClosestSample(0.75);
        var end = diagram.GetClosestSample(1.0);

        Assert.Equal(9.0, start.ShearForce, precision: 6);
        Assert.Equal(18.0, loadPosition.BendingMoment, precision: 6);
        Assert.Equal(-3.0, threeQuarter.ShearForce, precision: 6);
        Assert.Equal(6.0, threeQuarter.BendingMoment, precision: 6);
        Assert.Equal(-3.0, end.ShearForce, precision: 6);
        Assert.Equal(0.0, end.BendingMoment, precision: 6);
        Assert.Equal(18.0, diagram.MaxAbsBendingMoment, precision: 6);
    }


    [Fact]
    public void SampleMember_SimplySupportedBeamWithTriangularDistributedLoad_ShouldReturnQuadraticShearAndCubicMoment()
    {
        const double length = 6.0;
        const double endLoad = 9.0;

        StructuralModel model = CreateSingleMemberBeamModel(length)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.LinearDistributedLoad("T1", "LC1", "M1", StructuralLoadDirection.GlobalY, 0.0, -endLoad));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagram = new Frame2DInternalForceSampler().SampleMember(model, result, "M1", sampleCount: 3);

        var start = diagram.GetClosestSample(0.0);
        var middle = diagram.GetClosestSample(0.5);
        var end = diagram.GetClosestSample(1.0);

        Assert.Equal(9.0, start.ShearForce, precision: 6);
        Assert.Equal(0.0, start.BendingMoment, precision: 6);
        Assert.Equal(2.25, middle.ShearForce, precision: 6);
        Assert.Equal(20.25, middle.BendingMoment, precision: 6);
        Assert.Equal(-18.0, end.ShearForce, precision: 6);
        Assert.Equal(0.0, end.BendingMoment, precision: 6);
    }

    [Fact]
    public void SampleMember_WithInvalidSampleCount_ShouldThrowClearException()
    {
        StructuralModel model = CreateSingleMemberBeamModel(5.0)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        Frame2DInternalForceSampler sampler = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => sampler.SampleMember(model, result, "M1", sampleCount: 1));
    }

    private static StructuralModel CreateSingleMemberBeamModel(double length) =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", length, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("LC1", "First load case"));
}
