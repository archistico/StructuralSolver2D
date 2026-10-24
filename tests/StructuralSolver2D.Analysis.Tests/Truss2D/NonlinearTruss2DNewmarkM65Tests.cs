using StructuralSolver2D.Analysis.Truss2D.Nonlinear;

namespace StructuralSolver2D.Analysis.Tests.Truss2D;

public sealed class NonlinearTruss2DNewmarkM65Tests
{
    private const double E = 200_000, A = .01, H = 10_000, SigmaYield = 250;

    // L=2 m, m_total=3 kN*s^2/m, one unconstrained axial DOF: m_eff=m_total/3=1.
    private static NonlinearTruss2DTimeRequest Bar(
        IReadOnlyList<double>? factors = null, double dt = .01,
        double yieldStress = SigmaYield, double hardening = H,
        int iterations = 30, double damping = 0, double initialVelocity = 0)
        => new(
            new[] { new IncrementalTrussNode("A", 0, 0), new IncrementalTrussNode("B", 2, 0) },
            new[] { new NonlinearDynamicTrussMember("AB", "A", "B", A, E, yieldStress, hardening, 1.5) },
            new[] { new IncrementalTrussSupport("A", 1, 0),
                    new IncrementalTrussSupport("A", 0, 1),
                    new IncrementalTrussSupport("B", 0, 1) },
            new[] { new IncrementalTrussLoad("B", 4, 0) },
            factors ?? new[] { 0d, .25 }, dt, MassDamping: damping,
            InitialVelocity: new[] { 0d, 0d, initialVelocity, 0d },
            MaximumNewtonIterations: iterations);

    private static void Close(double expected, double observed, double tolerance = 1e-9)
        => Assert.True(double.IsFinite(observed) && Math.Abs(expected - observed) <= tolerance,
            $"Expected={expected:G17}; observed={observed:G17}; tolerance={tolerance:G17}");

    [Fact]
    public void ConsistentMass_ElasticNewmarkHasExactOneStepClosedForm()
    {
        var result = NonlinearTruss2DNewmark.Solve(Bar(yieldStress: 1e8));
        Assert.True(result.Completed, result.Failure);
        Assert.Equal(2, result.Steps.Count);
        // Effective acceleration coefficient = 4m/dt² = 40000; k=EA/L=1000.
        double q = 1 / 41_000d;
        var step = result.Steps[1];
        Close(q, step.Displacements[2], 1e-12);
        Close(2 * q / .01, step.Velocities[2], 1e-12);
        Close(4 * q / (.01 * .01), step.Accelerations[2], 1e-10);
        Close(1000 * q, step.AxialForces[0], 1e-10);
        // Consistent off-diagonal mass contributes to the reaction at restrained A.
        Close(.5 * (4 * q / (.01 * .01)) - 1000 * q, step.Reactions[0], 1e-9);
        Close(0, step.ReducedResidualNorm, 1e-9);
        Close(0, step.DiscreteWorkResidual, 1e-10);
    }

    [Fact]
    public void YieldingStep_MatchesIndependentPiecewiseClosedForm()
    {
        // Force 4 kN, dt=0.1 s. Elastic predictor exceeds yield extension 0.0025 m.
        // In plastic branch: N=2.5 + k_t*(u-0.0025), k_t=EA/L*H/(E+H).
        const double dt = .1;
        double kt = (E * A / 2) * H / (E + H);
        double qExpected = (4 - 2.5 + kt * .0025) / (kt + 4 / (dt * dt));
        var result = NonlinearTruss2DNewmark.Solve(Bar(factors: new[] { 0d, 1d }, dt: dt));
        Assert.True(result.Completed, result.Failure);
        var step = result.Steps[1];
        Close(qExpected, step.Displacements[2], 1e-10);
        Close(2.5 + kt * (qExpected - .0025), step.AxialForces[0], 1e-9);
        Assert.True(step.MaterialStates[0].AccumulatedPlasticStrain > 0);
        Assert.True(step.CumulativePlasticDissipation > 0);
        Close(0, step.DiscreteWorkResidual, 1e-9);
        Assert.True(double.IsFinite(step.PhysicalEnergyResidual));
        Assert.Equal(step.MaterialStates[0], result.FinalStates[0]);
    }

    [Fact]
    public void FailedStep_RollsBackEntireStructure()
    {
        var result = NonlinearTruss2DNewmark.Solve(Bar(new[] { 0d, 1d }, dt: .1, iterations: 1));
        Assert.False(result.Completed);
        Assert.Contains("M65_NONCONVERGENCE", result.Failure);
        Assert.Single(result.Steps);
        Assert.Equal(new AxialMaterialState(), result.FinalStates[0]);
    }

    [Fact]
    public void NonzeroInitialVelocity_ConservesElasticEnergyWithoutDamping()
    {
        var result = NonlinearTruss2DNewmark.Solve(Bar(
            factors: Enumerable.Repeat(0d, 201).ToArray(), dt: .001,
            yieldStress: 1e8, initialVelocity: .2));
        Assert.True(result.Completed, result.Failure);
        foreach (var step in result.Steps)
        {
            Close(.5 * .2 * .2, step.KineticEnergy + step.RecoverableEnergy, 1e-9);
            Close(0, step.CumulativePlasticDissipation, 1e-12);
            Close(0, step.DiscreteWorkResidual, 1e-9);
        }
    }

