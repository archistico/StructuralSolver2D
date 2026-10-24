using StructuralSolver2D.Analysis.Dynamics.Harmonic;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.Tests.Dynamics.M58;

public sealed class HarmonicResponseM58Tests
{
    [Theory]
    [InlineData(0, 0.05)]
    [InlineData(0.5, 0.05)]
    [InlineData(1.0, 0.05)]
    [InlineData(2.0, 0.05)]
    public void Sdof_MagnificationAndPhase_AgreeWithClosedForm(double frequencyRatio, double dampingRatio)
    {
        const double m = 2, k = 200, force = 7;
        double omega0 = Math.Sqrt(k / m), omega = frequencyRatio * omega0;
        var u = HarmonicFrequencyResponseSolver.SolveSdof(m, 2 * dampingRatio * m * omega0, k, omega, force);
        double expectedAmplification = 1 / Math.Sqrt(Math.Pow(1 - frequencyRatio * frequencyRatio, 2) +
                                                       Math.Pow(2 * dampingRatio * frequencyRatio, 2));
        Assert.InRange(Math.Abs(u.Magnitude / (force / k) - expectedAmplification), 0, 1e-12);
        double phase = Math.Atan2(-2 * dampingRatio * frequencyRatio, 1 - frequencyRatio * frequencyRatio);
        Assert.InRange(Math.Abs(u.Phase - phase), 0, 1e-12);
    }

    [Fact]
    public void Sdof_ResonanceWithoutDamping_IsExplicitlyRejected()
    {
        var err = Assert.Throws<InvalidOperationException>(() => HarmonicFrequencyResponseSolver.SolveSdof(2, 0, 200, 10, 1));
        Assert.Contains("M58_UNDAMPED_RESONANCE", err.Message);
    }

    [Fact]
    public void Damping_EnforcesMutualExclusivityAndPhysicalUnits()
    {
        Assert.Throws<ArgumentException>(() => new StructuralHarmonicDamping().Ratio(10));
        Assert.Throws<ArgumentException>(() => new StructuralHarmonicDamping { UniformModalRatio = -0.1 }.Ratio(10));
        Assert.Throws<ArgumentException>(() => new StructuralHarmonicDamping { UniformModalRatio = 0.05, RayleighAlphaInverseSeconds = 1, RayleighBetaSeconds = 0 }.Ratio(10));
        Assert.Throws<ArgumentException>(() => new StructuralHarmonicDamping { RayleighAlphaInverseSeconds = 1 }.Ratio(10));
        Assert.Equal(0.1, new StructuralHarmonicDamping { RayleighAlphaInverseSeconds = 1, RayleighBetaSeconds = 0.01 }.Ratio(10), 12);
    }

