namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// Locates benchmark files from test execution directories.
/// </summary>
internal static class BenchmarkRepository
{
    public static string FindRoot()
    {
        DirectoryInfo? directory = new(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "StructuralSolver2D.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "benchmarks")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root containing StructuralSolver2D.sln and benchmarks/.");
    }

    public static string ExpectedResultsPath(string repositoryRoot) =>
        Path.Combine(repositoryRoot, "benchmarks", "expected", "expected-results.json");

    public static string ResolveModelPath(string repositoryRoot, BenchmarkCase benchmark) =>
        Path.Combine(repositoryRoot, benchmark.ModelPath.Replace('/', Path.DirectorySeparatorChar));
}
