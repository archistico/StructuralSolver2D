using StructuralSolver2D.Analysis.Frame2D;

namespace StructuralSolver2D.Analysis.Tests.Frame2D.M71;

/// <summary>
/// Independent analytic reference for the M71 design decision. This does NOT test
/// a production nonlinear Frame2D element; that element is deferred to M72.
/// Units: E [kN/m2], I [m4], L [m], moment [kN*m], rotation [rad].
/// </summary>
public sealed class PlasticHingeFeasibilityM71Tests
{
    [Fact]
    public void ElasticFrameEnd_WithOtherEndFixed_HasRotationalStiffnessFourEIOverL()
    {
        const double e = 210_000_000d, area = 0.012d, inertia = 0.000032d, length = 4d;
        double[,] k = Frame2DElementMatrices.BuildLocalStiffness(e, area, inertia, length);
        Close(4 * e * inertia / length, k[5, 5], 1e-11);
        Close(2 * e * inertia / length, k[2, 5], 1e-11);
        Close(k[5, 2], k[2, 5], 1e-12);
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public void RotationalSpringInSeries_HasExactAnalyticalInitialTangent(double springToBeamRatio)
    {
        const double e = 210_000_000d, inertia = 0.000032d, length = 4d;
        double beam = 4 * e * inertia / length;
        double hinge = beam * springToBeamRatio;
        double imposedRotation = 0.007d;

        // Compatibility: theta_external = theta_beam + theta_hinge.
        // Equilibrium: Kbeam*theta_beam = Khinge*theta_hinge.
        double beamRotation = hinge / (beam + hinge) * imposedRotation;
        double hingeRotation = imposedRotation - beamRotation;
        double momentFromBeam = beam * beamRotation;
        double momentFromHinge = hinge * hingeRotation;
        double effective = beam * hinge / (beam + hinge);

        Close(momentFromBeam, momentFromHinge, 1e-11);
        Close(effective * imposedRotation, momentFromHinge, 1e-11);
        Assert.True(effective < beam, "A finite hinge spring necessarily softens the pre-yield member.");
    }

    [Fact]
    public void IsotropicBilinearHingeReference_YieldsAndUnloadsWithTrialStateUnchanged()
    {
        const double k = 4_000d, yieldMoment = 20d, hardening = 40d;
        var virgin = new HingeState(0, 0);
        HingeTrial elastic = Evaluate(0.003, virgin, k, yieldMoment, hardening);
        Close(12, elastic.Moment, 1e-12);
        Close(k, elastic.Tangent, 1e-12);
        Assert.Equal(virgin, elastic.State);

        HingeTrial plastic = Evaluate(0.010, virgin, k, yieldMoment, hardening);
        double gamma = (40 - 20) / (k + hardening);
        Close(gamma, plastic.State.PlasticRotation, 1e-14);
        Close(gamma, plastic.State.AccumulatedPlasticRotation, 1e-14);
        Close(40 - k * gamma, plastic.Moment, 1e-11);
        Close(k * hardening / (k + hardening), plastic.Tangent, 1e-11);
        Assert.Equal(virgin, new HingeState(0, 0)); // trial must not mutate committed state

        HingeTrial unloaded = Evaluate(0.006, plastic.State, k, yieldMoment, hardening);
        Close(k, unloaded.Tangent, 1e-12);
        Assert.Equal(plastic.State, unloaded.State);
        Assert.Equal(0d, unloaded.Dissipation);
        Assert.True(plastic.Dissipation >= 0);
    }

    [Fact]
    public void PlasticHingeReference_AnalyticalTangentAgreesWithCentralDifference()
    {
        const double k = 4_000d, my = 20d, h = 40d, theta = 0.01d, step = 1e-7;
        var state = new HingeState(0, 0);
        HingeTrial center = Evaluate(theta, state, k, my, h);
        double plus = Evaluate(theta + step, state, k, my, h).Moment;
        double minus = Evaluate(theta - step, state, k, my, h).Moment;
        double numerical = (plus - minus) / (2 * step);
        Close(center.Tangent, numerical, 1e-6);
    }

    private readonly record struct HingeState(double PlasticRotation, double AccumulatedPlasticRotation);
    private readonly record struct HingeTrial(double Moment, double Tangent, HingeState State, double Dissipation);

    // Deliberately independent of M63/M64/M65 production return mapping.
    // These values are a future M72 reference, not a claim of implemented hinge physics.
    private static HingeTrial Evaluate(double rotation, HingeState committed,
        double elasticRotationalStiffness, double yieldMoment, double isotropicHardening)
    {
        double trialMoment = elasticRotationalStiffness * (rotation - committed.PlasticRotation);
        double f = Math.Abs(trialMoment) - (yieldMoment + isotropicHardening * committed.AccumulatedPlasticRotation);
        if (f <= 0)
            return new HingeTrial(trialMoment, elasticRotationalStiffness, committed, 0);

        double increment = f / (elasticRotationalStiffness + isotropicHardening);
        double sign = Math.Sign(trialMoment);
        double moment = trialMoment - sign * elasticRotationalStiffness * increment;
        var next = new HingeState(committed.PlasticRotation + sign * increment,
            committed.AccumulatedPlasticRotation + increment);
        double dissipation = yieldMoment * increment;
        return new HingeTrial(moment,
            elasticRotationalStiffness * isotropicHardening /
            (elasticRotationalStiffness + isotropicHardening), next, dissipation);
    }

    private static void Close(double expected, double actual, double tolerance) =>
        Assert.InRange(Math.Abs(expected - actual), 0d, tolerance);
}
