using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.Tests.Dynamics;

public sealed class ModalPublicApiM57Tests
{
    private static StructuralModel Cantilever(int divisions = 8)
    {
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000, 77))
            .AddSection(new StructuralSection("SEC", "I", 0.01, 0.0001));
        for (int i = 0; i <= divisions; i++)
            model.AddNode(new StructuralNode($"N{i}", 4.0 * i / divisions, 0));
        for (int i = 0; i < divisions; i++)
            model.AddMember(new StructuralMember($"E{i}", $"N{i}", $"N{i + 1}", "MAT", "SEC"));
        model.AddSupport(StructuralSupport.Fixed("S", "N0"));
        return model;
    }

    [Fact]
    public void PublicService_RecoversCantileverFrequencyAndMassNormalizedShape()
    {
        var model = Cantilever();
        var output = new StructuralSolver2DService().AnalyzeModes(
            new StructuralModalAnalysisRequest(model, new StructuralDynamicsDefinition(), 3));
        Assert.Equal(3, output.Modes.Count);
        const double beta1 = 1.875104068711961;
        double mu = 77 * 0.01 / 9.80665;
        double expected = beta1 * beta1 / (2 * Math.PI * 4 * 4) * Math.Sqrt(210_000_000 * .0001 / mu);
        Assert.InRange(Math.Abs(output.Modes[0].FrequencyHertz / expected - 1), 0, 0.005);
        Assert.All(output.Modes, mode => {
            Assert.Equal(model.Nodes.Count, mode.Shape.Count);
            Assert.InRange(mode.RelativeResidual, 0, 1e-6);
            Assert.Equal(1.0 / mode.FrequencyHertz, mode.PeriodSeconds, 9);
            Assert.All(mode.Shape, node => { Assert.True(double.IsFinite(node.Ux)); Assert.True(double.IsFinite(node.Uy)); Assert.True(double.IsFinite(node.Rz)); });
        });
        Assert.Contains(output.Warnings, w => w.Contains("not real displacements", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(model.LoadCases);
    }

    [Fact]
    public void RequestAboveAdmissibleModeCount_IsCappedAndReported()
    {
        var output = new StructuralModalAnalysisService().Analyze(new StructuralModalAnalysisRequest(
            Cantilever(1), new StructuralDynamicsDefinition(), 50));
        Assert.Equal(output.AvailableModeCount, output.Modes.Count);
        Assert.Contains(output.Warnings, s => s.Contains("Requested 50", StringComparison.Ordinal));
    }

    [Fact]
    public void InvalidMass_IsRejectedWithPublicValidationIssues()
    {
        var model = Cantilever(2);
        model.Materials[0] = model.Materials[0] with { UnitWeight = null };
        var error = Assert.Throws<StructuralSolver2D.Analysis.StructuralAnalysisException>(() =>
            new StructuralModalAnalysisService().Analyze(new StructuralModalAnalysisRequest(model,
                new StructuralDynamicsDefinition())));
        Assert.Contains(error.ValidationIssues, issue => issue.Code == "DYNAMIC_MISSING_UNIT_WEIGHT");
    }

    [Fact]
    public void ReleasedBeam_IsRejectedWithModalDiagnostic()
    {
        var model = Cantilever(2);
        model.Members[0] = model.Members[0] with { ReleaseEndMoment = true };
        var ex = Assert.Throws<StructuralSolver2D.Analysis.StructuralAnalysisException>(() =>
            new StructuralModalAnalysisService().Analyze(new StructuralModalAnalysisRequest(model,
                new StructuralDynamicsDefinition())));
        Assert.Contains("M53_RELEASE_UNSUPPORTED", ex.Message);
    }

    [Fact]
    public void UnsupportedFreeFreeModel_HasExplicitDiagnostic()
    {
        var model = Cantilever(2);
        model.Supports.Clear();
        var ex = Assert.Throws<StructuralSolver2D.Analysis.StructuralAnalysisException>(() =>
            new StructuralModalAnalysisService().Analyze(new StructuralModalAnalysisRequest(model,
                new StructuralDynamicsDefinition())));
        Assert.Contains("M54_FREE_FREE_DEFERRED", ex.Message);
    }
}
