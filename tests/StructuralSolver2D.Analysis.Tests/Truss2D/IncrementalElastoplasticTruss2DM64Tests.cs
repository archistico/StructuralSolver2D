using StructuralSolver2D.Analysis.Truss2D.Nonlinear;

namespace StructuralSolver2D.Analysis.Tests.Truss2D;

public sealed class IncrementalElastoplasticTruss2DM64Tests
{
    private const double E = 200_000;
    private const double A = .01;
    private const double H = 10_000;
    private const double Fy = 250;

    private static IncrementalTrussRequest SingleBar(
        double hardening = H, double referenceForce = 3,
        IReadOnlyList<double>? factors = null,
        int iterations = 35, double absoluteTolerance = 1e-9,
        bool duplicateSupport = false)
    {
        var supports = new List<IncrementalTrussSupport>
        {
            new("A", 1, 0), new("A", 0, 1), new("B", 0, 1),
        };
        if (duplicateSupport) supports.Add(new("B", 0, -3));
        return new IncrementalTrussRequest(
            new[] { new IncrementalTrussNode("A", 0, 0), new IncrementalTrussNode("B", 2, 0) },
            new[] { new IncrementalTrussMember("AB", "A", "B", A, E, Fy, hardening) },
            supports,
            new[] { new IncrementalTrussLoad("B", referenceForce, 0) },
            factors ?? new[] { .5, 1d }, iterations, absoluteTolerance);
    }

    [Fact]
    public void OneBar_ElasticAndPlasticBranchesMatchClosedFormAndReactions()
    {
        var result = IncrementalElastoplasticTruss2DSolver.Solve(SingleBar());
        Assert.True(result.Completed, result.Failure);
        Assert.Equal(2, result.Steps.Count);
        var elastic = result.Steps[0];
        Close(.0015, elastic.Displacements[2], 1e-11);
        Close(1.5, elastic.AxialForces[0], 1e-9);
        Assert.Equal(new AxialMaterialState(), elastic.MaterialStates[0]);
        var plastic = result.Steps[1];
        // k=EA/L=1000, N_y=2.5, k_t=EA/L * H/(E+H)=47.619...
        double expected = .0025 + (3 - 2.5) / (E * A / 2 * H / (E + H));
        Close(expected, plastic.Displacements[2], 1e-10);
        Close(3, plastic.AxialForces[0], 1e-9);
        Close(-3, plastic.Reactions[0], 1e-9);
        Close(0, plastic.Reactions[2], 1e-9);
        Assert.True(plastic.MaterialStates[0].AccumulatedPlasticStrain > 0);
        Assert.True(plastic.CumulativePlasticDissipation > 0);
        Close(plastic.MaterialStates[0].PlasticStrain,
            result.FinalStates[0].PlasticStrain, 1e-14);
    }

    [Fact]
    public void TwoParallelBars_LoadRedistributesAfterFirstYields()
    {
        var request = new IncrementalTrussRequest(
            new[] { new IncrementalTrussNode("A", 0, 0), new IncrementalTrussNode("B", 2, 0) },
            new[]
            {
                new IncrementalTrussMember("soft", "A", "B", A, E, 250, H),
                new IncrementalTrussMember("strong", "A", "B", A, E, 500, H),
            },
            new[] { new IncrementalTrussSupport("A", 1, 0), new IncrementalTrussSupport("A", 0, 1),
                new IncrementalTrussSupport("B", 0, 1) },
            new[] { new IncrementalTrussLoad("B", 6, 0) },
            new[] { .5, 1d });
        var result = IncrementalElastoplasticTruss2DSolver.Solve(request);
        Assert.True(result.Completed, result.Failure);
        Close(.0015, result.Steps[0].Displacements[2], 1e-12); // 3/(2*1000)
        var step = result.Steps[^1];
        double tangent = E * A / 2 * H / (E + H);
        double expected = (6 - 2.5 + tangent * .0025) / (1000 + tangent);
        Close(expected, step.Displacements[2], 1e-11);
        Close(6, step.AxialForces.Sum(), 1e-9);
        Assert.True(step.MaterialStates[0].AccumulatedPlasticStrain > 0);
        Assert.Equal(new AxialMaterialState(), step.MaterialStates[1]);
        Close(-6, step.Reactions[0], 1e-9);
    }

