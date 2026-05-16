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
        AssertMemberEndForces(benchmark, result, expected.MemberEndForces, tolerance);
        AssertFrameExtremaIfRequested(benchmark, model, result, expected, tolerance);
        AssertDisplacementsIfRequested(benchmark, result, expected, tolerance);
        AssertReactionSumsIfRequested(benchmark, result, expected.ReactionSums, tolerance);
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


    private static void AssertMemberEndForces(
        BenchmarkCase benchmark,
        StructuralAnalysisResult result,
        IReadOnlyList<ExpectedMemberEndForce> expectedMemberEndForces,
        double tolerance)
    {
        foreach (ExpectedMemberEndForce expected in expectedMemberEndForces)
        {
            MemberEndForceResult force = result.MemberEndForces.Single(force =>
                string.Equals(force.MemberId, expected.MemberId, StringComparison.OrdinalIgnoreCase));

            if (expected.StartAxial.HasValue)
            {
                AssertClose(benchmark, $"start axial force in member {expected.MemberId}", expected.StartAxial.Value, force.StartAxial, tolerance);
            }

            if (expected.StartShear.HasValue)
            {
                AssertClose(benchmark, $"start shear force in member {expected.MemberId}", expected.StartShear.Value, force.StartShear, tolerance);
            }

            if (expected.StartMoment.HasValue)
            {
                AssertClose(benchmark, $"start moment in member {expected.MemberId}", expected.StartMoment.Value, force.StartMoment, tolerance);
            }

            if (expected.EndAxial.HasValue)
            {
                AssertClose(benchmark, $"end axial force in member {expected.MemberId}", expected.EndAxial.Value, force.EndAxial, tolerance);
            }

            if (expected.EndShear.HasValue)
            {
                AssertClose(benchmark, $"end shear force in member {expected.MemberId}", expected.EndShear.Value, force.EndShear, tolerance);
            }

            if (expected.EndMoment.HasValue)
            {
                AssertClose(benchmark, $"end moment in member {expected.MemberId}", expected.EndMoment.Value, force.EndMoment, tolerance);
            }
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


    private static void AssertReactionSumsIfRequested(
        BenchmarkCase benchmark,
        StructuralAnalysisResult result,
        ExpectedReactionSums? expectedReactionSums,
        double tolerance)
    {
        if (expectedReactionSums is null)
        {
            return;
        }

        if (expectedReactionSums.Fx.HasValue)
        {
            AssertClose(benchmark, "sum of support reactions Fx", expectedReactionSums.Fx.Value, result.Reactions.Sum(reaction => reaction.Fx), tolerance);
        }

        if (expectedReactionSums.Fy.HasValue)
        {
            AssertClose(benchmark, "sum of support reactions Fy", expectedReactionSums.Fy.Value, result.Reactions.Sum(reaction => reaction.Fy), tolerance);
        }

        if (expectedReactionSums.Mz.HasValue)
        {
            AssertClose(benchmark, "sum of support reactions Mz", expectedReactionSums.Mz.Value, result.Reactions.Sum(reaction => reaction.Mz), tolerance);
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
            else if (check.Contains("finite displacements", StringComparison.OrdinalIgnoreCase))
            {
                Assert.All(result.Displacements, displacement =>
                {
                    Assert.True(double.IsFinite(displacement.Ux), $"Benchmark {benchmark.Id}: non-finite Ux at node {displacement.NodeId}.");
                    Assert.True(double.IsFinite(displacement.Uy), $"Benchmark {benchmark.Id}: non-finite Uy at node {displacement.NodeId}.");
                    Assert.True(double.IsFinite(displacement.Rz), $"Benchmark {benchmark.Id}: non-finite Rz at node {displacement.NodeId}.");
                });
            }
            else if (check.Contains("non-zero member forces", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Contains(result.MemberEndForces, force =>
                    Math.Abs(force.EndAxial) > tolerance ||
                    Math.Abs(force.EndShear) > tolerance ||
                    Math.Abs(force.EndMoment) > tolerance);
            }
            else if (check.Contains("non-zero support reactions", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Contains(result.Reactions, reaction =>
                    Math.Abs(reaction.Fx) > tolerance ||
                    Math.Abs(reaction.Fy) > tolerance ||
                    Math.Abs(reaction.Mz) > tolerance);
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
