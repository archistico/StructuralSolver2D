using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Serviceability;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Serviceability;

/// <summary>
/// Validates the preliminary serviceability deflection checker.
/// </summary>
public sealed class PreliminaryDeflectionCheckerTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;

    [Fact]
    public void Check_CantileverTipLoad_ShouldPassWhenDeflectionIsBelowSelectedLimit()
    {
        StructuralModel model = CreateCantileverWithTipLoad(length: 5.0, force: 10.0);
        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagram = new Frame2DDisplacementSampler().SampleMember(model, result, "M1", sampleCount: 21);

        var check = new PreliminaryDeflectionChecker().Check(
            diagram,
            new DeflectionLimit(denominator: 40.0, DeflectionCheckDirection.GlobalY));

        Assert.Equal("M1", check.MemberId);
        Assert.Equal(DeflectionCheckStatus.Pass, check.Status);
        Assert.True(check.IsPass);
        Assert.Equal(5.0 / 40.0, check.AllowedDeflection, precision: 12);
        Assert.Equal(1.0, check.NormalizedPosition, precision: 12);
        Assert.Equal(5.0, check.Distance, precision: 12);
        Assert.Equal(Math.Abs(result.GetDisplacement("B").Uy), check.MaxAbsDeflection, precision: 12);
    }

    [Fact]
    public void Check_CantileverTipLoad_ShouldFailWhenDeflectionExceedsSelectedLimit()
    {
        StructuralModel model = CreateCantileverWithTipLoad(length: 5.0, force: 10.0);
        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagram = new Frame2DDisplacementSampler().SampleMember(model, result, "M1", sampleCount: 21);

        var check = new PreliminaryDeflectionChecker().Check(
            diagram,
            new DeflectionLimit(denominator: 100.0, DeflectionCheckDirection.GlobalY));

        Assert.Equal(DeflectionCheckStatus.Fail, check.Status);
        Assert.False(check.IsPass);
        Assert.True(check.UtilizationRatio > 1.0);
    }

    [Fact]
    public void Check_AllDiagrams_ShouldReturnOneResultPerDiagram()
    {
        StructuralModel model = CreateTwoMemberBeamWithMidspanLoad();
        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var diagrams = new Frame2DDisplacementSampler().SampleAllMembers(model, result, sampleCount: 11);

        var checks = new PreliminaryDeflectionChecker().Check(
            diagrams,
            new DeflectionLimit(denominator: 250.0, DeflectionCheckDirection.GlobalY));

        Assert.Equal(2, checks.Count);
        Assert.Contains(checks, check => check.MemberId == "M1");
        Assert.Contains(checks, check => check.MemberId == "M2");
    }

    [Fact]
    public void DeflectionLimit_WithInvalidDenominator_ShouldThrowClearException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DeflectionLimit(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DeflectionLimit(double.NaN));
    }

    private static StructuralModel CreateCantileverWithTipLoad(double length, double force) =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", length, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("LC1", "First load case"))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalY, -force));

    private static StructuralModel CreateTwoMemberBeamWithMidspanLoad() =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 2.5, 0.0))
            .AddNode(new StructuralNode("C", 5.0, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("M2", "B", "C", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("LC1", "First load case"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SC", "C"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalY, -10.0));
}
