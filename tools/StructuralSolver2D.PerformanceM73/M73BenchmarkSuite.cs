using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using StructuralSolver2D.Analysis.Frame2D.Nonlinear;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Solvers;

namespace StructuralSolver2D.PerformanceM73;

/// <summary>One repeatable, opt-in C# baseline. No time-dependent pass/fail threshold.</summary>
public static class M73BenchmarkSuite
{
    public sealed record CaseResult(string Scenario, int Size, int PhysicalDofs, int ActiveDofs,
        int Steps, int Iterations, double MaxNumericalError,
        double MedianMilliseconds, long MedianAllocatedBytes);

    public sealed record Report(string Runtime, string OperatingSystem, string Architecture,
        int ProcessorCount, bool Quick, int Repetitions, DateTimeOffset CapturedUtc,
        IReadOnlyList<CaseResult> Cases);

    private sealed record Sample(double MaxError, int Iterations);

    /// <summary>Run warmup + repetitions per case; failures of numerical checks throw.</summary>
    public static Report Run(bool quick = false, int repetitions = 3)
    {
        if (repetitions < 1 || repetitions > 20)
            throw new ArgumentOutOfRangeException(nameof(repetitions));
        var cases = new List<CaseResult>();
        foreach (int n in quick ? new[] { 8, 16 } : new[] { 16, 32, 64, 96 })
            cases.Add(Measure("Dense-SPD", n, n, n, 1, repetitions, () => Dense(n)));
        foreach (int n in quick ? new[] { 3, 5 } : new[] { 4, 8, 12, 20 })
            cases.Add(Measure("Truss-static", n, 2 * (n + 1), n, 2, repetitions,
                () => StaticChain(n)));
        foreach (int n in quick ? new[] { 2, 4 } : new[] { 3, 6, 10, 16 })
            cases.Add(Measure("Truss-Newmark", n, 2 * (n + 1), n, 7, repetitions,
                () => DynamicChain(n)));
        foreach (int n in quick ? new[] { 10 } : new[] { 25, 100, 250 })
            cases.Add(Measure("Frame-hinge-trials", n, 6, 6, n, repetitions,
                () => FrameHinge(n)));
        return new Report(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.ProcessorCount, quick, repetitions, DateTimeOffset.UtcNow, cases);
    }

    private static CaseResult Measure(string scenario, int size, int dofs, int active, int steps,
        int repetitions, Func<Sample> action)
    {
        _ = action(); // warmup: JIT and lazy initialization excluded from measurements
        var times = new List<double>(repetitions);
        var allocations = new List<long>(repetitions);
        Sample? last = null;
        for (int i = 0; i < repetitions; i++)
        {
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            last = action();
            watch.Stop();
            times.Add(watch.Elapsed.TotalMilliseconds);
            allocations.Add(GC.GetAllocatedBytesForCurrentThread() - bytes);
        }
        times.Sort(); allocations.Sort();
        return new CaseResult(scenario, size, dofs, active, steps, last!.Iterations,
            last.MaxError, times[times.Count / 2], allocations[allocations.Count / 2]);
    }

    private static Sample Dense(int n)
    {
        // A is positive definite and tridiagonal, but intentionally stored DENSE.
        // Prescribed solution x=1; rhs is independently assembled as row sums.
        var matrix = new double[n, n];
        var rhs = new double[n];
        for (int i = 0; i < n; i++)
        {
            matrix[i, i] = 4;
            if (i > 0) matrix[i, i - 1] = -1;
            if (i + 1 < n) matrix[i, i + 1] = -1;
            for (int j = 0; j < n; j++) rhs[i] += matrix[i, j];
        }
        var x = DenseLinearSystemSolver.Solve(matrix, rhs);
        double error = x.Max(v => Math.Abs(v - 1));
        Ensure(error <= 1e-10, "Dense exact-reference mismatch", error);
        return new Sample(error, 1);
    }

    private static NonlinearTruss2DModel Chain(int n, bool dynamic)
    {
        var nodes = Enumerable.Range(0, n + 1)
            .Select(i => new NonlinearTruss2DNode($"N{i}", i, 0)).ToArray();
        var members = Enumerable.Range(0, n)
            .Select(i => new NonlinearTruss2DMember($"E{i}", $"N{i}", $"N{i+1}",
                0.01, 200_000, 1e9, 200, dynamic ? .02 : null)).ToArray();
        var supports = nodes.Select(x => new NonlinearTruss2DSupport(x.Id, 0, 1)).ToList();
        supports.Add(new NonlinearTruss2DSupport("N0", 1, 0));
        var loads = new[] { new NonlinearTruss2DLoad($"N{n}", 1, 0) };
        return new NonlinearTruss2DModel(nodes, members, supports, loads);
    }

