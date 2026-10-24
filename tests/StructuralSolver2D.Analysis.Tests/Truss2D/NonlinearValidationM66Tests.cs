using StructuralSolver2D.Analysis.Truss2D.Nonlinear;

namespace StructuralSolver2D.Analysis.Tests.Truss2D;

/// <summary>M66 pre-public-API numerical gate. Independent references use branchwise
/// algebra, not M63/M64/M65 Newton, assembly or constitutive production code.
/// ALL tolerances are chosen a priori and expressed in physical units.</summary>
public sealed class NonlinearValidationM66Tests
{
    private const double E = 200_000, A = .01, L = 2, H = 10_000, Y = 250;

    private static void Near(double expected, double actual, double abs = 1e-9, double rel = 1e-8)
        => Assert.True(double.IsFinite(actual) && Math.Abs(actual - expected) <=
            abs + rel * Math.Max(Math.Abs(expected), Math.Abs(actual)),
            $"M66 benchmark: expected {expected:G17}, actual {actual:G17}, absTol={abs:G4}, relTol={rel:G4}");

    private static IncrementalTrussRequest TwoBars(IReadOnlyList<double> factors) => new(
        new[] { new IncrementalTrussNode("A", 0, 0), new IncrementalTrussNode("B", L, 0) },
        new[] { new IncrementalTrussMember("weak", "A", "B", A, E, 150, H),
                new IncrementalTrussMember("strong", "A", "B", A, E, 300, H) },
        new[] { new IncrementalTrussSupport("A", 1, 0),
                new IncrementalTrussSupport("A", 0, 1),
                new IncrementalTrussSupport("B", 0, 1) },
        new[] { new IncrementalTrussLoad("B", 4, 0) }, factors);

    private static NonlinearTruss2DTimeRequest Bar(IReadOnlyList<double> factors, double dt,
        double massDamping = 0, double stiffnessDamping = 0) => new(
        new[] { new IncrementalTrussNode("A", 0, 0), new IncrementalTrussNode("B", L, 0) },
        new[] { new NonlinearDynamicTrussMember("AB", "A", "B", A, E, Y, H, 1.5) },
        new[] { new IncrementalTrussSupport("A", 1, 0),
                new IncrementalTrussSupport("A", 0, 1),
                new IncrementalTrussSupport("B", 0, 1) },
        new[] { new IncrementalTrussLoad("B", 4, 0) }, factors, dt,
        MassDamping: massDamping, InitialStiffnessDamping: stiffnessDamping);

    [Fact]
    public void ParallelBars_IndependentMonotonePiecewiseEquilibriumAndYieldOnset()
    {
        var factors = new[] { .25d, .75d, 1d, 1.5d, 2d };
        var result = IncrementalElastoplasticTruss2DSolver.Solve(TwoBars(factors));
        Assert.True(result.Completed, result.Failure);
        Assert.Equal(factors.Length, result.Steps.Count);
        for (int i = 0; i < factors.Length; i++)
        {
            var s = result.Steps[i];
            double f = 4 * factors[i];
            double expectedU = NonlinearM66AlgebraicReference.VirginTwoBarExtension(f);
            double weak = NonlinearM66AlgebraicReference.VirginForceAtPositiveExtension(
                expectedU, A, E, 150, H, L);
            double strong = NonlinearM66AlgebraicReference.VirginForceAtPositiveExtension(
                expectedU, A, E, 300, H, L);
            Near(expectedU, s.Displacements[2], 1e-10);
            Near(weak, s.AxialForces[0], 1e-8);
            Near(strong, s.AxialForces[1], 1e-8);
            Near(f, s.AxialForces.Sum(), 1e-8);
            Near(-f, s.Reactions[0], 1e-8);
            Near(0, s.ResidualNorm, 1e-8);
            Assert.True(s.CumulativePlasticDissipation >= -1e-12);
        }
        Assert.Equal(new AxialMaterialState(), result.Steps[1].MaterialStates[1]);
        Assert.True(result.Steps[1].MaterialStates[0].AccumulatedPlasticStrain >= 0);
        Assert.True(result.Steps[^1].MaterialStates.All(s => s.AccumulatedPlasticStrain > 0));
    }

    [Fact]
    public void MonotoneLoadSubdivision_DoesNotAlterFinalStateOrDissipatedEnergy()
    {
        var few = IncrementalElastoplasticTruss2DSolver.Solve(
            TwoBars(new[] { .25d, .75d, 1d, 1.5d, 2d }));
        var many = IncrementalElastoplasticTruss2DSolver.Solve(
            TwoBars(Enumerable.Range(1, 20).Select(i => i / 10d).ToArray()));
        Assert.True(few.Completed, few.Failure);
        Assert.True(many.Completed, many.Failure);
        var a = few.Steps[^1]; var b = many.Steps[^1];
        Near(a.Displacements[2], b.Displacements[2], 1e-9);
        Near(a.CumulativePlasticDissipation, b.CumulativePlasticDissipation, 1e-9);
        for (int i = 0; i < 2; i++)
        {
            Near(a.AxialForces[i], b.AxialForces[i], 1e-8);
            Near(a.MaterialStates[i].PlasticStrain, b.MaterialStates[i].PlasticStrain, 1e-10);
            Near(a.MaterialStates[i].AccumulatedPlasticStrain,
                b.MaterialStates[i].AccumulatedPlasticStrain, 1e-10);
        }
    }

