using System.Text.Json;
using StructuralSolver2D.Analysis.Equilibrium;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// Runs the external benchmark catalog in benchmarks/expected/expected-results.json.
/// These tests intentionally load the same JSON models used by the CLI, so the benchmark
/// suite validates both the solver and the public example data files.
/// </summary>
public sealed class BenchmarkCatalogTests
{
    private const int DiagramSampleCount = 101;

    [Fact]
    public void BenchmarkCatalog_ShouldMatchExpectedResults()
    {
        string repositoryRoot = FindRepositoryRoot();
        string expectedFilePath = Path.Combine(repositoryRoot, "benchmarks", "expected", "expected-results.json");
        BenchmarkCatalog catalog = BenchmarkCatalog.Load(expectedFilePath);

        Assert.NotEmpty(catalog.Benchmarks);

        foreach (BenchmarkCase benchmark in catalog.Benchmarks)
        {
            RunBenchmark(repositoryRoot, benchmark);
        }
    }

    private static void RunBenchmark(string repositoryRoot, BenchmarkCase benchmark)
    {
        string modelPath = Path.Combine(repositoryRoot, benchmark.ModelPath.Replace('/', Path.DirectorySeparatorChar));
        StructuralModelJsonFile modelFile = StructuralModelJsonReader.Read(modelPath);
        StructuralModel model = modelFile.Model;
        StructuralAnalysisResult result = Analyze(model, benchmark.AnalysisId);
        BenchmarkExpected expected = benchmark.Expected;
        double tolerance = benchmark.Tolerance;

        AssertSupportReactions(benchmark, result, expected.SupportReactions, tolerance);
        AssertMemberAxialForces(benchmark, result, expected.MemberAxialForces, tolerance);
        AssertFrameExtremaIfRequested(benchmark, model, result, expected, tolerance);
        AssertDisplacementsIfRequested(benchmark, model, result, expected, tolerance);
        AssertNamedChecksIfRequested(benchmark, result, expected.Checks, tolerance);
        AssertGlobalEquilibrium(benchmark, model, result, tolerance);
    }

    private static StructuralAnalysisResult Analyze(StructuralModel model, string analysisId)
    {
        bool isTrussOnly = model.Members.All(member => member.Type == MemberType.Truss2D);
        bool isFrameOnly = model.Members.All(member => member.Type == MemberType.Frame2D);
        bool isCombination = model.LoadCombinations.Any(combination =>
            string.Equals(combination.Id, analysisId, StringComparison.OrdinalIgnoreCase));

        if (isTrussOnly)
        {
            Truss2DAnalyzer analyzer = new();
            return isCombination ? analyzer.AnalyzeCombination(model, analysisId) : analyzer.Analyze(model, analysisId);
        }

        if (isFrameOnly)
        {
            Frame2DAnalyzer frameAnalyzer = new();
            return isCombination ? frameAnalyzer.AnalyzeCombination(model, analysisId) : frameAnalyzer.Analyze(model, analysisId);
        }

        PlaneStructureAnalyzer planeAnalyzer = new();
        return isCombination ? planeAnalyzer.AnalyzeCombination(model, analysisId) : planeAnalyzer.Analyze(model, analysisId);
    }

    private static void AssertSupportReactions(
        BenchmarkCase benchmark,
        StructuralAnalysisResult result,
        IReadOnlyList<ExpectedSupportReaction> expectedReactions,
        double tolerance)
    {
        foreach (ExpectedSupportReaction expected in expectedReactions)
        {
            SupportReactionResult reaction = result.Reactions.Single(reaction =>
                string.Equals(reaction.NodeId, expected.NodeId, StringComparison.OrdinalIgnoreCase));

            if (expected.Fx.HasValue)
            {
                AssertClose(benchmark.Id, $"reaction Fx at node {expected.NodeId}", expected.Fx.Value, reaction.Fx, tolerance);
            }

            if (expected.Fy.HasValue)
            {
                AssertClose(benchmark.Id, $"reaction Fy at node {expected.NodeId}", expected.Fy.Value, reaction.Fy, tolerance);
            }

            if (expected.Mz.HasValue)
            {
                AssertClose(benchmark.Id, $"reaction Mz at node {expected.NodeId}", expected.Mz.Value, reaction.Mz, tolerance);
            }
        }
    }