    [Fact]
    public void Triangle_ElasticGlobalEquilibriumAndMemberForceBalance()
    {
        var nodes = new[]
        {
            new IncrementalTrussNode("A", 0, 0),
            new IncrementalTrussNode("B", 2, 0),
            new IncrementalTrussNode("C", 1, 1),
        };
        var members = new[]
        {
            new IncrementalTrussMember("AB", "A", "B", A, E, Fy, H),
            new IncrementalTrussMember("AC", "A", "C", A, E, Fy, H),
            new IncrementalTrussMember("BC", "B", "C", A, E, Fy, H),
        };
        var request = new IncrementalTrussRequest(nodes, members,
            new[] { new IncrementalTrussSupport("A", 1, 0), new IncrementalTrussSupport("A", 0, 1),
                new IncrementalTrussSupport("B", 0, 1) },
            new[] { new IncrementalTrussLoad("C", 0, -1) }, new[] { .5, 1d });
        var result = IncrementalElastoplasticTruss2DSolver.Solve(request);
        Assert.True(result.Completed, result.Failure);
        var step = result.Steps[^1];
        Assert.True(step.ResidualNorm < 1e-8);
        Close(-1 / Math.Sqrt(2), step.AxialForces[1], 1e-8);
        Close(-1 / Math.Sqrt(2), step.AxialForces[2], 1e-8);
        Close(.5, step.AxialForces[0], 1e-8);
        Close(1, step.Reactions[1] + step.Reactions[3], 1e-8);
        Close(0, step.Reactions[0], 1e-8);
        foreach (var state in step.MaterialStates) Assert.Equal(new AxialMaterialState(), state);
    }

    [Fact]
    public void InclinedAndRedundantSupports_AreAppliedWithoutPenaltyStiffness()
    {
        // One oblique bar at 45 degrees; A fixed, B constrained transversely to the member.
        double q = 1 / Math.Sqrt(2);
        var request = new IncrementalTrussRequest(
            new[] { new IncrementalTrussNode("A", 0, 0), new IncrementalTrussNode("B", q * 2, q * 2) },
            new[] { new IncrementalTrussMember("AB", "A", "B", A, E, Fy, H) },
            new[]
            {
                new IncrementalTrussSupport("A", 1, 0), new IncrementalTrussSupport("A", 0, 1),
                new IncrementalTrussSupport("B", -q, q), new IncrementalTrussSupport("B", -2*q, 2*q),
            },
            new[] { new IncrementalTrussLoad("B", 3*q, 3*q) }, new[] { 1d });
        var result = IncrementalElastoplasticTruss2DSolver.Solve(request);
        Assert.True(result.Completed, result.Failure);
        var step = result.Steps[0];
        double axialExtension = q * step.Displacements[2] + q * step.Displacements[3];
        Close(.013, axialExtension, 1e-10);
        Close(3, step.AxialForces[0], 1e-9);
        Close(0, -q * step.Displacements[2] + q * step.Displacements[3], 1e-11);
    }

    [Fact]
    public void RepeatedLoadFactors_AndUnload_DoNotDoubleCommitPlasticity()
    {
        var result = IncrementalElastoplasticTruss2DSolver.Solve(SingleBar(factors: new[] { 1d, 1d, 0d, -1d }));
        Assert.True(result.Completed, result.Failure);
        Close(result.Steps[0].CumulativePlasticDissipation,
            result.Steps[1].CumulativePlasticDissipation, 1e-12);
        Close(0, result.Steps[1].IncrementalPlasticDissipation, 1e-12);
        Close(3, result.Steps[0].AxialForces[0], 1e-9);
        Close(-3, result.Steps[^1].AxialForces[0], 1e-9);
        Assert.True(result.FinalStates[0].AccumulatedPlasticStrain > 0);
    }

    [Fact]
    public void PerfectPlasticity_OverYield_ReportsSingularityAndRollsBackEntireStep()
    {
        var result = IncrementalElastoplasticTruss2DSolver.Solve(
            SingleBar(hardening: 0, factors: new[] { .5, 1d }));
        Assert.False(result.Completed);
        Assert.Contains("M64_SINGULAR", result.Failure);
        Assert.Single(result.Steps);
        Close(1.5, result.Steps[0].AxialForces[0], 1e-12);
        Assert.Equal(new AxialMaterialState(), result.FinalStates[0]);
    }

