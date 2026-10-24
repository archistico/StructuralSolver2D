using StructuralSolver2D.Analysis.Dynamics.Nonlinear;
using StructuralSolver2D.Analysis.Dynamics.Transient;

namespace StructuralSolver2D.Analysis.Tests.Dynamics.M62;

public sealed class NonlinearSdofM62Tests
{
    private static NonlinearSdofParameters Parameters(double dt = .005, double fy = .4,
        double hardening = 100, double damping = .5, int maxNewton = 20) =>
        new(1, 1000, fy, hardening, damping, dt, maxNewton);

    [Fact]
    public void BilinearReturnMapping_YieldStressAndConsistentTangentMatchClosedForm()
    {
        var spring = new BilinearIsotropicSpring(200_000, 250, 10_000);
        var committed = new BilinearSpringState();
        var elastic = spring.Evaluate(.001, committed);
        Assert.Equal(200, elastic.InternalForce, 10);
        Assert.Equal(200_000, elastic.ConsistentTangent);
        Assert.Equal(committed, elastic.TrialState);
        Assert.Equal(0, elastic.PlasticDissipationIncrement);

        var plastic = spring.Evaluate(.002, committed);
        Assert.InRange(Math.Abs(plastic.InternalForce - 257.14285714285717), 0, 1e-10);
        Assert.InRange(Math.Abs(plastic.ConsistentTangent - 9523.809523809523), 0, 1e-10);
        Assert.True(plastic.TrialState.AccumulatedPlasticDisplacement > 0);
        Assert.True(plastic.PlasticDissipationIncrement > 0);
        Assert.Equal(new BilinearSpringState(), committed); // trial never commits implicitly

        var unloading = spring.Evaluate(.001, plastic.TrialState);
        Assert.Equal(200_000, unloading.ConsistentTangent);
        Assert.Equal(plastic.TrialState, unloading.TrialState);
        Assert.Equal(0, unloading.PlasticDissipationIncrement);
    }

    [Fact]
    public void BilinearReturnMapping_ReverseCyclesAndPerfectPlasticityRemainAdmissible()
    {
        var material = new BilinearIsotropicSpring(200_000, 250, 10_000);
        var state = new BilinearSpringState();
        double dissipation = 0;
        foreach (double u in new[] { 0d, .001, .002, .003, 0, -.003, 0, .003, -.003 })
        {
            var trial = material.Evaluate(u, state);
            Assert.True(trial.PlasticDissipationIncrement >= 0);
            Assert.True(trial.TrialState.AccumulatedPlasticDisplacement >= state.AccumulatedPlasticDisplacement);
            dissipation += trial.PlasticDissipationIncrement;
            state = trial.TrialState;
        }
        Assert.True(dissipation > 0);
        var perfect = new BilinearIsotropicSpring(1000, 2, 0).Evaluate(.01, new BilinearSpringState());
        Assert.Equal(2, perfect.InternalForce, 10);
        Assert.Equal(0, perfect.ConsistentTangent);
        Assert.True(perfect.PlasticDissipationIncrement > 0);
    }

    [Fact]
    public void PurelyElasticOscillator_MatchesExistingNewmarkAndShowsSecondOrderConvergence()
    {
        double Error(double dt)
        {
            var solver = new NonlinearSdofNewmark(new NonlinearSdofParameters(2, 200, 1e9, 0, 0, dt),
                initialDisplacement: .02);
            var linear = new NewmarkAverageAcceleration(10, 0, dt, .02, 0, 0);
            double e0 = solver.Current.KineticEnergy + solver.Current.RecoverableElasticEnergy;
            for (int i = 0; i < (int)Math.Round(.5 / dt); i++)
            {
                var s = solver.Advance(0);
                linear.Advance(0);
                Assert.InRange(Math.Abs(s.Displacement - linear.Displacement), 0, 2e-12);
                Assert.InRange(Math.Abs(s.Velocity - linear.Velocity), 0, 2e-10);
                Assert.InRange(Math.Abs(s.KineticEnergy + s.RecoverableElasticEnergy - e0), 0, 1e-10);
                Assert.InRange(Math.Abs(s.DiscreteWorkBalanceResidual), 0, 1e-10);
                Assert.InRange(Math.Abs(s.PhysicalEnergyBalanceResidual), 0, 1e-10);
                Assert.Equal(0, s.CumulativePlasticDissipation);
            }
            return Math.Abs(solver.Current.Displacement - .02 * Math.Cos(5));
        }
        double coarse = Error(.01);
        double fine = Error(.005);
        Assert.True(fine < .28 * coarse, $"Second-order error reduction expected; {coarse}, {fine}");
    }

