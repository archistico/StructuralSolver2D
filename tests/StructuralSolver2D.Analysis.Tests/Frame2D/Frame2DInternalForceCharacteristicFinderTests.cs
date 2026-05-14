using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

/// <summary>
/// Validates characteristic point detection on sampled N/V/M diagrams.
/// </summary>
public sealed class Frame2DInternalForceCharacteristicFinderTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;

    [Fact]
    public void Find_SimplySupportedBeamWithUniformLoad_ShouldDetectZeroShearAndMomentMaximumCandidateAtMidspan()
    {
        StructuralModel model = CreateSingleMemberBeamModel(5.0)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10.0));

        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");
        MemberInternalForceDiagram diagram = new Frame2DInternalForceSampler().SampleMember(model, result, "M1", sampleCount: 101);

        MemberInternalForceCharacteristics characteristics = new Frame2DInternalForceCharacteristicFinder().Find(diagram);

        InternalForceCharacteristicPoint zeroShear = Assert.Single(characteristics.Points, point =>
            point.Kind == InternalForceCharacteristicPointKind.ZeroCrossing &&
            point.Quantity == InternalForceQuantity.ShearForce &&
            Math.Abs(point.Distance - 2.5) < 1e-9);

        Assert.Equal(0.5, zeroShear.Position, precision: 12);
        Assert.Equal(0.0, zeroShear.Value, precision: 12);

        InternalForceCharacteristicPoint momentCandidate = Assert.Single(characteristics.Points, point =>
            point.Kind == InternalForceCharacteristicPointKind.BendingMomentExtremumCandidate &&
            point.Quantity == InternalForceQuantity.BendingMoment &&
            Math.Abs(point.Distance - 2.5) < 1e-9);

        Assert.Equal(31.25, momentCandidate.Value, precision: 6);
    }

    [Fact]
    public void Find_CantileverWithUniformLoad_ShouldDetectMaximumAbsoluteMomentAtFixedEnd()
    {
        StructuralModel model = CreateSingleMemberBeamModel(5.0)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10.0));

        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");
        MemberInternalForceDiagram diagram = new Frame2DInternalForceSampler().SampleMember(model, result, "M1", sampleCount: 51);

        MemberInternalForceCharacteristics characteristics = new Frame2DInternalForceCharacteristicFinder().Find(diagram);

        InternalForceCharacteristicPoint maxAbsMoment = characteristics.Points.Single(point =>
            point.Kind == InternalForceCharacteristicPointKind.SampledMaximumAbsolute &&
            point.Quantity == InternalForceQuantity.BendingMoment);

        Assert.Equal(0.0, maxAbsMoment.Distance, precision: 12);
        Assert.Equal(-125.0, maxAbsMoment.Value, precision: 6);
    }

    [Fact]
    public void Find_PointLoadOnMember_ShouldDetectShearDiscontinuityCandidateNearLoadPosition()
    {
        StructuralModel model = CreateSingleMemberBeamModel(8.0)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.PointLoadOnMember("P1", "LC1", "M1", StructuralLoadDirection.GlobalY, -12.0, 0.25));

        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");
        MemberInternalForceDiagram diagram = new Frame2DInternalForceSampler().SampleMember(model, result, "M1", sampleCount: 5);

        MemberInternalForceCharacteristics characteristics = new Frame2DInternalForceCharacteristicFinder().Find(diagram);

        InternalForceCharacteristicPoint discontinuity = Assert.Single(characteristics.Points, point =>
            point.Kind == InternalForceCharacteristicPointKind.DiscontinuityCandidate &&
            point.Quantity == InternalForceQuantity.ShearForce);

        Assert.Equal(0.25, discontinuity.Position, precision: 12);
        Assert.Equal(2.0, discontinuity.Distance, precision: 12);
    }

    [Fact]
    public void FindAll_ShouldReturnCharacteristicsForEveryDiagram()
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

        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");
        IReadOnlyList<MemberInternalForceDiagram> diagrams = new Frame2DInternalForceSampler().SampleAllMembers(model, result, sampleCount: 11);

        IReadOnlyList<MemberInternalForceCharacteristics> allCharacteristics = new Frame2DInternalForceCharacteristicFinder().FindAll(diagrams);

        Assert.Equal(2, allCharacteristics.Count);
        Assert.Contains(allCharacteristics, item => item.MemberId == "M1" && item.Points.Count > 0);
        Assert.Contains(allCharacteristics, item => item.MemberId == "M2" && item.Points.Count > 0);
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
