using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Frame2D.Nonlinear;

namespace StructuralSolver2D.Analysis.Tests.Frame2D.M72;

/// <summary>
/// Research M72 gate. Independent closed-form series benchmarks, local equilibrium,
/// directional derivatives and cyclic trial/commit. No Python and no global Frame2D solver.
/// </summary>
public sealed class ElastoplasticFrame2DM72Tests
{
    private const double E = 210_000_000d;
    private const double Area = 0.012d;
    private const double Inertia = 0.000032d;
    private const double L = 4d;
    private const double KTheta = 4000d;
    private const double My = 20d;
    private const double H = 40d;
    private static double KBeam => 4 * E * Inertia / L;

    private static ElastoplasticFrame2DElement Element(bool start = false, bool end = true,
        double h = H, int maxIterations = 40, bool rotated = false)
    {
        return new ElastoplasticFrame2DElement(0, 0, rotated ? 0 : L, rotated ? L : 0,
            E, Area, Inertia,
            start ? new BilinearRotationalHinge(KTheta, My, h) : null,
            end ? new BilinearRotationalHinge(KTheta, My, h) : null,
            maxIterations);
    }

    private static double[] RotationAtEnd(double radians) => new[] { 0d, 0d, 0d, 0d, 0d, radians };

    [Fact]
    public void NoHinges_IsExactlyElasticFrameStiffnessAndInternalForce()
    {
        var element = Element(start: false, end: false);
        double[] u = { .001, -.003, .02, -.002, .004, -.01 };
        var trial = element.EvaluateTrial(u);
        double[,] k = Frame2DElementMatrices.BuildLocalStiffness(E, Area, Inertia, L);
        for (int i = 0; i < 6; i++)
        {
            double f = 0;
            for (int j = 0; j < 6; j++)
            {
                Near(k[i, j], trial.GlobalTangent[i, j], 1e-11);
                f += k[i, j] * u[j];
            }
            Near(f, trial.GlobalInternalForces[i], 1e-11);
        }
        Near(0, trial.PlasticDissipationIncrement, 1e-14);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(4d)]
    [InlineData(100d)]
    public void ElasticEndHinge_IsBeamAndSpringInSeries(double ratio)
    {
        double spring = ratio * KBeam;
        var element = new ElastoplasticFrame2DElement(0, 0, L, 0, E, Area, Inertia,
            endHinge: new BilinearRotationalHinge(spring, 1e6, 0));
        const double theta = .002;
        var trial = element.EvaluateTrial(RotationAtEnd(theta));
        double expected = KBeam * spring / (KBeam + spring);
        Near(expected, trial.LocalTangent[5, 5], 1e-9);
        Near(expected * theta, trial.LocalInternalForces[5], 1e-10);
        Near(0, trial.StartTrialState.AccumulatedPlasticRotation, 1e-14);
        Near(0, trial.EndTrialState.AccumulatedPlasticRotation, 1e-14);
        // Beam end and hinge carry the SAME moment at their interface.
        double mBeam = KBeam * trial.InternalBeamDisplacements[5];
        Near(mBeam, trial.LocalInternalForces[5], 1e-10);
    }

    [Fact]
    public void MonotonicPlasticMomentAndTangent_AgreeWithClosedFormSeriesSolution()
    {
        const double theta = .025;
        var trial = Element().EvaluateTrial(RotationAtEnd(theta));
        // Analytic series law: theta = M/Kbeam + M/Ktheta + gamma,
        // M = My + H*gamma after positive yield from a virgin state.
        double moment = (theta + My / H) / (1 / KBeam + 1 / KTheta + 1 / H);
        double gamma = (moment - My) / H;
        double tangent = 1 / (1 / KBeam + 1 / KTheta + 1 / H);
        Assert.True(moment > My);
        Near(moment, trial.LocalInternalForces[5], 1e-9);
        Near(moment, trial.GlobalInternalForces[5], 1e-9);
        Near(tangent, trial.LocalTangent[5, 5], 1e-8);
        Near(gamma, trial.EndTrialState.PlasticRotation, 1e-11);
        Near(My * gamma, trial.PlasticDissipationIncrement, 1e-10);
        Near(0, trial.StartTrialState.AccumulatedPlasticRotation, 1e-14);
    }

