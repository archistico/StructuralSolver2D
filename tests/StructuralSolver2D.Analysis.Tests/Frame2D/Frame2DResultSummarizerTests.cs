using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

/// <summary>
/// Validates compact result summaries and internal-force extrema.
/// </summary>
public sealed class Frame2DResultSummarizerTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;

    [Fact]
    public void MemberInternalForceExtrema_FromDiagram_ShouldFindMinMaxAndMaxAbsoluteValues()
    {
        var diagram = new MemberInternalForceDiagram(
            "M1",
            4.0,
            new[]
            {
                new MemberInternalForceSample("M1", 0.00, 0.0, 5.0, 10.0, 0.0),
                new MemberInternalForceSample("M1", 0.25, 1.0, -8.0, 3.0, -12.0),
                new MemberInternalForceSample("M1", 0.50, 2.0, 2.0, -15.0, 9.0),
                new MemberInternalForceSample("M1", 1.00, 4.0, 6.0, -4.0, 30.0),
            });

        MemberInternalForceExtrema extrema = MemberInternalForceExtrema.FromDiagram(diagram);

        Assert.Equal(-8.0, extrema.MinNormalForce.Value, precision: 12);
        Assert.Equal(6.0, extrema.MaxNormalForce.Value, precision: 12);
        Assert.Equal(-8.0, extrema.MaxAbsNormalForce.Value, precision: 12);

        Assert.Equal(-15.0, extrema.MinShearForce.Value, precision: 12);
        Assert.Equal(10.0, extrema.MaxShearForce.Value, precision: 12);
        Assert.Equal(-15.0, extrema.MaxAbsShearForce.Value, precision: 12);

        Assert.Equal(-12.0, extrema.MinBendingMoment.Value, precision: 12);
        Assert.Equal(30.0, extrema.MaxBendingMoment.Value, precision: 12);
        Assert.Equal(30.0, extrema.MaxAbsBendingMoment.Value, precision: 12);
        Assert.Equal(1.0, extrema.MaxAbsBendingMoment.Position, precision: 12);
    }

    [Fact]
    public void MemberInternalForceExtrema_FromEmptyDiagram_ShouldThrowClearException()
    {
        var diagram = new MemberInternalForceDiagram("M1", 4.0, Array.Empty<MemberInternalForceSample>());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => MemberInternalForceExtrema.FromDiagram(diagram));

        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Summarize_SimplySupportedBeamWithUniformLoad_ShouldReturnExpectedGlobalExtrema()
    {
        const double length = 5.0;
        const double load = 10.0;

        StructuralModel model = CreateSingleMemberBeamModel(length)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -load));

        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");
        IReadOnlyList<MemberInternalForceDiagram> diagrams = new Frame2DInternalForceSampler().SampleAllMembers(model, result, sampleCount: 11);

        StructuralAnalysisSummary summary = new Frame2DResultSummarizer().Summarize(result, diagrams);

        Assert.Equal("LC1", summary.LoadCaseId);
        Assert.Equal(25.0, summary.MaxAbsReactionFy.Value, precision: 6);
        Assert.Equal(25.0, Math.Abs(summary.MaxAbsShearForce.Value), precision: 6);
        Assert.Equal(31.25, summary.MaxAbsBendingMoment.Value, precision: 6);
        Assert.Equal("M1", summary.MaxAbsBendingMoment.MemberId);
        Assert.Equal(0.5, summary.MaxAbsBendingMoment.Position, precision: 6);
    }

    [Fact]
    public void Summarize_AxiallyLoadedMember_ShouldReturnExpectedNormalForceExtreme()
    {
        const double length = 5.0;
        const double force = 100.0;

        StructuralModel model = CreateSingleMemberBeamModel(length)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(new StructuralSupport("SB", "B", false, true, true, SupportType.Custom, "Transverse and rotational restraint"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalX, force));

        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");
        IReadOnlyList<MemberInternalForceDiagram> diagrams = new Frame2DInternalForceSampler().SampleAllMembers(model, result, sampleCount: 5);

        StructuralAnalysisSummary summary = new Frame2DResultSummarizer().Summarize(result, diagrams);

        Assert.Equal(force, summary.MaxAbsNormalForce.Value, precision: 6);
        Assert.Equal(0.0, summary.MaxAbsShearForce.Value, precision: 6);
        Assert.Equal(0.0, summary.MaxAbsBendingMoment.Value, precision: 6);
    }

    [Fact]
    public void Summarize_WithNoDiagrams_ShouldReturnZeroInternalForceExtrema()
    {
        var result = new StructuralAnalysisResult(
            "LC1",
            new[] { new NodalDisplacementResult("A", 0.001, -0.002, 0.003) },
            new[] { new SupportReactionResult("SA", "A", 4.0, -7.0, 2.0) },
            Array.Empty<MemberEndForceResult>());

        StructuralAnalysisSummary summary = new Frame2DResultSummarizer().Summarize(
            result,
            Array.Empty<MemberInternalForceDiagram>());

        Assert.Equal(0.001, summary.MaxAbsUx.Value, precision: 12);
        Assert.Equal(-0.002, summary.MaxAbsUy.Value, precision: 12);
        Assert.Equal(0.003, summary.MaxAbsRz.Value, precision: 12);
        Assert.Equal(4.0, summary.MaxAbsReactionFx.Value, precision: 12);
        Assert.Equal(-7.0, summary.MaxAbsReactionFy.Value, precision: 12);
        Assert.Equal(2.0, summary.MaxAbsReactionMz.Value, precision: 12);
        Assert.Equal(0.0, summary.MaxAbsNormalForce.Value, precision: 12);
        Assert.Equal(0.0, summary.MaxAbsShearForce.Value, precision: 12);
        Assert.Equal(0.0, summary.MaxAbsBendingMoment.Value, precision: 12);
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