    [Fact]
    public void MassDamping_CausesPositiveViscousDissipationAndEnergyDecay()
    {
        var result = NonlinearTruss2DNewmark.Solve(Bar(
            factors: Enumerable.Repeat(0d, 201).ToArray(), dt: .001,
            yieldStress: 1e8, initialVelocity: .2, damping: 2));
        Assert.True(result.Completed, result.Failure);
        Assert.True(result.Steps[^1].CumulativeViscousDissipation > 0);
        Assert.True(result.Steps[^1].KineticEnergy + result.Steps[^1].RecoverableEnergy <
                    result.Steps[0].KineticEnergy);
        Close(0, result.Steps[^1].DiscreteWorkResidual, 1e-9);
    }

    [Fact]
    public void PlasticStepFollowedByUnload_KeepsStateAndNonnegativeDissipation()
    {
        var result = NonlinearTruss2DNewmark.Solve(
            Bar(new[] { 0d, 1d, 1d, 0d, -1d, 0d }, dt: .1));
        Assert.True(result.Completed, result.Failure);
        Assert.Contains(result.Steps, s => s.MaterialStates[0].AccumulatedPlasticStrain > 0);
        for (int i = 1; i < result.Steps.Count; i++)
            Assert.True(result.Steps[i].CumulativePlasticDissipation >=
                        result.Steps[i - 1].CumulativePlasticDissipation - 1e-12);
    }

    [Fact]
    public void RotatedBarAndInclinedSupport_AreInvariantInAxialResponse()
    {
        double c = 1 / Math.Sqrt(2);
        var horizontal = Bar(new[] { 0d, 1d }, dt: .1);
        var rotated = horizontal with
        {
            Nodes = new[] { new IncrementalTrussNode("A", 0, 0),
                            new IncrementalTrussNode("B", 2 * c, 2 * c) },
            Supports = new[] { new IncrementalTrussSupport("A", 1, 0),
                               new IncrementalTrussSupport("A", 0, 1),
                               new IncrementalTrussSupport("B", -c, c) },
            Loads = new[] { new IncrementalTrussLoad("B", 4 * c, 4 * c) },
            InitialVelocity = new double[4],
        };
        var x = NonlinearTruss2DNewmark.Solve(horizontal);
        var y = NonlinearTruss2DNewmark.Solve(rotated);
        Assert.True(x.Completed, x.Failure);
        Assert.True(y.Completed, y.Failure);
        double axial = c * y.Steps[1].Displacements[2] + c * y.Steps[1].Displacements[3];
        Close(x.Steps[1].Displacements[2], axial, 1e-10);
        Close(x.Steps[1].AxialForces[0], y.Steps[1].AxialForces[0], 1e-9);
    }

    [Fact]
    public void InvalidMassAndIncompatibleInitialVelocity_AreRejected()
    {
        var bad = Bar() with { Members = new[] { new NonlinearDynamicTrussMember("AB", "A", "B",
                A, E, SigmaYield, H, 0) } };
        Assert.Throws<ArgumentException>(() => NonlinearTruss2DNewmark.Solve(bad));
        Assert.Throws<ArgumentException>(() => NonlinearTruss2DNewmark.Solve(
            Bar() with { InitialVelocity = new[] { 1d, 0d, 0d, 0d } }));
    }

    [Fact]
    public void TwoMemberFailure_IsAtomicAfterAnAcceptedElasticStep()
    {
        var original = Bar(new[] { 0d, .05, 1d }, dt: .1, iterations: 2);
        var request = original with
        {
            Members = new[]
            {
                new NonlinearDynamicTrussMember("weak", "A", "B", A, E, 250, H, 1.5),
                new NonlinearDynamicTrussMember("strong", "A", "B", A, E, 500, H, 1.5),
            },
            Loads = new[] { new IncrementalTrussLoad("B", 20, 0) },
        };
        var result = NonlinearTruss2DNewmark.Solve(request);
        Assert.False(result.Completed);
        Assert.Contains("M65_NONCONVERGENCE", result.Failure);
        Assert.Equal(2, result.Steps.Count); // initial and first accepted increment only
        Assert.Equal(new AxialMaterialState(), result.FinalStates[0]);
        Assert.Equal(new AxialMaterialState(), result.FinalStates[1]);
        Assert.Equal(result.Steps[1].MaterialStates[0], result.FinalStates[0]);
        Assert.Equal(result.Steps[1].MaterialStates[1], result.FinalStates[1]);
    }

    [Fact]
    public void LinearRamp_RefiningTimeStepConvergesTowardAnalyticalOscillator()
    {
        const double totalTime = .2;
        double omega = Math.Sqrt(1000d); // k=1000, consistent constrained mass=1
        double exact = 4d / 1000 * (1 - Math.Sin(omega * totalTime) / (omega * totalTime));
        double Error(int count)
        {
            var factors = Enumerable.Range(0, count + 1).Select(i => (double)i / count).ToArray();
            var r = NonlinearTruss2DNewmark.Solve(Bar(factors, totalTime / count, yieldStress: 1e8));
            Assert.True(r.Completed, r.Failure);
            return Math.Abs(r.Steps[^1].Displacements[2] - exact);
        }
        double coarse = Error(8), middle = Error(16), fine = Error(32);
        Assert.True(coarse > middle && middle > fine,
            $"Temporal convergence failed: {coarse:G6}, {middle:G6}, {fine:G6}");
        Assert.True(fine < 3e-5);
    }

}
