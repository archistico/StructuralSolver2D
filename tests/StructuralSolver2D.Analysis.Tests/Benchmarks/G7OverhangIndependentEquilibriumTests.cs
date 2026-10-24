namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// G7 independent free-body equilibrium on the overhang from the G5-L3 reading.
/// The book inspires the geometry/pattern choice; q=5 kN/m, 6+2 m and
/// the values below are independently specified synthetic test data.
/// Both supports are BILATERAL. A negative reaction is not evidence that
/// an unanchored physical support can transmit uplift.
/// </summary>
public sealed class G7OverhangIndependentEquilibriumTests
{
    [Theory]
    [InlineData("B13", -5.0 / 3.0, 35.0 / 3.0, 10.0)]
    [InlineData("B14", 40.0 / 3.0, 80.0 / 3.0, 40.0)]
    public void ModelledOverhangPreservesIndependentReactionSignsAndMoments(
        string id,double expectedRA,double expectedRB,double totalDownward)
    {
        var repository=BenchmarkRepository.FindRoot();
        var catalog=BenchmarkCatalog.Load(BenchmarkRepository.ExpectedResultsPath(repository));
        var benchmark=Assert.Single(catalog.Benchmarks, b=>b.Id==id);
        var (_,result)=BenchmarkAnalysisRunner.Run(repository,benchmark);
        var a=Assert.Single(result.Reactions,r=>r.NodeId=="A");
        var b=Assert.Single(result.Reactions,r=>r.NodeId=="B");
        Assert.Equal(expectedRA,a.Fy,6);
        Assert.Equal(expectedRB,b.Fy,6);
        Assert.Equal(totalDownward,a.Fy+b.Fy,6);
        // The overhang resultant acts at x=7m. AB (when loaded) at x=3m.
        var momentFromLoads=5*2*7+(id=="B14" ? 5*6*3 : 0);
        Assert.Equal(momentFromLoads,6*b.Fy,6);
        if (id=="B13") Assert.True(a.Fy<0,
            "Hinge A experiences UPLIFT in the bilaterally restrained ideal model.");
    }

    [Fact]
    public void AddingMainSpanLoadChangesBothReactionsByExactlyFifteenKilonewtons()
    {
        var repository=BenchmarkRepository.FindRoot();
        var catalog=BenchmarkCatalog.Load(BenchmarkRepository.ExpectedResultsPath(repository));
        double[] Reactions(string id)
        {
            var b=Assert.Single(catalog.Benchmarks,c=>c.Id==id);
            var (_,r)=BenchmarkAnalysisRunner.Run(repository,b);
            return new[]{
                Assert.Single(r.Reactions,x=>x.NodeId=="A").Fy,
                Assert.Single(r.Reactions,x=>x.NodeId=="B").Fy};
        }
        var uplift=Reactions("B13");
        var combined=Reactions("B14");
        Assert.Equal(15,combined[0]-uplift[0],6);
        Assert.Equal(15,combined[1]-uplift[1],6);
    }
}