    [Fact]
    public void RotatedElement_ZeroRigidTranslationAndSymmetricDirectionalTangent()
    {
        double angle = 37 * Math.PI / 180;
        double c = Math.Cos(angle), si = Math.Sin(angle);
        var element = new ElastoplasticTruss2DElement(1, -2,
            1 + L * c, -2 + L * si, A, E, Y, H);
        var rigid = element.EvaluateTrial(new[] { .06, -.04, .06, -.04 });
        Near(0, rigid.AxialForce, 1e-9);
        Assert.All(rigid.InternalForces, v => Near(0, v, 1e-9));
        var u = new[] { 0d, 0d, .002 * c, .002 * si }; // below yield
        var baseTrial = element.EvaluateTrial(u);
        double[] direction = { -.2, .3, .4, -.6 };
        const double h = 1e-7;
        var plus = element.EvaluateTrial(u.Zip(direction, (a, b) => a + h * b).ToArray());
        var minus = element.EvaluateTrial(u.Zip(direction, (a, b) => a - h * b).ToArray());
        for (int row = 0; row < 4; row++)
        {
            double expected = 0;
            for (int col = 0; col < 4; col++)
            {
                expected += baseTrial.TangentStiffness[row, col] * direction[col];
                Near(baseTrial.TangentStiffness[row, col],
                    baseTrial.TangentStiffness[col, row], 1e-10);
            }
            Near(expected, (plus.InternalForces[row] - minus.InternalForces[row]) / (2 * h), 1e-6);
        }
        Assert.Equal(new AxialMaterialState(), element.CommittedState);
    }

    [Fact]
    public void ScalarNonlinearDynamicHistory_IndependentAnalyticBranchwiseNewmark()
    {
        // Includes hardening, unloading and reverse plasticity. This is NOT
        // a call to the M62 spring or a copy of the production Newton loop.
        var factors = new[] { 0d, .5, 1, 0, -.5, -1, -1, 0, .9, 0 };
        const double dt = .1, alpha = .4, beta = .0001;
        var reference = NonlinearM66AlgebraicReference.Solve(factors, dt,
            massDamping: alpha, stiffnessDamping: beta);
        var actual = NonlinearTruss2DNewmark.Solve(Bar(factors, dt, alpha, beta));
        Assert.True(actual.Completed, actual.Failure);
        Assert.Equal(reference.Count, actual.Steps.Count);
        for (int i = 0; i < reference.Count; i++)
        {
            var r = reference[i]; var a = actual.Steps[i];
            Near(r.U, a.Displacements[2], 2e-10);
            Near(r.V, a.Velocities[2], 2e-9);
            Near(r.A, a.Accelerations[2], 2e-8);
            Near(r.N, a.AxialForces[0], 2e-8);
            Near(r.Material.PlasticStrain, a.MaterialStates[0].PlasticStrain, 1e-10);
            Near(r.Material.AccumulatedPlasticStrain,
                a.MaterialStates[0].AccumulatedPlasticStrain, 1e-10);
            Near(r.PlasticEnergy, a.CumulativePlasticDissipation, 1e-9);
            Near(r.ElasticEnergy, a.RecoverableEnergy, 1e-9);
            Near(r.HardeningEnergy, a.HardeningEnergy, 1e-9);
            Near(0, a.ReducedResidualNorm, 1e-8);
            Near(0, a.DiscreteWorkResidual, 1e-8);
        }
        Assert.Contains(actual.Steps, s => s.MaterialStates[0].AccumulatedPlasticStrain > 0);
        Assert.True(actual.Steps[^1].CumulativePlasticDissipation > 0);
    }

    [Fact]
    public void ParallelDynamicBars_PartitionOfAreaAndMassDoesNotChangeResponse()
    {
        var factors = new[] { 0d, .5, 1, .75, 0, -1, 0, .8 };
        var single = Bar(factors, .1, .5, .0001);
        var divided = single with
        {
            Members = new[]
            {
                new NonlinearDynamicTrussMember("left", "A", "B", A / 2, E, Y, H, .75),
                new NonlinearDynamicTrussMember("right", "A", "B", A / 2, E, Y, H, .75),
            }
        };
        var x = NonlinearTruss2DNewmark.Solve(single);
        var y = NonlinearTruss2DNewmark.Solve(divided);
        Assert.True(x.Completed, x.Failure);
        Assert.True(y.Completed, y.Failure);
        for (int i = 0; i < x.Steps.Count; i++)
        {
            var a = x.Steps[i]; var b = y.Steps[i];
            Near(a.Displacements[2], b.Displacements[2], 1e-10);
            Near(a.Velocities[2], b.Velocities[2], 1e-10);
            Near(a.Accelerations[2], b.Accelerations[2], 1e-8);
            Near(a.AxialForces[0], b.AxialForces.Sum(), 1e-8);
            Near(a.CumulativePlasticDissipation, b.CumulativePlasticDissipation, 1e-9);
            Near(a.CumulativeViscousDissipation, b.CumulativeViscousDissipation, 1e-9);
            Near(a.Reactions[0], b.Reactions[0], 1e-8);
            Near(b.AxialForces[0], b.AxialForces[1], 1e-9);
            Assert.Equal(b.MaterialStates[0], b.MaterialStates[1]);
        }
    }

