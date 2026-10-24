using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.Tests.Dynamics.M591;

public sealed class TimeConvergenceM591Tests
{
    private static StructuralTimeHistoryAnalysisRequest Base(int count=40, double dt=.0002, bool excited=true)
    {
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000, 77))
            .AddSection(new StructuralSection("SEC", "Rect", .01, .0001))
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 1, 0))
            .AddMember(new StructuralMember("AB", "A", "B", "MAT", "SEC"))
            .AddSupport(StructuralSupport.Fixed("FIX", "A"));
        return new StructuralTimeHistoryAnalysisRequest(model, new StructuralDynamicsDefinition(),
            dt, count, new StructuralHarmonicDamping { UniformModalRatio = .02 },
            Array.Empty<StructuralTimeLoadPattern>(),
            excited ? new[] { new StructuralTimeInitialNodeState("B", Vy:.01) } : null);
    }

    [Fact]
    public void M591_FreeVibration_ComparesSamePhysicalTimes_AndImprovesUy()
    {
        var request = new StructuralTimeConvergenceRequest(Base());
        var result = new StructuralTimeConvergenceAnalysisService().Analyze(request);
        Assert.Equal(40, result.CoarseStepCount);
        Assert.InRange(Math.Abs(result.EndTimeSeconds-.008), 0, 1e-12);
        Assert.True(result.HasMeaningfulResponse);
        var uy = Assert.Single(result.Components, c => c.Component == "Uy");
        Assert.True(uy.HasResolvedSignal);
        Assert.True(uy.CoarseMediumRelativeDifference > 0);
        Assert.True(uy.MediumFineRelativeDifference < uy.CoarseMediumRelativeDifference,
            $"coarse/medium={uy.CoarseMediumRelativeDifference}, medium/fine={uy.MediumFineRelativeDifference}");
        Assert.True(result.FocusPeak.FineAmplitude > 0);
        Assert.Equal("B", result.FocusPeak.NodeId);
        Assert.Equal(3, result.ModalResolution.Count);
        Assert.All(result.ModalResolution, m => Assert.Equal(m.CoarseCyclesPerStep / 4, m.FineCyclesPerStep, 9));
    }

    [Fact]
    public void M591_ZeroSignal_IsInconclusive_NotApprovedByZeroDifferences()
    {
        var result = new StructuralTimeConvergenceAnalysisService().Analyze(
            new StructuralTimeConvergenceRequest(Base(excited:false)));
        Assert.False(result.HasMeaningfulResponse);
        Assert.False(result.MeetsIllustrativeTolerances);
        Assert.False(result.DifferencesDecrease);
        Assert.Contains(result.Warnings, x => x.Contains("M591_ZERO_RESPONSE"));
        Assert.All(result.Components, c => Assert.False(c.HasResolvedSignal));
        Assert.All(result.Components, c => Assert.Equal(0.0, c.MediumFineRelativeDifference));
    }

    [Fact]
    public void M591_RejectsUnsupportedRefinementBudgetAndBadFocus()
    {
        var service = new StructuralTimeConvergenceAnalysisService();
        Assert.Throws<ArgumentException>(() => service.Analyze(new(Base(count:501))));
        Assert.Throws<ArgumentException>(() => service.Analyze(new(Base(count:0))));
        Assert.Throws<ArgumentException>(() => service.Analyze(new(Base(), "MISSING")));
        Assert.Throws<ArgumentException>(() => service.Analyze(new(Base(), "B", "bad")));
    }

    [Fact]
    public void M591_PiecewiseLoadKnotsNotOnCoarseGrid_AreReported()
    {
        var baseRequest=Base(excited:false);
        double end=baseRequest.TimeStepSeconds*baseRequest.StepCount;
        var history = new StructuralTimeLoadPattern(
            new[] {new StructuralHarmonicNodalForce("B", Fy:-1)},
            new[] {new StructuralTimeLoadKnot(0,0),new StructuralTimeLoadKnot(.00035,1),
                   new StructuralTimeLoadKnot(.00075,0),new StructuralTimeLoadKnot(end,0)});
        var result = new StructuralTimeConvergenceAnalysisService().Analyze(
            new(baseRequest with {Loads=new[]{history}}));
        Assert.Contains(result.Warnings, x => x.Contains("M591_KNOT_ALIGNMENT"));
        Assert.True(result.FocusPeak.FineAmplitude > 0);
    }

    [Fact]
    public void M591_LongWindow_FlagsAnUnachievableAllModeResolutionBudget()
    {
        var result = new StructuralTimeConvergenceAnalysisService().Analyze(
            new StructuralTimeConvergenceRequest(Base(count: 400, dt: .002)));
        Assert.False(result.FineGridResolvesAllModes);
        Assert.Contains(result.Warnings, x => x.StartsWith("M591_STEP_BUDGET:", StringComparison.Ordinal));
    }

    [Fact]
    public void M591_ConstrainedFocus_IsNotMisrepresentedAsAMeaningfulPeak()
    {
        var result = new StructuralTimeConvergenceAnalysisService().Analyze(
            new StructuralTimeConvergenceRequest(Base(), "A", "Uy"));
        Assert.True(result.HasMeaningfulResponse);
        Assert.Equal(0.0, result.FocusPeak.FineAmplitude);
        Assert.Contains(result.Warnings, x => x.StartsWith("M591_FOCUS_ZERO:", StringComparison.Ordinal));
    }

}