    [Fact]
    public void ConstantForce_OneStepMatchesExactElasticNewmarkEquation()
    {
        const double dt = .01;
        var solver = new NonlinearSdofNewmark(new NonlinearSdofParameters(2, 200, 1e6, 0, 0, dt));
        var step = solver.Advance(3);
        double expected = (.25 * dt * dt * 3 / 2) / (1 + .25 * dt * dt * 200 / 2);
        Assert.InRange(Math.Abs(step.Displacement - expected), 0, 1e-13);
        Assert.InRange(Math.Abs(step.EquilibriumResidual), 0, 1e-9);
    }

    [Fact]
    public void FinitePulse_ProducesPlasticityWithPositiveDissipationAndSmallDiscreteResidual()
    {
        var solver = new NonlinearSdofNewmark(Parameters());
        double lastPlastic = 0, lastViscous = 0;
        for (int i = 1; i <= 160; i++)
        {
            double force = i <= 20 ? 5 : 0; // finite-duration pulse, NOT a Dirac impulse
            var s = solver.Advance(force);
            Assert.True(double.IsFinite(s.Displacement));
            Assert.True(double.IsFinite(s.PhysicalEnergyBalanceResidual));
            Assert.InRange(Math.Abs(s.EquilibriumResidual), 0, 1e-7);
            Assert.InRange(Math.Abs(s.DiscreteWorkBalanceResidual), 0, 1e-8);
            Assert.True(s.CumulativePlasticDissipation >= lastPlastic - 1e-12);
            Assert.True(s.CumulativeViscousDissipation >= lastViscous - 1e-12);
            Assert.True(s.MaterialState.AccumulatedPlasticDisplacement >= 0);
            lastPlastic = s.CumulativePlasticDissipation;
            lastViscous = s.CumulativeViscousDissipation;
        }
        Assert.True(lastPlastic > 0);
        Assert.True(lastViscous > 0);
    }

    [Fact]
    public void FinitePulse_MatchesIndependentPiecewiseAnalyticalReferenceAtFourTimes()
    {
        // Reference: scripts/m62-reference-piecewise-sdof.py, which analytically
        // selects the elastic/positive-plastic/negative-plastic branch per step;
        // it does not use the Newton solver under test.
        var expected = new (int Step, double U, double V, double Alpha, double Dissipation)[]
        {
            (20,  .02039450801738481, .3818684252850392,  .01817682547034983, .007270730188139931),
            (40,  .04316810104112660, .02985101093318743, .03888009185556964, .015552036742227860),
            (80,  .04298928366248342, .02463528877175392, .03896689307140128, .015586757228560510),
            (160, .04264419779621661, .01580238952113522, .03896689307140128, .015586757228560510),
        };
        var solver = new NonlinearSdofNewmark(Parameters());
        int slot = 0;
        for (int i = 1; i <= 160; i++)
        {
            var step = solver.Advance(i <= 20 ? 5 : 0);
            if (slot >= expected.Length || i != expected[slot].Step) continue;
            var reference = expected[slot++];
            Assert.InRange(Math.Abs(step.Displacement - reference.U), 0, 2e-9);
            Assert.InRange(Math.Abs(step.Velocity - reference.V), 0, 2e-9);
            Assert.InRange(Math.Abs(step.MaterialState.AccumulatedPlasticDisplacement - reference.Alpha), 0, 2e-9);
            Assert.InRange(Math.Abs(step.CumulativePlasticDissipation - reference.Dissipation), 0, 2e-9);
        }
        Assert.Equal(expected.Length, slot);
    }

