using StructuralSolver2D.Analysis.Dynamics.Seismic;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Dynamics.M70;

public sealed class SeismicCqcM70Tests
{
    private static readonly SeismicSpectrumPoint[] FlatSpectrum =
        { new(0, 2.5), new(10, 2.5) };

    private static StructuralModel TwoSpanAxial()
    {
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000, 77))
            .AddSection(new StructuralSection("SEC", "Bar", .01, .0001))
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2, 0))
            .AddNode(new StructuralNode("C", 4, 0))
            .AddMember(new StructuralMember("AB", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddMember(new StructuralMember("BC", "B", "C", "MAT", "SEC", MemberType.Truss2D));
        // A collinear Truss2D chain has no transverse stiffness at the middle node.
        // Restrain B.Uy to remove that REAL mechanism while retaining B.Ux and C.Ux
        // as the two physical axial degrees of freedom tested by the CQC benchmark.
        model.AddSupport(StructuralSupport.Fixed("SA", "A"));
        model.AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
        model.AddSupport(StructuralSupport.SimpleSupport("SC", "C"));
        return model;
    }

    [Fact]
    public void TwoSpanWithoutIntermediateTransverseRestraint_IsCorrectlyRejectedAsMechanism()
    {
        // Regression guard: never weaken M54's structural stability diagnostics
        // to make a CQC test pass. B.Uy is unrestrained in this deliberately
        // unstable variant of the two-span axial benchmark.
        var model = TwoSpanAxial();
        model.Supports.Remove(model.Supports.Single(s => s.Id == "SB"));
        var request = new StructuralSeismicSpectrumRequest(model, new(),
            SeismicGroundDirection.GlobalX, FlatSpectrum,
            Combination: SeismicModalCombination.Cqc, ModalDampingRatio: .05);
        var error = Assert.Throws<StructuralAnalysisException>(() =>
            new StructuralSeismicAnalysisService().AnalyzeSpectrum(request));
        Assert.Contains("M54_INTERNAL_MECHANISM", error.Message);
    }

    [Theory]
    [InlineData(3.0, 3.0, 0.05, 1.0)]
    [InlineData(2.0, 2.0, 0.10, 1.0)]
    public void EqualFrequencies_HaveUnitCorrelation(double fi, double fj, double zeta, double expected)
    {
        Assert.InRange(Math.Abs(EqualDampingCqc.Correlation(fi, fj, zeta) - expected), 0, 1e-12);
        Assert.InRange(Math.Abs(EqualDampingCqc.Combine(new[] { 2.0, 3.0 }, new[] { fi, fj }, zeta) - 5), 0, 1e-12);
        Assert.Equal(1d, EqualDampingCqc.Correlation(fj, fi, zeta));
    }

    [Fact]
    public void TwoCorrelatedModes_MatchClosedFormIncludingSignedCancellation()
    {
        // Analytic equal-zeta CQC coefficient, independently evaluated in this test.
        const double r = 0.97;
        const double zeta = 0.05;
        double rho = 8 * zeta * zeta * (1 + r) * Math.Pow(r, 1.5) /
            (Math.Pow(1 - r * r, 2) + 4 * zeta * zeta * r * Math.Pow(1 + r, 2));
        double same = Math.Sqrt(4 + 9 + 12 * rho);
        double opposing = Math.Sqrt(4 + 9 - 12 * rho);
        double a = EqualDampingCqc.Combine(new[] { 2.0, 3.0 }, new[] { 10.0, 10 / r }, zeta);
        double b = EqualDampingCqc.Combine(new[] { 2.0, -3.0 }, new[] { 10 / r, 10.0 }, zeta);
        Assert.InRange(Math.Abs(a - same), 0, 1e-12);
        Assert.InRange(Math.Abs(b - opposing), 0, 1e-12);
        Assert.True(opposing < Math.Sqrt(13));
        Assert.True(same > Math.Sqrt(13));
    }

    [Fact]
    public void SeparatedModes_CorrelationApproachesZero_AndOrderDoesNotMatter()
    {
        double rho = EqualDampingCqc.Correlation(1, 1000, .05);
        Assert.InRange(rho, 0, 1e-6);
        double a = EqualDampingCqc.Combine(new[] { 2.0, -3.0 }, new[] { 1.0, 1000.0 }, .05);
        double b = EqualDampingCqc.Combine(new[] { -3.0, 2.0 }, new[] { 1000.0, 1.0 }, .05);
        Assert.InRange(Math.Abs(a - b), 0, 1e-12);
        Assert.InRange(Math.Abs(a - Math.Sqrt(13)), 0, 2e-6);
    }

    [Fact]
    public void SingleMode_CqcEqualsSrss_ForPhysicalModel()
    {
        var model = TwoSpanAxial();
        var request = new StructuralSeismicSpectrumRequest(model, new(), SeismicGroundDirection.GlobalX,
            FlatSpectrum, ModeCount: 1);
        var service = new StructuralSeismicAnalysisService();
        var srss = service.AnalyzeSpectrum(request);
        var cqc = service.AnalyzeSpectrum(request with {
            Combination = SeismicModalCombination.Cqc, ModalDampingRatio = .05 });
        Assert.Equal(SeismicModalCombination.Cqc, cqc.Combination);
        Assert.Equal(.05, cqc.ModalDampingRatio!.Value);
        Assert.Single(cqc.Modes);
        Assert.Equal(srss.RetainedMassPercentage, cqc.RetainedMassPercentage, 12);
        for (int i = 0; i < srss.NodeEnvelopes.Count; i++)
        {
            Assert.InRange(Math.Abs(srss.NodeEnvelopes[i].AbsUxMeters - cqc.NodeEnvelopes[i].AbsUxMeters), 0, 1e-12);
            Assert.InRange(Math.Abs(srss.NodeEnvelopes[i].AbsUyMeters - cqc.NodeEnvelopes[i].AbsUyMeters), 0, 1e-12);
        }
    }

    [Fact]
    public void TwoSpanModel_CqcAgreesWithIndependentMassWeightedModeContributions()
    {
        var model = TwoSpanAxial();
        var service = new StructuralSeismicAnalysisService();
        var request = new StructuralSeismicSpectrumRequest(model, new(), SeismicGroundDirection.GlobalX,
            FlatSpectrum, Combination: SeismicModalCombination.Cqc, ModalDampingRatio: .05);
        var cqc = service.AnalyzeSpectrum(request);
        Assert.Equal(2, cqc.Modes.Count);
        Assert.Contains(cqc.Warnings, w => w.Contains("M70_CQC"));
        // The corrected supports reproduce the validated M60 two-axial-mode
        // SRSS reference without changing the underlying CQC production code.
        var srss = service.AnalyzeSpectrum(request with {
            Combination = SeismicModalCombination.Srss, ModalDampingRatio = null });
        Assert.InRange(Math.Abs(srss.NodeEnvelopes[2].AbsUxMeters - 7.708062366148531e-7), 0, 1e-12);
        // For two axial DOFs, obtain signed modal displacement contributions from
        // mass-normalized eigenvectors and independent r^T M phi participation.
        var prepared = SeismicParticipation.Calculate(model, new(), SeismicGroundDirection.GlobalX);
        var contributions = new double[2];
        for (int i = 0; i < 2; i++)
        {
            var eigen = prepared.Spectrum.Modes[i];
            var participation = prepared.Modes[i].ParticipationFactor;
            double omega = 2 * Math.PI * eigen.FrequencyHertz;
            contributions[i] = eigen.GlobalMassNormalizedShape[6] * participation * 2.5 / (omega * omega);
        }
        double ratio = Math.Min(cqc.Modes[0].FrequencyHertz, cqc.Modes[1].FrequencyHertz) /
            Math.Max(cqc.Modes[0].FrequencyHertz, cqc.Modes[1].FrequencyHertz);
        double rho = 8 * .05 * .05 * (1 + ratio) * Math.Pow(ratio, 1.5) /
            (Math.Pow(1 - ratio * ratio, 2) + 4 * .05 * .05 * ratio * Math.Pow(1 + ratio, 2));
        double expected = Math.Sqrt(contributions[0]*contributions[0] +
            contributions[1]*contributions[1] + 2*rho*contributions[0]*contributions[1]);
        Assert.InRange(Math.Abs(cqc.NodeEnvelopes[2].AbsUxMeters - expected), 0, 1e-12);
        Assert.All(cqc.NodeEnvelopes, n => Assert.True(n.AbsUxMeters >= 0));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-.01)]
    [InlineData(.30001)]
    [InlineData(.00000001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Cqc_InvalidDampingFailsFast(double damping)
    {
        var request = new StructuralSeismicSpectrumRequest(TwoSpanAxial(), new(),
            SeismicGroundDirection.GlobalX, FlatSpectrum, Combination: SeismicModalCombination.Cqc,
            ModalDampingRatio: damping);
        Assert.Throws<ArgumentException>(() => new StructuralSeismicAnalysisService().AnalyzeSpectrum(request));
    }

    [Fact]
    public void Cqc_RequiresDamping_AndSrssDoesNotAcceptUnusedDamping()
    {
        var request = new StructuralSeismicSpectrumRequest(TwoSpanAxial(), new(),
            SeismicGroundDirection.GlobalX, FlatSpectrum);
        var solver = new StructuralSeismicAnalysisService();
        Assert.Throws<ArgumentException>(() => solver.AnalyzeSpectrum(request with {Combination = SeismicModalCombination.Cqc}));
        Assert.Throws<ArgumentException>(() => solver.AnalyzeSpectrum(request with {ModalDampingRatio = .05}));
        Assert.Throws<ArgumentException>(() => solver.AnalyzeSpectrum(request with {
            Combination = (SeismicModalCombination)101 }));
    }

    [Fact]
    public void Correlation_RejectsInvalidFrequency_AndDoesNotInventAResult()
    {
        Assert.Throws<ArgumentException>(() => EqualDampingCqc.Correlation(0, 10, .05));
        Assert.Throws<ArgumentException>(() => EqualDampingCqc.Correlation(double.PositiveInfinity, 10, .05));
        Assert.Throws<ArgumentException>(() => EqualDampingCqc.Combine(new[] { 1.0 }, new[] { 1.0, 2.0 }, .05));
        Assert.Throws<ArgumentException>(() => EqualDampingCqc.Combine(new[] { double.NaN }, new[] { 1.0 }, .05));
        Assert.Equal(0, EqualDampingCqc.Combine(new[] { 0.0 }, new[] { 1.0 }, .05));
    }
}
