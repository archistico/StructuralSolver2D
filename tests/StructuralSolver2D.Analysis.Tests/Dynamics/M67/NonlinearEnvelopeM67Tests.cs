using StructuralSolver2D.Analysis.Truss2D.Nonlinear;

namespace StructuralSolver2D.Analysis.Tests.Dynamics.M67;

public sealed class NonlinearEnvelopeM67Tests
{
    private static NonlinearTruss2DTimeRequest Example(IReadOnlyList<double>? factors = null, int iterations = 30) =>
        new(new[] { new IncrementalTrussNode("A",0,0), new IncrementalTrussNode("B",2,0) },
            new[] { new NonlinearDynamicTrussMember("AB","A","B",.01,200_000,250,10_000,1.5) },
            new[] { new IncrementalTrussSupport("A",1,0), new IncrementalTrussSupport("A",0,1),
                new IncrementalTrussSupport("B",0,1) },
            new[] { new IncrementalTrussLoad("B",4,0) },
            factors ?? new[]{0d,1d,0d,-1d,0d}, .1, MaximumNewtonIterations:iterations);

    [Fact]
    public void AcceptedNonlinearSteps_RecoverAxialAndSupportReactionPeaksWithoutElasticApproximation()
    {
        var request = Example();
        NonlinearTruss2DTimeResult solve = NonlinearTruss2DNewmark.Solve(request);
        Assert.True(solve.Completed, solve.Failure);
        var env = NonlinearTruss2DForceEnvelopesM67.FromAcceptedSteps(request,solve);
        var member = Assert.Single(env.Members);
        Assert.Equal("AB",member.MemberId);
        Assert.True(member.AxialForce.Magnitude>0);
        Assert.True(member.AccumulatedPlasticStrain.Magnitude>0);
        Assert.Equal(solve.Steps.Max(s=>Math.Abs(s.AxialForces[0])),member.AxialForce.Magnitude,10);
        Assert.Equal(solve.Steps.Max(s=>s.MaterialStates[0].AccumulatedPlasticStrain),
            member.AccumulatedPlasticStrain.Magnitude,10);
        Assert.Equal(solve.Steps.Max(s=>s.CumulativePlasticDissipation),
            env.CumulativePlasticDissipation.Magnitude,10);
        Assert.Equal(2,env.NodeReactions.Count);
        Assert.True(env.IncludesOnlyAcceptedSteps);
    }

    [Fact]
    public void FailedStep_DoesNotContributeToEnvelopesOrPlasticDissipation()
    {
        var request = Example(new[] {0d,1d}, iterations:1);
        var solve=NonlinearTruss2DNewmark.Solve(request);
        Assert.False(solve.Completed);
        Assert.Single(solve.Steps);
        var envelope=NonlinearTruss2DForceEnvelopesM67.FromAcceptedSteps(request,solve);
        Assert.Equal(0, envelope.Members[0].AxialForce.Magnitude,12);
        Assert.Equal(0, envelope.CumulativePlasticDissipation.Magnitude,12);
    }
}
