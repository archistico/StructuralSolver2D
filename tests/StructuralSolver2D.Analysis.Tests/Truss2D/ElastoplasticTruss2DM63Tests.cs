using StructuralSolver2D.Analysis.Dynamics.Nonlinear;
using StructuralSolver2D.Analysis.Truss2D.Nonlinear;

namespace StructuralSolver2D.Analysis.Tests.Truss2D;

public sealed class ElastoplasticTruss2DM63Tests
{
    private const double E = 200_000;
    private const double A = .01;
    private const double Fy = 250;
    private const double H = 10_000;

    private static ElastoplasticTruss2DElement Bar(double x1 = 0, double y1 = 0,
        double x2 = 2, double y2 = 0, double hardening = H) =>
        new(x1, y1, x2, y2, A, E, Fy, hardening);

    private static double[] AxialDofs(double extension) => new[] { 0d, 0d, extension, 0d };

    [Fact]
    public void ConstitutiveLaw_ClosedFormReturnMappingAndElasticUnloading()
    {
        var mat = new BilinearIsotropicAxialMaterial(E, Fy, H);
        var virgin = new AxialMaterialState();
        var elastic = mat.Evaluate(.001, virgin);
        Assert.Equal(200, elastic.Stress, 10);
        Assert.Equal(E, elastic.TangentModulus);
        Assert.Equal(virgin, elastic.TrialState);
        Assert.Equal(0, elastic.PlasticDissipationDensityIncrement);

        var plastic = mat.Evaluate(.002, virgin);
        Close(257.14285714285717, plastic.Stress, 1e-9);
        Close(9523.809523809523, plastic.TangentModulus, 1e-9);
        Close(.0007142857142857143, plastic.TrialState.PlasticStrain, 1e-14);
        Close(plastic.TrialState.PlasticStrain, plastic.TrialState.AccumulatedPlasticStrain, 1e-14);
        Assert.True(plastic.PlasticDissipationDensityIncrement > 0);
        Assert.Equal(new AxialMaterialState(), virgin);

        var unloading = mat.Evaluate(.001, plastic.TrialState);
        Assert.Equal(E, unloading.TangentModulus);
        Assert.Equal(plastic.TrialState, unloading.TrialState);
        Assert.Equal(0, unloading.PlasticDissipationDensityIncrement);
    }