    [Fact]
    public void FailedMultiMemberIncrement_PreservesEveryPreviousCommittedState()
    {
        var request = new IncrementalTrussRequest(
            new[] { new IncrementalTrussNode("A", 0, 0), new IncrementalTrussNode("B", 2, 0) },
            new[]
            {
                new IncrementalTrussMember("first", "A", "B", A, E, 250, 0),
                new IncrementalTrussMember("second", "A", "B", A, E, 500, 0),
            },
            new[] { new IncrementalTrussSupport("A", 1, 0), new IncrementalTrussSupport("A", 0, 1),
                new IncrementalTrussSupport("B", 0, 1) },
            new[] { new IncrementalTrussLoad("B", 10, 0) }, new[] { .6, 1d });
        var result = IncrementalElastoplasticTruss2DSolver.Solve(request);
        Assert.False(result.Completed);
        Assert.Contains("M64_SINGULAR", result.Failure);
        Assert.Single(result.Steps);
        Assert.True(result.FinalStates[0].AccumulatedPlasticStrain > 0);
        Assert.Equal(new AxialMaterialState(), result.FinalStates[1]);
        Assert.Equal(result.Steps[0].MaterialStates[0], result.FinalStates[0]);
        Assert.Equal(result.Steps[0].MaterialStates[1], result.FinalStates[1]);
        Close(6, result.Steps[0].AxialForces.Sum(), 1e-9);
    }

    [Fact]
    public void IterationLimit_RollsBackStepWithoutLeakingCommittedPlasticity()
    {
        // One Newton update cannot correct the reduced tangent after yielding.
        var result = IncrementalElastoplasticTruss2DSolver.Solve(
            SingleBar(factors: new[] { .5, 1d }, iterations: 1));
        Assert.False(result.Completed);
        Assert.Contains("M64_NONCONVERGENCE", result.Failure);
        Assert.Single(result.Steps);
        Assert.Equal(new AxialMaterialState(), result.FinalStates[0]);
        Close(.0015, result.Steps[0].Displacements[2], 1e-12);
    }

    [Fact]
    public void MechanismAndInsufficientConstraints_AreExplicitFailures()
    {
        var original = SingleBar(factors: new[] { 1d });
        var freeTransverse = original with
        {
            Supports = new[] { new IncrementalTrussSupport("A", 1, 0),
                new IncrementalTrussSupport("A", 0, 1) },
        };
        var result = IncrementalElastoplasticTruss2DSolver.Solve(freeTransverse);
        Assert.False(result.Completed);
        Assert.Contains("M64_SINGULAR", result.Failure);
    }

    [Fact]
    public void ZeroLoadDoesNotMakeAnUnrestrainedMechanismAppearStable()
    {
        var request = SingleBar(factors: new[] { 0d }) with
        {
            Supports = new[] { new IncrementalTrussSupport("A", 1, 0),
                new IncrementalTrussSupport("A", 0, 1) },
        };
        var result = IncrementalElastoplasticTruss2DSolver.Solve(request);
        Assert.False(result.Completed);
        Assert.Contains("M64_SINGULAR", result.Failure);
        Assert.Empty(result.Steps);
    }

    [Fact]
    public void InvalidInputs_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => IncrementalElastoplasticTruss2DSolver.Solve(
            SingleBar() with { LoadFactors = new[] { double.NaN } }));
        Assert.Throws<ArgumentException>(() => IncrementalElastoplasticTruss2DSolver.Solve(
            SingleBar() with { Supports = new[] { new IncrementalTrussSupport("A", 0, 0) } }));
        Assert.Throws<ArgumentException>(() => IncrementalElastoplasticTruss2DSolver.Solve(
            SingleBar() with { Members = new[] { new IncrementalTrussMember("bad", "A", "X", A, E, Fy, H) } }));
        Assert.Throws<ArgumentException>(() => IncrementalElastoplasticTruss2DSolver.Solve(
            SingleBar() with { Nodes = new[] { new IncrementalTrussNode("A", 0, 0),
                new IncrementalTrussNode("a", 2, 0) } }));
    }

    private static void Close(double expected, double actual, double tolerance) =>
        Assert.True(Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected:G17}, observed {actual:G17}, tolerance {tolerance:G6}.");
}