    [Fact]
    public void ElasticLimit_RefinementAgainstIndependentClosedFormRamp()
    {
        // A slowly varying applied ramp has exact analytic response for an initially
        // unstressed oscillator: x(T)=F/k*(1-sin(omega*T)/(omega*T)).
        const double total = .2;
        double omega = Math.Sqrt(NonlinearM66AlgebraicReference.InitialStiffness /
                                 NonlinearM66AlgebraicReference.EffectiveMass);
        double exact = 4 / NonlinearM66AlgebraicReference.InitialStiffness *
            (1 - Math.Sin(omega * total) / (omega * total));
        double Error(int n)
        {
            var factors = Enumerable.Range(0, n + 1).Select(i => (double)i / n).ToArray();
            var request = Bar(factors, total / n) with
            {
                Members = new[] { new NonlinearDynamicTrussMember("AB", "A", "B", A, E, 1e8, H, 1.5) }
            };
            var r = NonlinearTruss2DNewmark.Solve(request);
            Assert.True(r.Completed, r.Failure);
            return Math.Abs(r.Steps[^1].Displacements[2] - exact);
        }
        var e1 = Error(8); var e2 = Error(16); var e3 = Error(32);
        Assert.True(e1 > e2 && e2 > e3, $"M66 time convergence: {e1:G8} {e2:G8} {e3:G8}");
        Assert.True(e3 < 3e-5);
    }

    [Fact]
    public void CorruptedConstitutiveHistory_IsRejectedWithoutMutation()
    {
        var m = new BilinearIsotropicAxialMaterial(E, Y, H);
        foreach (var invalid in new[]
        {
            new AxialMaterialState(double.NaN, 0),
            new AxialMaterialState(0, double.PositiveInfinity),
            new AxialMaterialState(.3, .01),
            new AxialMaterialState(0, -.5),
        })
            Assert.Throws<ArgumentException>(() => m.Evaluate(.01, invalid));
        var e = new ElastoplasticTruss2DElement(0, 0, L, 0, A, E, Y, H);
        var virgin = e.CommittedState;
        _ = e.EvaluateTrial(new double[] { 0, 0, .01, 0 });
        Assert.Equal(virgin, e.CommittedState);
    }

    [Fact]
    public void DynamicUnrestrainedTransverseMechanism_IsNotMaskedByInertia()
    {
        var broken = Bar(new[] { 0d, .3, 1d }, .05) with
        {
            Supports = new[] { new IncrementalTrussSupport("A", 1, 0),
                               new IncrementalTrussSupport("A", 0, 1) }
        };
        var ex = Assert.Throws<ArgumentException>(() => NonlinearTruss2DNewmark.Solve(broken));
        Assert.Contains("M65_MECHANISM", ex.Message);
    }

    [Fact]
    public void PlasticDissipation_NeverDecreasesAndFailedNewtonKeepsAcceptedState()
    {
        var factors = new[] { 0d, .1, 1, 0, -1, 0, 1 };
        var full = NonlinearTruss2DNewmark.Solve(Bar(factors, .1));
        Assert.True(full.Completed, full.Failure);
        for (int i = 1; i < full.Steps.Count; i++)
        {
            Assert.True(full.Steps[i].CumulativePlasticDissipation >=
                        full.Steps[i - 1].CumulativePlasticDissipation - 1e-12);
            Assert.True(full.Steps[i].MaterialStates[0].AccumulatedPlasticStrain >=
                        full.Steps[i - 1].MaterialStates[0].AccumulatedPlasticStrain - 1e-12);
        }
        // Different yield stresses cause a branch change within the same global step.
        // Two Newton evaluations accept the first elastic step, but not the large jump.
        var previous = Bar(new[] { 0d, .05, 1d }, .1);
        var difficult = previous with
        {
            Members = new[]
            {
                new NonlinearDynamicTrussMember("weak", "A", "B", A, E, 250, H, 1.5),
                new NonlinearDynamicTrussMember("strong", "A", "B", A, E, 500, H, 1.5)
            },
            Loads = new[] { new IncrementalTrussLoad("B", 20, 0) },
            MaximumNewtonIterations = 2
        };
        var failed = NonlinearTruss2DNewmark.Solve(difficult);
        Assert.False(failed.Completed);
        Assert.Equal(2, failed.Steps.Count); // initial and a previous accepted increment
        Assert.Equal(failed.Steps[^1].MaterialStates[0], failed.FinalStates[0]);
        Assert.Equal(failed.Steps[^1].MaterialStates[1], failed.FinalStates[1]);
        Assert.All(failed.Steps, s => Near(0, s.ReducedResidualNorm, 1e-8));
    }
}