    [Fact]
    public void HorizontalBar_AnalyticalAxialForceAndConsistentTangent4x4()
    {
        var bar = Bar();
        var t = bar.EvaluateTrial(AxialDofs(.004)); // epsilon=0.002; plastic branch
        Close(.002, t.AxialStrain, 1e-14);
        Close(2.5714285714285716, t.AxialForce, 1e-11);
        Close(47.61904761904762, t.TangentStiffness[0, 0], 1e-11);
        Close(-t.AxialForce, t.InternalForces[0], 1e-12);
        Close(t.AxialForce, t.InternalForces[2], 1e-12);
        Assert.Equal(0, t.InternalForces[1]);
        Assert.Equal(0, t.InternalForces[3]);
        Close(-t.TangentStiffness[0, 0], t.TangentStiffness[0, 2], 1e-12);
        for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++)
            Close(t.TangentStiffness[i, j], t.TangentStiffness[j, i], 1e-13);
        Assert.Equal(new AxialMaterialState(), bar.CommittedState);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(90)]
    [InlineData(135)]
    [InlineData(-63)]
    public void InclinedMembers_ProjectionForceAndEnergyAgreeWithAxialReference(double angleDegrees)
    {
        double rad = angleDegrees * Math.PI / 180;
        double c = Math.Cos(rad), s = Math.Sin(rad);
        var bar = Bar(x2: 2 * c, y2: 2 * s);
        var displacements = new[] { 0d, 0d, .004 * c, .004 * s };
        var t = bar.EvaluateTrial(displacements);
        Close(.002, t.AxialStrain, 5e-15);
        Close(2.5714285714285716, t.AxialForce, 2e-11);
        Close(-c * t.AxialForce, t.InternalForces[0], 2e-11);
        Close(-s * t.AxialForce, t.InternalForces[1], 2e-11);
        Close(c * t.AxialForce, t.InternalForces[2], 2e-11);
        Close(s * t.AxialForce, t.InternalForces[3], 2e-11);
        var reference = Bar().EvaluateTrial(AxialDofs(.004));
        Close(reference.RecoverableElasticEnergy, t.RecoverableElasticEnergy, 1e-11);
        Close(reference.PlasticDissipationIncrement, t.PlasticDissipationIncrement, 1e-11);
    }

    [Fact]
    public void ReversingNodeOrder_PreservesPhysicalForcesEnergiesAndStiffness()
    {
        var forward = Bar();
        var reverse = Bar(x1: 2, y1: 0, x2: 0, y2: 0);
        var a = forward.EvaluateTrial(AxialDofs(.004));
        var b = reverse.EvaluateTrial(new[] { .004, 0d, 0d, 0d });
        Close(a.AxialStrain, b.AxialStrain, 1e-14);
        Close(a.AxialForce, b.AxialForce, 1e-12);
        Close(a.InternalForces[2], b.InternalForces[0], 1e-12);
        Close(a.InternalForces[0], b.InternalForces[2], 1e-12);
        Close(a.TangentStiffness[2, 2], b.TangentStiffness[0, 0], 1e-12);
        Close(a.RecoverableElasticEnergy, b.RecoverableElasticEnergy, 1e-13);
        Close(a.PlasticDissipationIncrement, b.PlasticDissipationIncrement, 1e-13);
    }

    [Fact]
    public void RigidTranslationAndInfinitesimalRotation_HaveNoArtificialAxialResistance()
    {
        var bar = Bar(x1: 1, y1: 2, x2: 3, y2: 2);
        var move = bar.EvaluateTrial(new[] { 5d, -3d, 5d, -3d });
        Close(0, move.AxialForce, 1e-12);
        Close(0, move.AxialStrain, 1e-12);
        var rotation = bar.EvaluateTrial(new[] { -.002, .001, -.002, .003 });
        Close(0, rotation.AxialForce, 1e-12);
        Close(0, rotation.AxialStrain, 1e-12);
        // A purely transverse motion carries no LINEAR geometric stiffness.
        var transverse = bar.EvaluateTrial(new[] { 0d, 0d, 0d, .1 });
        Close(0, transverse.AxialForce, 1e-12);
        double[] rigidMode = { 1, 0, 1, 0 };
        for (int i = 0; i < 4; i++)
        {
            double product = 0;
            for (int j = 0; j < 4; j++) product += move.TangentStiffness[i, j] * rigidMode[j];
            Close(0, product, 1e-12);
        }
    }

    [Theory]
    [InlineData(.001, 1e-5)]   // elastic
    [InlineData(.004, 1e-5)]   // plastic
    public void ConsistentTangent_MatchesFiniteDifferenceOfInternalForce(double displacement, double tolerance)
    {
        var bar = Bar(x2: 1.6, y2: 1.2);
        double c = bar.Cosine, s = bar.Sine;
        double[] u = { 0, 0, displacement * c, displacement * s };
        var center = bar.EvaluateTrial(u);
        const double h = 1e-7;
        for (int j = 0; j < 4; j++)
        {
            var plus = (double[])u.Clone();
            var minus = (double[])u.Clone();
            plus[j] += h;
            minus[j] -= h;
            var fp = bar.EvaluateTrial(plus).InternalForces;
            var fm = bar.EvaluateTrial(minus).InternalForces;
            for (int i = 0; i < 4; i++)
                Close(center.TangentStiffness[i, j], (fp[i] - fm[i]) / (2 * h), tolerance);
        }
    }

    [Fact]
    public void ElasticLimit_AgreesWithExistingTruss2DGlobalStiffness()
    {
        var bar = Bar(x2: 1.6, y2: 1.2);
        var t = bar.EvaluateTrial(new[] { 0d, 0d, .001, -.0003 });
        var reference = StructuralSolver2D.Analysis.Truss2D.Truss2DElementMatrices.BuildGlobalStiffness(
            E, A, bar.Length, bar.Cosine, bar.Sine);
        for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++) Close(reference[i, j], t.TangentStiffness[i, j], 1e-11);
        for (int i = 0; i < 4; i++)
        {
            double force = 0;
            double[] u = { 0, 0, .001, -.0003 };
            for (int j = 0; j < 4; j++) force += reference[i, j] * u[j];
            Close(force, t.InternalForces[i], 1e-12);
        }
    }

    [Fact]
    public void M62Spring_ProvidesDimensionallyConvertedIndependentReference()
    {
        var bar = Bar();
        var spring = new BilinearIsotropicSpring(E * A / 2, Fy * A, H * A / 2);
        var springState = new BilinearSpringState();
        foreach (double extension in new[] { .002, .004, .007, .001, -.005, -.010, 0d, .005 })
        {
            var t = bar.EvaluateTrial(AxialDofs(extension));
            var s = spring.Evaluate(extension, springState);
            Close(s.InternalForce, t.AxialForce, 1e-11);
            Close(s.ConsistentTangent, t.TangentStiffness[2, 2], 1e-10);
            Close(s.PlasticDissipationIncrement, t.PlasticDissipationIncrement, 1e-12);
            Close(s.RecoverableElasticEnergy, t.RecoverableElasticEnergy, 1e-12);
            Close(s.HardeningEnergy, t.HardeningEnergy, 1e-12);
            Close(s.TrialState.PlasticDisplacement, 2 * t.TrialState.PlasticStrain, 1e-13);
            bar.Commit(t);
            springState = s.TrialState;
        }
        Assert.True(bar.CommittedState.AccumulatedPlasticStrain > 0);
    }

    [Fact]
    public void MonotoneLoading_ExternalWorkEqualsStoredAndDissipatedEnergy()
    {
        var bar = Bar();
        var t = bar.EvaluateTrial(AxialDofs(.004));
        double eYield = Fy / E;
        double et = E * H / (E + H);
        double eTotal = .004 / 2;
        double work = A * 2 * (.5 * E * eYield * eYield + Fy * (eTotal - eYield) +
            .5 * et * (eTotal - eYield) * (eTotal - eYield));
        Close(work, t.RecoverableElasticEnergy + t.HardeningEnergy + t.PlasticDissipationIncrement, 1e-12);
    }

    [Fact]
    public void Commit_IsExplicit_RepeatTrialIsPure_StaleAndForeignTrialsRejected()
    {
        var a = Bar();
        var b = Bar();
        var t1 = a.EvaluateTrial(AxialDofs(.006));
        var t2 = a.EvaluateTrial(AxialDofs(.006));
        Assert.Equal(new AxialMaterialState(), a.CommittedState);
        Assert.Equal(t1.TrialState, t2.TrialState);
        Assert.Throws<InvalidOperationException>(() => b.Commit(t1));
        Assert.Equal(new AxialMaterialState(), b.CommittedState);
        a.Commit(t1);
        Assert.Equal(t1.TrialState, a.CommittedState);
        Assert.Throws<InvalidOperationException>(() => a.Commit(t1));
        Assert.Throws<InvalidOperationException>(() => a.Commit(t2));
        Assert.Equal(t1.TrialState, a.CommittedState);
        var unloaded = a.EvaluateTrial(AxialDofs(.001));
        Assert.Equal(t1.TrialState, a.CommittedState);
        a.Commit(unloaded);
        Assert.Equal(t1.TrialState.AccumulatedPlasticStrain, a.CommittedState.AccumulatedPlasticStrain);
    }

    [Fact]
    public void MonotoneLoading_IsIndependentOfIntermediateCommittedSubsteps()
    {
        static ElastoplasticTruss2DTrial Solve(int increments)
        {
            var bar = Bar();
            ElastoplasticTruss2DTrial? final = null;
            for (int i = 1; i <= increments; i++)
            {
                final = bar.EvaluateTrial(AxialDofs(.008 * i / increments));
                bar.Commit(final);
            }
            return final!;
        }
        var single = Solve(1);
        var stepped = Solve(64);
        Close(single.AxialForce, stepped.AxialForce, 1e-11);
        Close(single.TrialState.PlasticStrain, stepped.TrialState.PlasticStrain, 1e-13);
        Close(single.TrialState.AccumulatedPlasticStrain, stepped.TrialState.AccumulatedPlasticStrain, 1e-13);
        // Incremental dissipations cannot be compared via only the FINAL trial;
        // the path's cumulative sum is required for any cumulative-energy output.
    }

    [Fact]
    public void ModelTranslation_LeavesLocalStrainAndGlobalTangentUnchanged()
    {
        var a = Bar(x1: 1, y1: -3, x2: 2.6, y2: -1.8);
        var b = Bar(x1: 1001, y1: -3003, x2: 1002.6, y2: -3001.8);
        double[] u = { 0.001, -0.004, 0.004, -0.001 };
        var x = a.EvaluateTrial(u);
        var y = b.EvaluateTrial(u);
        Close(x.AxialForce, y.AxialForce, 5e-10);
        for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++) Close(x.TangentStiffness[i, j], y.TangentStiffness[i, j], 2e-9);
    }

    [Fact]
    public void PerfectPlasticity_HasZeroTangentialStiffnessButNonzeroDissipation()
    {
        var b = Bar(hardening: 0);
        var t = b.EvaluateTrial(AxialDofs(.02));
        Close(A * Fy, t.AxialForce, 1e-12);
        Close(0, t.TangentStiffness[0, 0], 1e-12);
        Assert.True(t.PlasticDissipationIncrement > 0);
        Assert.Equal(new AxialMaterialState(), b.CommittedState);
    }

    [Fact]
    public void InvalidGeometryMaterialStateAndNonfiniteTrial_DoNotChangeCommittedState()
    {
        Assert.Throws<ArgumentException>(() => Bar(x2: 0));
        Assert.Throws<ArgumentException>(() => Bar(x2: double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => Bar(x2: 1e250));
        Assert.Throws<ArgumentException>(() => new ElastoplasticTruss2DElement(0, 0, 1, 0, 0, E, Fy, H));
        Assert.Throws<ArgumentException>(() => new ElastoplasticTruss2DElement(0, 0, 1, 0, A, 0, Fy, H));
        Assert.Throws<ArgumentException>(() => new BilinearIsotropicAxialMaterial(E, Fy, -1));
        var mat = new BilinearIsotropicAxialMaterial(E, Fy, H);
        Assert.Throws<ArgumentException>(() => mat.Evaluate(0, new AxialMaterialState(.01, 0)));
        Assert.Throws<ArgumentException>(() => mat.Evaluate(double.NaN, default));
        var bar = Bar();
        Assert.Throws<ArgumentException>(() => bar.EvaluateTrial(new double[] { 0, 0, 0 }));
        Assert.Throws<ArgumentException>(() => bar.EvaluateTrial(new[] { 0d, 0d, double.PositiveInfinity, 0d }));
        Assert.Throws<ArgumentException>(() => bar.EvaluateTrial(new[] { 0d, 0d, 1e308, 0d }));
        Assert.Equal(new AxialMaterialState(), bar.CommittedState);
    }

    private static void Close(double expected, double observed, double tolerance) =>
        Assert.True(double.IsFinite(observed) && Math.Abs(expected - observed) <= tolerance,
            $"Expected {expected:R}, got {observed:R} (tolerance {tolerance:R}).");
}