    [Fact]
    public void MonotonicPlasticWork_EqualsRecoverableEnergyPlusDissipation()
    {
        const double theta = .025;
        var trial = Element().EvaluateTrial(RotationAtEnd(theta));
        double yieldRotation = My * (1 / KBeam + 1 / KTheta);
        double moment = trial.LocalInternalForces[5];
        // Independent exact integral of piecewise-linear external moment vs rotation.
        double work = 0.5 * My * yieldRotation +
            0.5 * (My + moment) * (theta - yieldRotation);
        Near(work, trial.RecoverableEnergy + trial.PlasticDissipationIncrement, 1e-9);
        Assert.True(trial.RecoverableEnergy > 0);
    }

    [Fact]
    public void PlasticTangent_IsDerivativeOfCondensedForcesInSameBranch()
    {
        var element = Element();
        double[] u = { .0005, .003, -.002, .0007, -.001, .035 };
        var center = element.EvaluateTrial(u);
        Assert.True(center.EndTrialState.AccumulatedPlasticRotation > 0);
        const double eps = 1e-7;
        for (int j = 0; j < 6; j++)
        {
            var plus = (double[])u.Clone();
            var minus = (double[])u.Clone();
            plus[j] += eps;
            minus[j] -= eps;
            var p = element.EvaluateTrial(plus);
            var n = element.EvaluateTrial(minus);
            for (int i = 0; i < 6; i++)
            {
                double finiteDifference = (p.LocalInternalForces[i] - n.LocalInternalForces[i]) / (2 * eps);
                double scale = Math.Max(1, Math.Abs(center.LocalTangent[i, j]));
                Assert.True(Math.Abs(finiteDifference - center.LocalTangent[i, j]) <= 2e-6 * scale,
                    $"M72 consistent tangent [{i},{j}]: reference {finiteDifference}, actual {center.LocalTangent[i, j]}");
            }
        }
        Assert.Equal(new RotationalHingeState(), element.EndCommittedState); // all derivative calls were pure trials
    }

    [Fact]
    public void BothHinges_EquilibrateAndCondenseSymmetrically()
    {
        var element = Element(start: true, end: true);
        double[] u = { 0d, 0d, -.045, 0d, .001, .042 };
        var trial = element.EvaluateTrial(u);
        Assert.True(trial.StartTrialState.AccumulatedPlasticRotation > 0);
        Assert.True(trial.EndTrialState.AccumulatedPlasticRotation > 0);
        var beamForces = Multiply(Frame2DElementMatrices.BuildLocalStiffness(E, Area, Inertia, L),
            trial.InternalBeamDisplacements);
        Near(beamForces[2], trial.LocalInternalForces[2], 1e-9);
        Near(beamForces[5], trial.LocalInternalForces[5], 1e-9);
        Near(0, trial.LocalInternalForces[1] + trial.LocalInternalForces[4], 1e-8);
        for (int i = 0; i < 6; i++)
            for (int j = 0; j < 6; j++)
                Near(trial.LocalTangent[i, j], trial.LocalTangent[j, i], 1e-8);
        Assert.True(trial.LocalIterations >= 2);
    }

    [Fact]
    public void RotatingWholeElement_PreservesPhysicalResults()
    {
        var horizontal = Element();
        var vertical = Element(rotated: true);
        double[] horizontalU = { -.0005, .003, .001, .0015, -.004, .028 };
        double[] verticalU = RotatedVector(horizontalU);
        var a = horizontal.EvaluateTrial(horizontalU);
        var b = vertical.EvaluateTrial(verticalU);
        var rotatedForce = RotatedVector(a.GlobalInternalForces);
        for (int i = 0; i < 6; i++)
            Near(rotatedForce[i], b.GlobalInternalForces[i], 1e-8);
        Near(a.PlasticDissipationIncrement, b.PlasticDissipationIncrement, 1e-10);
        Near(a.EndTrialState.PlasticRotation, b.EndTrialState.PlasticRotation, 1e-10);
    }