    private static void AssertMemberAxialForces(
        BenchmarkCase benchmark,
        StructuralAnalysisResult result,
        IReadOnlyList<ExpectedMemberAxialForce> expectedMemberAxialForces,
        double tolerance)
    {
        foreach (ExpectedMemberAxialForce expected in expectedMemberAxialForces)
        {
            MemberEndForceResult force = result.MemberEndForces.Single(force =>
                string.Equals(force.MemberId, expected.MemberId, StringComparison.OrdinalIgnoreCase));

            AssertClose(benchmark.Id, $"normal force in member {expected.MemberId}", expected.NormalForce, force.EndAxial, tolerance);
        }
    }

    private static void AssertFrameExtremaIfRequested(
        BenchmarkCase benchmark,
        StructuralModel model,
        StructuralAnalysisResult result,
        BenchmarkExpected expected,
        double tolerance)
    {
        if (!expected.MaxAbsShear.HasValue && !expected.MaxAbsMoment.HasValue)
        {
            return;
        }

        Frame2DInternalForceSampler sampler = new();
        IReadOnlyList<MemberInternalForceDiagram> diagrams = sampler.SampleAllMembers(model, result, DiagramSampleCount);
        StructuralAnalysisSummary summary = new Frame2DResultSummarizer().Summarize(result, diagrams);

        if (expected.MaxAbsShear.HasValue)
        {
            AssertClose(benchmark.Id, "maximum absolute shear", expected.MaxAbsShear.Value, Math.Abs(summary.MaxAbsShearForce.Value), tolerance);
        }

        if (expected.MaxAbsMoment.HasValue)
        {
            AssertClose(benchmark.Id, "maximum absolute bending moment", expected.MaxAbsMoment.Value, Math.Abs(summary.MaxAbsBendingMoment.Value), tolerance);
        }
    }

    private static void AssertDisplacementsIfRequested(
        BenchmarkCase benchmark,
        StructuralModel model,
        StructuralAnalysisResult result,
        BenchmarkExpected expected,
        double tolerance)
    {
        foreach (ExpectedNodalDisplacement expectedDisplacement in expected.NodalDisplacements)
        {
            NodalDisplacementResult actual = result.GetDisplacement(expectedDisplacement.NodeId);

            if (expectedDisplacement.Ux.HasValue)
            {
                AssertClose(benchmark.Id, $"nodal Ux at node {expectedDisplacement.NodeId}", expectedDisplacement.Ux.Value, actual.Ux, tolerance);
            }

            if (expectedDisplacement.Uy.HasValue)
            {
                AssertClose(benchmark.Id, $"nodal Uy at node {expectedDisplacement.NodeId}", expectedDisplacement.Uy.Value, actual.Uy, tolerance);
            }

            if (expectedDisplacement.Rz.HasValue)
            {
                AssertClose(benchmark.Id, $"nodal Rz at node {expectedDisplacement.NodeId}", expectedDisplacement.Rz.Value, actual.Rz, tolerance);
            }
        }

        if (expected.TipDisplacementY.HasValue)
        {
            NodalDisplacementResult tip = result.GetDisplacement("B");
            AssertClose(benchmark.Id, "tip vertical displacement", expected.TipDisplacementY.Value, tip.Uy, tolerance);
        }

        if (expected.TipRotationZ.HasValue)
        {
            NodalDisplacementResult tip = result.GetDisplacement("B");
            AssertClose(benchmark.Id, "tip rotation", expected.TipRotationZ.Value, tip.Rz, tolerance);
        }
    }

