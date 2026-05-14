using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

/// <summary>
/// Validates finite-element displacement interpolation along 2D frame members.
/// </summary>
public sealed class Frame2DDisplacementSamplerTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;

    [Fact]
    public void SampleMember_ShouldMatchNodalDisplacementsAtMemberEnds()
    {
        StructuralModel model = CreateSingleMemberBeamModel(5.0)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalY, -10.0));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagram = new Frame2DDisplacementSampler().SampleMember(model, result, "M1", sampleCount: 5);

        var start = diagram.GetClosestSample(0.0);
        var end = diagram.GetClosestSample(1.0);
        var displacementA = result.GetDisplacement("A");
        var displacementB = result.GetDisplacement("B");

        Assert.Equal(displacementA.Ux, start.GlobalUx, precision: 12);
        Assert.Equal(displacementA.Uy, start.GlobalUy, precision: 12);
        Assert.Equal(displacementA.Rz, start.LocalRz, precision: 12);

        Assert.Equal(displacementB.Ux, end.GlobalUx, precision: 12);
        Assert.Equal(displacementB.Uy, end.GlobalUy, precision: 12);
        Assert.Equal(displacementB.Rz, end.LocalRz, precision: 12);
    }

    [Fact]
    public void SampleMember_CantileverWithTipLoad_ShouldReturnExpectedMidspanInterpolation()
    {
        const double length = 5.0;
        const double force = 10.0;

        StructuralModel model = CreateSingleMemberBeamModel(length)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalY, -force));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagram = new Frame2DDisplacementSampler().SampleMember(model, result, "M1", sampleCount: 3);

        var middle = diagram.GetClosestSample(0.5);

        double expectedMidspanDeflection = -force * Math.Pow(length / 2.0, 2) * ((3.0 * length) - (length / 2.0)) / (6.0 * ElasticModulus * Inertia);
        double expectedMidspanRotation = -force * (length / 2.0) * (2.0 * length - (length / 2.0)) / (2.0 * ElasticModulus * Inertia);

        Assert.Equal(expectedMidspanDeflection, middle.GlobalUy, precision: 12);
        Assert.Equal(expectedMidspanRotation, middle.LocalRz, precision: 12);
    }

    [Fact]
    public void SampleMember_SingleElementUniformLoad_ShouldDocumentFiniteElementInterpolationAtMidspan()
    {
        const double length = 5.0;
        const double load = 10.0;

        StructuralModel model = CreateSingleMemberBeamModel(length)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -load));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagram = new Frame2DDisplacementSampler().SampleMember(model, result, "M1", sampleCount: 3);

        var middle = diagram.GetClosestSample(0.5);

        double exactClosedFormMidspanDeflection = -5.0 * load * Math.Pow(length, 4) / (384.0 * ElasticModulus * Inertia);
        double finiteElementInterpolatedMidspanDeflection = -load * Math.Pow(length, 4) / (96.0 * ElasticModulus * Inertia);

        Assert.Equal(finiteElementInterpolatedMidspanDeflection, middle.GlobalUy, precision: 12);
        Assert.NotEqual(Math.Round(exactClosedFormMidspanDeflection, 12), Math.Round(middle.GlobalUy, 12));
    }

    [Fact]
    public void SampleMember_MidspanNode_ShouldExposeExactClosedFormDeflectionAsNodalResult()
    {
        const double length = 5.0;
        const double load = 10.0;

        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("C", length / 2.0, 0.0))
            .AddNode(new StructuralNode("B", length, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember("M1", "A", "C", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("M2", "C", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("LC1", "First load case"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -load))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q2", "LC1", "M2", StructuralLoadDirection.GlobalY, -load));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var displacementC = result.GetDisplacement("C");

        double exactClosedFormMidspanDeflection = -5.0 * load * Math.Pow(length, 4) / (384.0 * ElasticModulus * Inertia);

        Assert.Equal(exactClosedFormMidspanDeflection, displacementC.Uy, precision: 12);
    }

    [Fact]
    public void SampleAllMembers_ShouldReturnOneDiagramPerFrameMember()
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
        var diagrams = new Frame2DDisplacementSampler().SampleAllMembers(model, result, sampleCount: 7);

        Assert.Equal(2, diagrams.Count);
        Assert.Contains(diagrams, diagram => diagram.MemberId == "M1" && diagram.Samples.Count == 7);
        Assert.Contains(diagrams, diagram => diagram.MemberId == "M2" && diagram.Samples.Count == 7);
    }

    [Fact]
    public void SampleMember_WithInvalidSampleCount_ShouldThrowClearException()
    {
        StructuralModel model = CreateSingleMemberBeamModel(5.0)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        Frame2DDisplacementSampler sampler = new();

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
