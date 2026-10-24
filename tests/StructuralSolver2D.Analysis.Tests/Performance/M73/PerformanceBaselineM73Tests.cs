using System.Text.Json;
using StructuralSolver2D.Analysis.Solvers;
using StructuralSolver2D.PerformanceM73;

namespace StructuralSolver2D.Analysis.Tests.Performance.M73;

/// <summary>Numeric acceptance is independent of the machine's elapsed times.</summary>
public sealed class PerformanceBaselineM73Tests
{
    [Fact]
    public void QuickBaseline_AllScenariosHaveNumericalValidationAndMeaningfulDimensions()
    {
        var report = M73BenchmarkSuite.Run(quick: true, repetitions: 1);
        Assert.Equal(7, report.Cases.Count);
        Assert.Equal(2, report.Cases.Count(x => x.Scenario == "Dense-SPD"));
        Assert.Equal(2, report.Cases.Count(x => x.Scenario == "Truss-static"));
        Assert.Equal(2, report.Cases.Count(x => x.Scenario == "Truss-Newmark"));
        Assert.Single(report.Cases, x => x.Scenario == "Frame-hinge-trials");
        foreach (var item in report.Cases)
        {
            Assert.InRange(item.MaxNumericalError, 0, 1e-6);
            Assert.True(double.IsFinite(item.MedianMilliseconds) && item.MedianMilliseconds >= 0);
            Assert.True(item.MedianAllocatedBytes >= 0);
            Assert.True(item.PhysicalDofs >= item.ActiveDofs);
            Assert.True(item.Iterations > 0);
        }
    }

    [Fact]
    public void CsvAndJson_UseInvariantNumbersAndRoundTrip()
    {
        var report = M73BenchmarkSuite.Run(quick: true, repetitions: 1);
        string folder = Path.Combine(Path.GetTempPath(), "m73-bench-" + Guid.NewGuid().ToString("N"));
        try
        {
            M73BenchmarkSuite.Save(report, folder);
            string csv = File.ReadAllText(Path.Combine(folder, "m73-baseline.csv"));
            Assert.Equal(report.Cases.Count + 1, csv.TrimEnd().Split('\n').Length);
            Assert.Contains("scenario,size,physical_dofs", csv);
            Assert.Contains("Frame-hinge-trials", csv);
            var readBack = JsonSerializer.Deserialize<M73BenchmarkSuite.Report>(
                File.ReadAllText(Path.Combine(folder, "m73-baseline.json")));
            Assert.NotNull(readBack);
            Assert.Equal(report.Cases.Count, readBack.Cases.Count);
            Assert.All(readBack.Cases, item => Assert.True(double.IsFinite(item.MaxNumericalError)));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void DenseSolver_NumericalScalingAndActualMechanismRemainDistinguishable()
    {
        // Mixed stiffness scales must not be misclassified as an unstable model.
        double[] x = DenseLinearSystemSolver.Solve(
            new double[,] { { 1e-6, 0 }, { 0, 2 } }, new[] { 1e-6, 2d });
        Assert.InRange(Math.Abs(x[0] - 1), 0, 1e-12);
        Assert.InRange(Math.Abs(x[1] - 1), 0, 1e-12);
        // Genuine rank deficiency must still throw (never regularize with fake stiffness).
        Assert.Throws<StructuralAnalysisException>(() => DenseLinearSystemSolver.Solve(
            new double[,] { { 1, 1 }, { 2, 2 } }, new[] { 2d, 4d }));
    }

    [Fact]
    public void RepetitionLimits_AreValidatedWithoutAnyTimingGate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => M73BenchmarkSuite.Run(quick: true, repetitions: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => M73BenchmarkSuite.Run(quick: true, repetitions: 21));
    }
}