    private static void AssertNamedChecksIfRequested(
        BenchmarkCase benchmark,
        StructuralAnalysisResult result,
        IReadOnlyList<string> checks,
        double tolerance)
    {
        foreach (string check in checks)
        {
            if (check.Contains("vertical equilibrium", StringComparison.OrdinalIgnoreCase))
            {
                double totalFy = result.Reactions.Sum(reaction => reaction.Fy);
                AssertClose(benchmark.Id, check, 60.0, totalFy, tolerance);
            }
            else if (check.Contains("RAy = RBy", StringComparison.OrdinalIgnoreCase))
            {
                double left = result.Reactions.Single(reaction => string.Equals(reaction.NodeId, "A", StringComparison.OrdinalIgnoreCase)).Fy;
                double right = result.Reactions.Single(reaction => string.Equals(reaction.NodeId, "B", StringComparison.OrdinalIgnoreCase)).Fy;
                AssertClose(benchmark.Id, check, left, right, tolerance);
            }
            else if (check.Contains("horizontal reactions equal and opposite", StringComparison.OrdinalIgnoreCase))
            {
                double left = result.Reactions.Single(reaction => string.Equals(reaction.NodeId, "A", StringComparison.OrdinalIgnoreCase)).Fx;
                double right = result.Reactions.Single(reaction => string.Equals(reaction.NodeId, "B", StringComparison.OrdinalIgnoreCase)).Fx;
                AssertClose(benchmark.Id, check, 0.0, left + right, tolerance);
            }
            else if (check.Contains("stable solution", StringComparison.OrdinalIgnoreCase))
            {
                Assert.NotEmpty(result.Displacements);
            }
        }
    }


    private static void AssertGlobalEquilibrium(
        BenchmarkCase benchmark,
        StructuralModel model,
        StructuralAnalysisResult result,
        double tolerance)
    {
        GlobalEquilibriumResult equilibrium = new GlobalEquilibriumChecker().Check(model, result);
        Assert.True(
            equilibrium.IsInEquilibrium(tolerance),
            $"Benchmark {benchmark.Id}: global equilibrium residual too large. " +
            $"Residual Fx={equilibrium.ResidualFx}, Fy={equilibrium.ResidualFy}, Mz={equilibrium.ResidualMz}, tolerance={tolerance}.");
    }

    private static void AssertClose(string benchmarkId, string quantity, double expected, double actual, double tolerance)
    {
        double difference = Math.Abs(expected - actual);
        Assert.True(
            difference <= tolerance,
            $"Benchmark {benchmarkId}: {quantity} mismatch. Expected {expected}, actual {actual}, tolerance {tolerance}, difference {difference}.");
    }

    private static string FindRepositoryRoot()
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

    private sealed class BenchmarkCatalog
    {
        public List<BenchmarkCase> Benchmarks { get; set; } = new();

        public static BenchmarkCatalog Load(string filePath)
        {
            string json = File.ReadAllText(filePath);
            BenchmarkCatalog? catalog = JsonSerializer.Deserialize<BenchmarkCatalog>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            return catalog ?? throw new InvalidOperationException("Benchmark expected-results.json is empty or invalid.");
        }
    }

    private sealed class BenchmarkCase
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ModelPath { get; set; } = string.Empty;
        public string AnalysisId { get; set; } = string.Empty;
        public double Tolerance { get; set; } = 1e-6;
        public BenchmarkExpected Expected { get; set; } = new();
    }

    private sealed class BenchmarkExpected
    {
        public List<ExpectedSupportReaction> SupportReactions { get; set; } = new();
        public double? MaxAbsShear { get; set; }
        public double? MaxAbsMoment { get; set; }
        public double? TipDisplacementY { get; set; }
        public double? TipRotationZ { get; set; }
        public List<ExpectedMemberAxialForce> MemberAxialForces { get; set; } = new();
        public List<ExpectedNodalDisplacement> NodalDisplacements { get; set; } = new();
        public List<string> Checks { get; set; } = new();
    }

    private sealed class ExpectedSupportReaction
    {
        public string NodeId { get; set; } = string.Empty;
        public double? Fx { get; set; }
        public double? Fy { get; set; }
        public double? Mz { get; set; }
    }


    private sealed class ExpectedNodalDisplacement
    {
        public string NodeId { get; set; } = string.Empty;
        public double? Ux { get; set; }
        public double? Uy { get; set; }
        public double? Rz { get; set; }
    }

    private sealed class ExpectedMemberAxialForce
    {
        public string MemberId { get; set; } = string.Empty;
        public double NormalForce { get; set; }
    }
}
