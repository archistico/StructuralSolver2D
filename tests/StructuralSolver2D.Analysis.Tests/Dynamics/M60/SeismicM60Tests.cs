using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Dynamics.M60;

public sealed class SeismicM60Tests
{
    // One physical bar, two-node consistent mass, one admissible axial DOF.
    // Ground influence r=[1,1]. Analytical modal effective mass=3/4*m_total.
    // Fixed end is restrained; the free-end orthogonal translation is supported.
    private static StructuralModel AxialBar()
    {
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000, 77))
            .AddSection(new StructuralSection("SEC", "Bar", .01, .0001))
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2, 0))
            .AddMember(new StructuralMember("AB", "A", "B", "MAT", "SEC", MemberType.Truss2D));
        model.AddSupport(StructuralSupport.Fixed("SA", "A"));
        model.AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
        return model;
    }
    private static readonly StructuralDynamicsDefinition Dynamics = new();

    [Fact]
    public void Spectrum_OneAxialMode_HasAnalyticParticipationMassAndPseudoDisplacement()
    {
        const double a = 2.5;
        var solver = new StructuralSeismicAnalysisService();
        var result = solver.AnalyzeSpectrum(new(AxialBar(), Dynamics, SeismicGroundDirection.GlobalX,
            new[] { new SeismicSpectrumPoint(0, a), new SeismicSpectrumPoint(10, a) }));
        double lineMass = 77 / DynamicMassUnits.StandardGravityMetersPerSecondSquared * .01;
        double total = lineMass * 2;
        double k = 210_000_000 * .01 / 2;
        double activeMass = total / 3;
        double expectedFreq = Math.Sqrt(k / activeMass) / (2 * Math.PI);
        Assert.Single(result.Modes);
        Assert.InRange(Math.Abs(result.GroundMass - total), 0, 1e-12);
        Assert.InRange(Math.Abs(result.Modes[0].EffectiveMass - .75 * total), 0, 1e-11);
        Assert.InRange(Math.Abs(result.RetainedMassPercentage - 75), 0, 1e-7);
        Assert.InRange(Math.Abs(result.Modes[0].FrequencyHertz - expectedFreq), 0, expectedFreq * 1e-8);
        double expectedTipDisplacement = 1.5 * activeMass * a / k;
        Assert.InRange(Math.Abs(result.NodeEnvelopes[1].AbsUxMeters - expectedTipDisplacement), 0, 1e-11);
        Assert.Equal(0, result.NodeEnvelopes[0].AbsUxMeters);
    }

    [Fact]
    public void Spectrum_InterpolatesByPeriod_AndRejectsExtrapolationOrInvalidOrdinate()
    {
        var service = new StructuralSeismicAnalysisService();
        var valid = new StructuralSeismicSpectrumRequest(AxialBar(), Dynamics, SeismicGroundDirection.GlobalX,
            new[] { new SeismicSpectrumPoint(0, 1), new SeismicSpectrumPoint(10, 3) });
        var output = service.AnalyzeSpectrum(valid);
        double expected = 1 + output.Modes[0].PeriodSeconds * .2;
        Assert.InRange(Math.Abs(output.Modes[0].PseudoAccelerationMetersPerSecondSquared!.Value - expected), 0, 1e-12);
        Assert.Throws<ArgumentException>(() => service.AnalyzeSpectrum(valid with {
            Spectrum = new[] { new SeismicSpectrumPoint(1, 2), new SeismicSpectrumPoint(2, 3) }
        }));
        Assert.Throws<ArgumentException>(() => service.AnalyzeSpectrum(valid with {
            Spectrum = new[] { new SeismicSpectrumPoint(0, 1), new SeismicSpectrumPoint(10, -1) }
        }));
        Assert.Throws<ArgumentException>(() => service.AnalyzeSpectrum(valid with {
            Spectrum = new[] { new SeismicSpectrumPoint(0, 1), new SeismicSpectrumPoint(0, 2) }
        }));
    }

    [Fact]
    public void Spectrum_TwoAxialModes_SRSSMatchesIndependentReference_AndTruncatesConservatively()
    {
        var model = AxialBar();
        model.AddNode(new StructuralNode("C", 4, 0));
        model.AddMember(new StructuralMember("BC", "B", "C", "MAT", "SEC", MemberType.Truss2D));
        model.AddSupport(StructuralSupport.SimpleSupport("SC", "C"));
        var points = new[] {new SeismicSpectrumPoint(0, 2.5), new SeismicSpectrumPoint(10, 2.5)};
        var full = new StructuralSeismicAnalysisService().AnalyzeSpectrum(
            new(model, Dynamics, SeismicGroundDirection.GlobalX, points));
        Assert.Equal(2, full.Modes.Count);
        // Computed separately with scipy.linalg.eigh for the two free axial DOFs.
        const double reference = 7.708062366148531e-7;
        Assert.InRange(Math.Abs(full.NodeEnvelopes[2].AbsUxMeters - reference), 0, 1e-12);
        var truncated = new StructuralSeismicAnalysisService().AnalyzeSpectrum(
            new(model, Dynamics, SeismicGroundDirection.GlobalX, points, ModeCount: 1));
        Assert.Single(truncated.Modes);
        Assert.True(truncated.RetainedMassPercentage < full.RetainedMassPercentage);
        Assert.True(truncated.NodeEnvelopes[2].AbsUxMeters <= full.NodeEnvelopes[2].AbsUxMeters);
        Assert.Contains(truncated.Warnings, w => w.Contains("M60_TRUNCATED_MODES"));
    }

    [Fact]
    public void Time_EquivalentForceSignAndAbsoluteAcceleration_AgreeWithClosedFormAtT0()
    {
        // ag(0)=+a, all relative initial coordinates zero: expected rel a_tip=-1.5a.
        const double ag = 1.7;
        var request = new StructuralSeismicTimeRequest(AxialBar(), Dynamics,
            SeismicGroundDirection.GlobalX, .001, 12,
            new StructuralHarmonicDamping { UniformModalRatio = .02 },
            new[] { new SeismicAccelerationKnot(0, ag), new SeismicAccelerationKnot(.012, ag) });
        var result = new StructuralSeismicAnalysisService().AnalyzeTime(request);
        Assert.DoesNotContain(result.RelativeResponse.Warnings,
            w => w.StartsWith("M59 linear first-order elastic motion only;"));
        Assert.Equal(13, result.RelativeResponse.Steps.Count);
        Assert.Equal(13, result.GroundAndAbsoluteAccelerations.Count);
        var initial = result.RelativeResponse.Steps[0];
        Assert.InRange(Math.Abs(initial.Nodes[1].Ax + 1.5 * ag), 0, 1e-9);
        Assert.InRange(Math.Abs(result.GroundAndAbsoluteAccelerations[0].AbsoluteAccelerations[1].AxMetersPerSecondSquared + .5 * ag), 0, 1e-9);
        Assert.InRange(Math.Abs(result.GroundAndAbsoluteAccelerations[0].AbsoluteAccelerations[0].AxMetersPerSecondSquared - ag), 0, 1e-12);
        Assert.True(result.RelativeResponse.Envelopes[1].MaxAbsUx > 0);
        Assert.All(result.RelativeResponse.Steps, sample =>
            Assert.InRange(Math.Abs(sample.Energy.BalanceResidual), 0, 1e-9));
    }

    [Fact]
    public void Time_ZeroGroundSignalIsZero_AndInvalidInputRejected()
    {
        var request = new StructuralSeismicTimeRequest(AxialBar(), Dynamics,
            SeismicGroundDirection.GlobalX, .002, 10,
            new StructuralHarmonicDamping { UniformModalRatio = .03 },
            new[] {new SeismicAccelerationKnot(0, 0), new SeismicAccelerationKnot(.02, 0)});
        var service = new StructuralSeismicAnalysisService();
        var result = service.AnalyzeTime(request);
        Assert.All(result.RelativeResponse.Steps, s => Assert.Equal(0, s.Nodes[1].Ux));
        Assert.Throws<ArgumentException>(() => service.AnalyzeTime(request with {
            GroundAcceleration = new[] {new SeismicAccelerationKnot(0,0),new SeismicAccelerationKnot(.01,1)}
        }));
        Assert.Throws<ArgumentException>(() => service.AnalyzeTime(request with {
            GroundAcceleration = new[] {new SeismicAccelerationKnot(0,0),new SeismicAccelerationKnot(0,1)}
        }));
        Assert.Throws<ArgumentException>(() => service.AnalyzeTime(request with {
            GroundAcceleration = new[] {new SeismicAccelerationKnot(0,double.NaN),new SeismicAccelerationKnot(.02,0)}
        }));
        Assert.Throws<ArgumentException>(() => service.AnalyzeTime(request with {StepCount=2001}));
    }
}
