namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// Runs the external benchmark catalog in benchmarks/expected/expected-results.json.
/// These tests intentionally load the same JSON models used by the CLI, so the benchmark
/// suite validates both the solver and the public example data files.
/// </summary>
public sealed class BenchmarkCatalogTests
{
    [Fact]
    public void BenchmarkCatalog_ShouldBeWellFormed()
    {
        string repositoryRoot = BenchmarkRepository.FindRoot();
        BenchmarkCatalog catalog = BenchmarkCatalog.Load(BenchmarkRepository.ExpectedResultsPath(repositoryRoot));

        IReadOnlyList<string> issues = catalog.Validate(repositoryRoot);

        Assert.True(
            issues.Count == 0,
            "The benchmark catalog contains structural issues:" + Environment.NewLine + string.Join(Environment.NewLine, issues));
    }

    [Fact]
    public void BenchmarkCatalog_ShouldMatchExpectedResults()
    {
        string repositoryRoot = BenchmarkRepository.FindRoot();
        BenchmarkCatalog catalog = BenchmarkCatalog.Load(BenchmarkRepository.ExpectedResultsPath(repositoryRoot));

        Assert.NotEmpty(catalog.Benchmarks);

        foreach (BenchmarkCase benchmark in catalog.Benchmarks)
        {
            var (model, result) = BenchmarkAnalysisRunner.Run(repositoryRoot, benchmark);
            BenchmarkResultAssertions.AssertBenchmark(model, result, benchmark);
        }
    }
}