    [Fact]
    public void TrialsDoNotCommit_AndStaleOrForeignCommitIsRejected()
    {
        var first = Element();
        var second = Element();
        var trial = first.EvaluateTrial(RotationAtEnd(.03));
        Assert.True(trial.EndTrialState.AccumulatedPlasticRotation > 0);
        Assert.Equal(new RotationalHingeState(), first.EndCommittedState);
        Assert.Throws<InvalidOperationException>(() => second.Commit(trial));
        first.Commit(trial);
        var committed = first.EndCommittedState;
        Assert.Throws<InvalidOperationException>(() => first.Commit(trial));
        Assert.Equal(committed, first.EndCommittedState);
        var unloaded = first.EvaluateTrial(RotationAtEnd(.022));
        Near(0, unloaded.PlasticDissipationIncrement, 1e-10);
        Assert.Equal(committed, first.EndCommittedState);
        Assert.Equal(committed, unloaded.EndTrialState);
    }

    [Fact]
    public void IncrementalLoadControlOfCantilever_FollowsAnalyticMomentRotationWithCommit()
    {
        var element = Element();
        double[] targets = { 0, 10, 19, 20.2, 10, 0, -12, -20.5 };
        double rotation = 0;
        double totalDissipation = 0;
        foreach (double imposedMoment in targets)
        {
            // At the cantilever free end, all DOFs except Rz are restrained here.
            // External Newton loop is test-only; public nonlinear frame assembly is NOT exposed.
            ElastoplasticFrame2DTrial? accepted = null;
            for (int iteration = 0; iteration < 35; iteration++)
            {
                var candidate = element.EvaluateTrial(RotationAtEnd(rotation));
                double residual = imposedMoment - candidate.GlobalInternalForces[5];
                if (Math.Abs(residual) < 1e-8)
                {
                    accepted = candidate;
                    break;
                }
                Assert.True(candidate.GlobalTangent[5, 5] > 0, "M72 test assumes positive H.");
                rotation += residual / candidate.GlobalTangent[5, 5];
            }
            Assert.NotNull(accepted);
            Near(imposedMoment, accepted!.GlobalInternalForces[5], 1e-7);
            Assert.True(accepted.PlasticDissipationIncrement >= 0);
            totalDissipation += accepted.PlasticDissipationIncrement;
            element.Commit(accepted);
        }
        Assert.True(totalDissipation > 0);
        Assert.True(element.EndCommittedState.AccumulatedPlasticRotation > 0);
    }

    [Fact]
    public void PerfectPlasticity_DoesNotInventArtificialTangent()
    {
        var element = Element(h: 0);
        var trial = element.EvaluateTrial(RotationAtEnd(.025));
        Near(My, trial.LocalInternalForces[5], 1e-9);
        Near(0, trial.LocalTangent[5, 5], 1e-8);
        for (int i = 0; i < 6; i++) Near(0, trial.LocalTangent[i, 5], 1e-8);
    }

    [Fact]
    public void FailedLocalEquilibrium_LeavesCommittedHingesUnchanged()
    {
        var element = Element(maxIterations: 1);
        Assert.Throws<StructuralAnalysisException>(() => element.EvaluateTrial(RotationAtEnd(.025)));
        Assert.Equal(new RotationalHingeState(), element.EndCommittedState);
    }

    [Fact]
    public void InvalidInputAndStates_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => new BilinearRotationalHinge(0, My, H));
        Assert.Throws<ArgumentException>(() => new BilinearRotationalHinge(KTheta, My, -1));
        Assert.Throws<ArgumentException>(() => new ElastoplasticFrame2DElement(0, 0, 0, 0, E, Area, Inertia));
        Assert.Throws<ArgumentException>(() => Element().EvaluateTrial(new double[5]));
        Assert.Throws<ArgumentException>(() => Element().EvaluateTrial(new[] { 0d, 0d, 0d, 0d, 0d, double.NaN }));
        var hinge = new BilinearRotationalHinge(KTheta, My, H);
        Assert.Throws<ArgumentException>(() => hinge.Evaluate(0, new RotationalHingeState(.2, .01)));
        Assert.Throws<ArgumentException>(() => hinge.Evaluate(double.PositiveInfinity, default));
    }

    private static double[] Multiply(double[,] a, double[] v)
    {
        var output = new double[v.Length];
        for (int i = 0; i < v.Length; i++)
            for (int j = 0; j < v.Length; j++) output[i] += a[i, j] * v[j];
        return output;
    }

    private static double[] RotatedVector(double[] u) =>
        new[] { -u[1], u[0], u[2], -u[4], u[3], u[5] };

    private static void Near(double expected, double actual, double tolerance) =>
        Assert.True(double.IsFinite(actual) && Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected:G17}, obtained {actual:G17} (abs tolerance {tolerance:G5}).");
}