    private static StructuralModel Cantilever()
    {
        var model = new StructuralModel().AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000, 77))
            .AddSection(new StructuralSection("SEC", "Section", 0.01, 0.0001));
        for (int i = 0; i <= 4; i++) model.AddNode(new StructuralNode($"N{i}", i, 0));
        for (int i = 0; i < 4; i++) model.AddMember(new StructuralMember($"E{i}", $"N{i}", $"N{i + 1}", "MAT", "SEC"));
        model.AddSupport(StructuralSupport.Fixed("S", "N0"));
        return model;
    }

    [Fact]
    public void CompleteModalSweep_StaticLimitIsConsistentAcrossDampingModels()
    {
        var model = Cantilever();
        var forces = new[] { new StructuralHarmonicNodalForce("N4", Fy: -1) };
        var service = new StructuralHarmonicAnalysisService();
        var first = service.Analyze(new(model, new StructuralDynamicsDefinition(), forces, new[] { 0d, 1d },
            new StructuralHarmonicDamping { UniformModalRatio = 0.03 }));
        var second = service.Analyze(new(model, new StructuralDynamicsDefinition(), forces, new[] { 0d, 1d },
            new StructuralHarmonicDamping { RayleighAlphaInverseSeconds = 0.05, RayleighBetaSeconds = 0.0001 }));
        var staticTip = first.Samples[0].Nodes[^1].Uy;
        const double expectedTip = -1 * 4.0 * 4 * 4 / (3 * 210_000_000 * 0.0001);
        Assert.InRange(Math.Abs(staticTip.Real / expectedTip - 1), 0, 1e-6);
        Assert.InRange(Math.Abs(staticTip.Imaginary), 0, 1e-12);
        Assert.InRange(Math.Abs(second.Samples[0].Nodes[^1].Uy.Real / expectedTip - 1), 0, 1e-6);
        Assert.True(first.Samples[1].Nodes[^1].Uy.Amplitude > 0);
        Assert.True(first.NaturalFrequenciesHertz.Count > 0);
        Assert.Equal(first.NaturalFrequenciesHertz.Count, first.ModalDampingRatios.Count);
    }

    [Fact]
    public void FirstMode_HasFiniteResonantPeakAndLagWithPositiveDamping()
    {
        var model = Cantilever();
        var dynamics = new StructuralDynamicsDefinition();
        var modal = new StructuralModalAnalysisService().Analyze(new StructuralModalAnalysisRequest(model, dynamics, 1));
        double f1 = modal.Modes[0].FrequencyHertz;
        var sweep = new StructuralHarmonicAnalysisService().Analyze(new(model, dynamics,
            new[] { new StructuralHarmonicNodalForce("N4", Fy: 1) },
            new[] { 0d, f1 }, new StructuralHarmonicDamping { UniformModalRatio = 0.05 }));
        var atResonance = sweep.Samples[1].Nodes[^1].Uy;
        Assert.True(atResonance.Amplitude > 5 * sweep.Samples[0].Nodes[^1].Uy.Amplitude);
        Assert.InRange(atResonance.PhaseDegrees, -120, -60);
        Assert.True(double.IsFinite(atResonance.Real));
        Assert.True(double.IsFinite(atResonance.Imaginary));
    }

    [Fact]
    public void ForceAppliedOnlyAtRestrainedNode_ProducesZeroFreeResponse()
    {
        var model = Cantilever();
        var output = new StructuralHarmonicAnalysisService().Analyze(new(model, new StructuralDynamicsDefinition(),
            new[] { new StructuralHarmonicNodalForce("N0", Fy: 3) }, new[] { 0d, 1d },
            new StructuralHarmonicDamping { UniformModalRatio = 0.02 }));
        Assert.All(output.Samples, sample => Assert.All(sample.Nodes, node =>
            Assert.Equal(0d, node.Uy.Amplitude)));
    }

    [Fact]
    public void InvalidInputAndUnsupportedModels_FailExplicitly()
    {
        var model = Cantilever();
        var dynamics = new StructuralDynamicsDefinition();
        var service = new StructuralHarmonicAnalysisService();
        var damp = new StructuralHarmonicDamping { UniformModalRatio = 0.05 };
        Assert.Throws<ArgumentException>(() => service.Analyze(new(model, dynamics,
            new[] { new StructuralHarmonicNodalForce("missing", Fy: 1) }, new[] { 1d }, damp)));
        Assert.Throws<ArgumentException>(() => service.Analyze(new(model, dynamics,
            new[] { new StructuralHarmonicNodalForce("N4", Fy: 1) }, new[] { double.NaN }, damp)));
        Assert.Throws<ArgumentException>(() => service.Analyze(new(model, dynamics,
            new[] { new StructuralHarmonicNodalForce("N4", Fy: 1) }, new[] { 2d, 1d }, damp)));
        model.Members[0] = model.Members[0] with { ReleaseEndMoment = true };
        var error = Assert.Throws<StructuralSolver2D.Analysis.StructuralAnalysisException>(() => service.Analyze(new(model, dynamics,
            new[] { new StructuralHarmonicNodalForce("N4", Fy: 1) }, new[] { 1d }, damp)));
        Assert.Contains("M53_RELEASE_UNSUPPORTED", error.Message);
    }
}