    [Fact]
    public void TimeRefinement_MovesDisplacementTowardIndependentFineReference()
    {
        double Solve(double dt)
        {
            var solver = new NonlinearSdofNewmark(Parameters(dt: dt));
            int count = (int)Math.Round(.8 / dt);
            for (int i = 1; i <= count; i++) solver.Advance(i * dt <= .1 + 1e-12 ? 5 : 0);
            return solver.Current.Displacement;
        }
        const double fineReference = .042718661354043064; // piecewise branch solution, dt=.00125
        double coarseError = Math.Abs(Solve(.01) - fineReference);
        double mediumError = Math.Abs(Solve(.005) - fineReference);
        double fineError = Math.Abs(Solve(.0025) - fineReference);
        Assert.True(mediumError < coarseError && fineError < mediumError,
            $"Expected convergence under refinement: {coarseError}, {mediumError}, {fineError}");
    }

    [Fact]
    public void NewtonFailure_LeavesCommittedMaterialTimeAndMotionUntouched()
    {
        var solver = new NonlinearSdofNewmark(Parameters(maxNewton: 1));
        var committed = solver.Current;
        var error = Assert.Throws<InvalidOperationException>(() => solver.Advance(100));
        Assert.Contains("M62_NEWTON_NOT_CONVERGED", error.Message);
        Assert.Equal(committed, solver.Current);
        Assert.Equal(0, solver.Current.MaterialState.AccumulatedPlasticDisplacement);
        Assert.Equal(0, solver.Current.Time);
    }

    [Fact]
    public void InvalidInputsAndPlasticInitialConditions_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => new BilinearIsotropicSpring(0, 1, 0));
        Assert.Throws<ArgumentException>(() => new BilinearIsotropicSpring(1, -1, 0));
        Assert.Throws<ArgumentException>(() => new BilinearIsotropicSpring(1, 1, -1));
        Assert.Throws<ArgumentException>(() => new BilinearIsotropicSpring(1, 1, 0).Evaluate(double.NaN, default));
        Assert.Throws<ArgumentException>(() => new NonlinearSdofNewmark(Parameters(dt: 0)));
        Assert.Throws<ArgumentException>(() => new NonlinearSdofNewmark(Parameters(maxNewton: 0)));
        Assert.Throws<ArgumentException>(() => new NonlinearSdofNewmark(Parameters(), initialDisplacement: .01));
        var solver = new NonlinearSdofNewmark(Parameters());
        var initial = solver.Current;
        Assert.Throws<ArgumentException>(() => solver.Advance(double.PositiveInfinity));
        Assert.Equal(initial, solver.Current);
    }

    [Fact]
    public void MalformedForce_AfterCommittedPlasticHistory_DoesNotAlterCommittedState()
    {
        var solver = new NonlinearSdofNewmark(Parameters());
        for (int i = 0; i < 20; i++) solver.Advance(5);
        Assert.True(solver.Current.MaterialState.AccumulatedPlasticDisplacement > 0);
        var committed = solver.Current;
        Assert.Throws<ArgumentException>(() => solver.Advance(double.NaN));
        Assert.Equal(committed, solver.Current);
    }

    [Fact]
    public void NegativeAndPositiveForcePulses_AreSymmetricWithIsotropicHardening()
    {
        var plus = new NonlinearSdofNewmark(Parameters(damping: 0));
        var minus = new NonlinearSdofNewmark(Parameters(damping: 0));
        for (int i = 1; i <= 80; i++)
        {
            double force = i <= 16 ? 5 : 0;
            var p = plus.Advance(force);
            var n = minus.Advance(-force);
            Assert.InRange(Math.Abs(p.Displacement + n.Displacement), 0, 1e-12);
            Assert.InRange(Math.Abs(p.Velocity + n.Velocity), 0, 1e-11);
            Assert.InRange(Math.Abs(p.InternalForce + n.InternalForce), 0, 1e-10);
            Assert.InRange(Math.Abs(p.CumulativePlasticDissipation - n.CumulativePlasticDissipation), 0, 1e-10);
        }
        Assert.True(plus.Current.CumulativePlasticDissipation > 0);
    }
}
