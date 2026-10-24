using StructuralSolver2D.Analysis.PublicApi;

namespace StructuralSolver2D.Analysis.Tests.Truss2D;

public sealed class NonlinearPublicApiM68Tests
{
    private static NonlinearTruss2DModel Model(double? mass = 1) => new(
        new[] { new NonlinearTruss2DNode("A", 0, 0), new NonlinearTruss2DNode("B", 1, 0) },
        new[] { new NonlinearTruss2DMember("AB", "A", "B", 1, 1000, 5, 100, mass) },
        new[] { new NonlinearTruss2DSupport("A", 1, 0), new NonlinearTruss2DSupport("A", 0, 1),
                new NonlinearTruss2DSupport("B", 0, 1) },
        new[] { new NonlinearTruss2DLoad("B", 2, 0) });

    [Fact]
    public void PublicStatic_ElasticAndPlastic_StepsAndStatesHaveCorrectMapping()
    {
        var service = new StructuralSolver2DService();
        var result = service.AnalyzeNonlinearTrussStatic(new StructuralNonlinearStaticRequest(Model(), new[] { 1.0, 4.0 }));
        Assert.True(result.Completed, result.Failure);
        Assert.Equal(new[] { "A", "B" }, result.NodeIds);
        Assert.Equal(new[] { "AB" }, result.MemberIds);
        Assert.Equal(2, result.Steps.Count);
        Assert.Equal(.002, result.Steps[0].Displacements[2], 9);
        Assert.Equal(2, result.Steps[0].AxialForces[0], 9);
        Assert.True(result.Steps[1].MaterialStates[0].AccumulatedPlasticStrain > 0);
        Assert.True(result.Steps[1].CumulativePlasticDissipation > 0);
        Assert.Equal(result.Steps[1].MaterialStates[0], result.FinalStates[0]);
    }

    [Fact]
    public void PublicTime_ReportsPhysicalHistoriesAndZeroState()
    {
        var service = new StructuralSolver2DService();
        var result = service.AnalyzeNonlinearTrussTime(new StructuralNonlinearTimeRequest(
            Model(), new[] { 0.0, 1.0, 0.0, 0.0 }, .01));
        Assert.True(result.Completed, result.Failure);
        Assert.Equal(4, result.Steps.Count);
        Assert.Equal(0, result.Steps[0].Time);
        Assert.Equal(.03, result.Steps[3].Time, 11);
        Assert.Equal(4, result.Steps[1].Velocities.Count);
        Assert.Single(result.Steps[1].AxialForces);
        Assert.All(result.Steps, s => Assert.All(s.Displacements, x => Assert.True(double.IsFinite(x))));
    }

    [Fact]
    public void PublicTime_RequiresPhysicalMassAndDoesNotInventIt()
    {
        var service = new StructuralSolver2DService();
        var ex = Assert.Throws<ArgumentException>(() => service.AnalyzeNonlinearTrussTime(
            new StructuralNonlinearTimeRequest(Model(null), new[] { 0.0, 1.0 }, .01)));
        Assert.Contains("M68_MASS", ex.Message);
    }

    [Fact]
    public void StaticOutput_ReportsFailureAndDoesNotCommitRejectedFactor()
    {
        var service = new StructuralSolver2DService();
        var output = service.AnalyzeNonlinearTrussStatic(new StructuralNonlinearStaticRequest(
            Model(), new[] { 1.0, 4.0 }, MaxIterations: 1));
        Assert.False(output.Completed);
        Assert.NotNull(output.Failure);
        Assert.Single(output.Steps);
        Assert.Equal(0, output.FinalStates[0].AccumulatedPlasticStrain, 12);
    }

    [Fact]
    public void EmptyOrMissingContracts_AreRejectedWithActionableError()
    {
        var api = new StructuralNonlinearTruss2DService();
        Assert.Throws<ArgumentNullException>(() => api.AnalyzeStatic(null!));
        var error = Assert.Throws<ArgumentException>(() => api.AnalyzeStatic(
            new StructuralNonlinearStaticRequest(new NonlinearTruss2DModel(null!, null!, null!, null!),
                new[] { 1.0 })));
        Assert.Contains("M68_MODEL", error.Message);
    }
}
