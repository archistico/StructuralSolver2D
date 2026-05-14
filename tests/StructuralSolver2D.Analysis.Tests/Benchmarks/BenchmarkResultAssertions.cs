using StructuralSolver2D.Analysis.Equilibrium;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// Centralizes all assertions supported by the benchmark catalog.
/// Keeping benchmark assertions in one place makes the JSON catalog easier to extend safely.
/// </summary>
internal static class BenchmarkResultAssertions
{
    private const int DiagramSampleCount = 101;

    public static void AssertBenchmark(StructuralModel model, StructuralAnalysisResult result, BenchmarkCase benchmark)
    {
        BenchmarkExpected expected = benchmark.Expected;
        double tolerance = benchmark.Tolerance;

        AssertSupportReactions(benchmark, result, expected.SupportReactions, tolerance);
        AssertMemberAxialForces(benchmark, result, expected.MemberAxialForces, tolerance);
        AssertFrameExtremaIfRequested(benchmark, model, result, expected, tolerance);
        AssertDisplacementsIfRequested(benchmark, result, expected, tolerance);
        AssertNamedChecksIfRequested(benchmark, result, expected.Checks, tolerance);
        AssertGlobalEquilibrium(benchmark, model, result, tolerance);
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
                AssertClose(benchmark, $"reaction Fx at node {expected.NodeId}", expected.Fx.Value, reaction.Fx, tolerance);
            }

            if (expected.Fy.HasValue)
            {
                AssertClose(benchmark, $"reaction Fy at node {expected.NodeId}", expected.Fy.Value, reaction.Fy, tolerance);
            }

            if (expected.Mz.HasValue)
            {
                AssertClose(benchmark, $"reaction Mz at node {expected.NodeId}", expected.Mz.Value, reaction.Mz, tolerance);
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

            AssertClose(benchmark, $"normal force in member {expected.MemberId}", expected.NormalForce, force.EndAxial, tolerance);
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

        IReadOnlyList<MemberInternalForceDiagram> diagrams = new Frame2DInternalForceSampler().SampleAllMembers(model, result, DiagramSampleCount);
        StructuralAnalysisSummary summary = new Frame2DResultSummarizer().Summarize(result, diagrams);

        if (expected.MaxAbsShear.HasValue)
        {
            AssertClose(benchmark, "maximum absolute shear", expected.MaxAbsShear.Value, Math.Abs(summary.MaxAbsShearForce.Value), tolerance);
        }

        if (expected.MaxAbsMoment.HasValue)
        {
            AssertClose(benchmark, "maximum absolute bending moment", expected.MaxAbsMoment.Value, Math.Abs(summary.MaxAbsBendingMoment.Value), tolerance);
        }
    }

    private static void AssertDisplacementsIfRequested(
        BenchmarkCase benchmark,
        StructuralAnalysisResult result,
        BenchmarkExpected expected,
        double tolerance)
    {
        foreach (ExpectedNodalDisplacement expectedDisplacement in expected.NodalDisplacements)
        {
            NodalDisplacementResult actual = result.GetDisplacement(expectedDisplacement.NodeId);

            if (expectedDisplacement.Ux.HasValue)
            {
                AssertClose(benchmark, $"nodal Ux at node {expectedDisplacement.NodeId}", expectedDisplacement.Ux.Value, actual.Ux, tolerance);
            }

            if (expectedDisplacement.Uy.HasValue)
            {
                AssertClose(benchmark, $"nodal Uy at node {expectedDisplacement.NodeId}", expectedDisplacement.Uy.Value, actual.Uy, tolerance);
            }

            if (expectedDisplacement.Rz.HasValue)
            {
                AssertClose(benchmark, $"nodal Rz at node {expectedDisplacement.NodeId}", expectedDisplacement.Rz.Value, actual.Rz, tolerance);
            }
        }

        if (expected.TipDisplacementY.HasValue)
        {
            NodalDisplacementResult tip = result.GetDisplacement("B");
            AssertClose(benchmark, "tip vertical displacement", expected.TipDisplacementY.Value, tip.Uy, tolerance);
        }

        if (expected.TipRotationZ.HasValue)
        {
            NodalDisplacementResult tip = result.GetDisplacement("B");
            AssertClose(benchmark, "tip rotation", expected.TipRotationZ.Value, tip.Rz, tolerance);
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
                AssertClose(benchmark, check, 60.0, totalFy, tolerance);
            }
            else if (check.Contains("RAy = RBy", StringComparison.OrdinalIgnoreCase))
            {
                double left = result.Reactions.Single(reaction => string.Equals(reaction.NodeId, "A", StringComparison.OrdinalIgnoreCase)).Fy;
                double right = result.Reactions.Single(reaction => string.Equals(reaction.NodeId, "B", StringComparison.OrdinalIgnoreCase)).Fy;
                AssertClose(benchmark, check, left, right, tolerance);
            }
            else if (check.Contains("horizontal reactions equal and opposite", StringComparison.OrdinalIgnoreCase))
            {
                double left = result.Reactions.Single(reaction => string.Equals(reaction.NodeId, "A", StringComparison.OrdinalIgnoreCase)).Fx;
                double right = result.Reactions.Single(reaction => string.Equals(reaction.NodeId, "B", StringComparison.OrdinalIgnoreCase)).Fx;
                AssertClose(benchmark, check, 0.0, left + right, tolerance);
            }
            else if (check.Contains("stable solution", StringComparison.OrdinalIgnoreCase))
            {
                Assert.NotEmpty(result.Displacements);
            }
            else
            {
                throw new InvalidOperationException($"Benchmark {benchmark.Id}: unsupported named check '{check}'.");
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
            $"Benchmark {benchmark.Id} - {benchmark.Name}: global equilibrium residual too large. " +
            $"Residual Fx={equilibrium.ResidualFx}, Fy={equilibrium.ResidualFy}, Mz={equilibrium.ResidualMz}, tolerance={tolerance}.");
    }

    private static void AssertClose(BenchmarkCase benchmark, string quantity, double expected, double actual, double tolerance)
    {
        double difference = Math.Abs(expected - actual);
        Assert.True(
            difference <= tolerance,
            $"Benchmark {benchmark.Id} - {benchmark.Name}: {quantity} mismatch. " +
            $"Expected {expected}, actual {actual}, tolerance {tolerance}, difference {difference}.");
    }
}