    private static Sample StaticChain(int n)
    {
        var result = new StructuralNonlinearTruss2DService().AnalyzeStatic(
            new StructuralNonlinearStaticRequest(Chain(n, false), new[] { .5, 1d }));
        Ensure(result.Completed && result.Steps.Count == 2,
            "Static chain not converged: " + result.Failure);
        var end = result.Steps[^1];
        double expected = n / 2000d; // F L / EA = (1kN)*(n m)/(0.01m²*200000kN/m²)
        double error = Math.Abs(end.Displacements[2 * n] - expected);
        Ensure(error <= 5e-8 * Math.Max(1, expected), "Static chain analytic displacement mismatch", error);
        Ensure(end.AxialForces.All(force => Math.Abs(force - 1) < 1e-6),
            "Static chain axial forces do not satisfy equilibrium");
        Ensure(end.MaterialStates.All(state => state.AccumulatedPlasticStrain == 0),
            "Elastic reference must not plasticize");
        return new Sample(error, result.Steps.Sum(s => s.NewtonIterations));
    }

    private static Sample DynamicChain(int n)
    {
        double[] history = { 0, .3, 1, 1, .4, 0, -.2, 0 };
        var result = new StructuralNonlinearTruss2DService().AnalyzeTime(
            new StructuralNonlinearTimeRequest(Chain(n, true), history, .01));
        Ensure(result.Completed && result.Steps.Count == history.Length,
            "Time chain not converged: " + result.Failure);
        double maxResidual = result.Steps.Max(step => Math.Abs(step.ReducedResidualNorm));
        Ensure(double.IsFinite(maxResidual) && maxResidual < 1e-6,
            "Time chain dynamic equilibrium residual is excessive", maxResidual);
        Ensure(result.Steps.All(step => step.Displacements.All(double.IsFinite) &&
            step.MaterialStates.All(state => state.AccumulatedPlasticStrain == 0)),
            "Elastic time history contains nonfinite displacement or plasticity");
        return new Sample(maxResidual, result.Steps.Sum(s => s.NewtonIterations));
    }

    private static Sample FrameHinge(int count)
    {
        var element = new ElastoplasticFrame2DElement(0, 0, 4, 0,
            210_000_000, .012, .000032,
            startHinge: new BilinearRotationalHinge(4000, 20, 40),
            endHinge: new BilinearRotationalHinge(4000, 20, 40));
        double error = 0;
        int iterations = 0;
        for (int i = 0; i < count; i++)
        {
            double amplitude = .032 + .00001 * (i % 9);
            var trial = element.EvaluateTrial(new[] { 0d, 0d, -amplitude, 0d, 0d, amplitude });
            iterations += trial.LocalIterations;
            for (int a = 0; a < 6; a++)
                for (int b = 0; b < 6; b++)
                    error = Math.Max(error, Math.Abs(trial.GlobalTangent[a, b] - trial.GlobalTangent[b, a]));
            Ensure(trial.EndTrialState.AccumulatedPlasticRotation > 0 &&
                trial.StartTrialState.AccumulatedPlasticRotation > 0,
                "Hinges did not yield under the benchmark rotations");
        }
        Ensure(error < 1e-7, "Frame hinge condensed tangent asymmetry", error);
        Ensure(element.EndCommittedState.AccumulatedPlasticRotation == 0 &&
            element.StartCommittedState.AccumulatedPlasticRotation == 0,
            "Trial benchmarking must never commit material history");
        return new Sample(error, iterations);
    }

    private static void Ensure(bool condition, string message, double? value = null)
    {
        if (!condition)
            throw new InvalidOperationException($"M73_NUMERICAL: {message}" +
                (value.HasValue ? $" ({value.Value.ToString("G17", CultureInfo.InvariantCulture)})" : ""));
    }

    public static void Save(Report report, string directory)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(directory, "m73-baseline.json"),
            JsonSerializer.Serialize(report, options), new UTF8Encoding(false));
        var csv = new StringBuilder("scenario,size,physical_dofs,active_dofs,steps,newton_or_local_iterations,max_numerical_error,median_ms,median_allocated_bytes\n");
        foreach (var c in report.Cases)
            csv.AppendJoin(',', c.Scenario, c.Size.ToString(CultureInfo.InvariantCulture),
                c.PhysicalDofs.ToString(CultureInfo.InvariantCulture),
                c.ActiveDofs.ToString(CultureInfo.InvariantCulture),
                c.Steps.ToString(CultureInfo.InvariantCulture),
                c.Iterations.ToString(CultureInfo.InvariantCulture),
                c.MaxNumericalError.ToString("G17", CultureInfo.InvariantCulture),
                c.MedianMilliseconds.ToString("G17", CultureInfo.InvariantCulture),
                c.MedianAllocatedBytes.ToString(CultureInfo.InvariantCulture)).Append('\n');
        File.WriteAllText(Path.Combine(directory, "m73-baseline.csv"), csv.ToString(), new UTF8Encoding(false));
    }
}
