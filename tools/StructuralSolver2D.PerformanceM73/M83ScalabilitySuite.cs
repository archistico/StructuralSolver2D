using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Frame2D.SecondOrder;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.PerformanceM73;

/// <summary>
/// M83-A: opt-in end-to-end Frame2D scalability baseline. Measures the ACTUAL
/// first-order frame solver and the experimental M82 linearized tangent solver.
/// It neither introduces a sparse solver nor treats timing as an accuracy test.
/// </summary>
public static class M83ScalabilitySuite
{
    private const double E = 210_000_000d;   // kN/m^2
    private const double A = 0.006d;         // m^2
    private const double I = 0.00002d;       // m^4
    private const double L = 4d;             // m
    private const double H = 2d;             // kN, global +X
    private const double P = 100d;           // kN, global -Y (M82 only)

    public sealed record CaseResult(
        string Scenario, int MemberCount, int PhysicalDofs, int FreeDofs,
        double MaxNumericalError, double TipUx, double? CriticalMultiplier,
        double MedianMilliseconds, long MedianAllocatedBytes);

    public sealed record Report(
        string Runtime, string OperatingSystem, string Architecture,
        int ProcessorCount, bool Quick, int Repetitions, DateTimeOffset CapturedUtc,
        IReadOnlyList<CaseResult> Cases);

    private sealed record Sample(double Error, double TipUx, double? CriticalMultiplier);

    public static Report Run(bool quick = false, int repetitions = 3)
    {
        if (repetitions < 1 || repetitions > 20)
            throw new ArgumentOutOfRangeException(nameof(repetitions), "Use 1..20 repetitions.");

        var results = new List<CaseResult>();
        int[] firstOrderSizes = quick ? new[] { 2, 4 } : new[] { 8, 16, 32, 64 };
        int[] secondOrderSizes = quick ? new[] { 2, 4 } : new[] { 2, 4, 8, 16 };

        foreach (int size in firstOrderSizes)
        {
            StructuralModel model = Cantilever(size, compression: 0d);
            var solver = new Frame2DAnalyzer();
            results.Add(Measure("Frame2D-first-order", size, repetitions,
                () => CheckFirstOrder(solver, model)));
        }
        foreach (int size in secondOrderSizes)
        {
            StructuralModel model = Cantilever(size, compression: P);
            var solver = new Frame2DGlobalSecondOrderM82();
            results.Add(Measure("Frame2D-M82-linearized", size, repetitions,
                () => CheckM82(solver, model)));
        }
        return new Report(
            System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.ProcessorCount, quick, repetitions, DateTimeOffset.UtcNow, results);
    }

    /// <summary>Write separate M83 files; never overwrite the M73 baseline.</summary>
    public static void Save(Report report, string directory)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(directory, "m83-scalability.json"), json, new UTF8Encoding(false));

        var csv = new StringBuilder();
        csv.AppendLine("scenario,members,physical_dofs,free_dofs,max_numerical_error,tip_ux_m,critical_multiplier,median_ms,median_allocated_bytes");
        foreach (CaseResult item in report.Cases)
        {
            csv.Append(item.Scenario).Append(',')
                .Append(item.MemberCount).Append(',')
                .Append(item.PhysicalDofs).Append(',')
                .Append(item.FreeDofs).Append(',')
                .Append(item.MaxNumericalError.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(item.TipUx.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(item.CriticalMultiplier?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
                .Append(item.MedianMilliseconds.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(item.MedianAllocatedBytes).AppendLine();
        }
        File.WriteAllText(Path.Combine(directory, "m83-scalability.csv"), csv.ToString(), new UTF8Encoding(false));
    }

    private static CaseResult Measure(string scenario, int members, int repetitions, Func<Sample> run)
    {
        _ = run(); // JIT warmup, and explicit numerical validation before timing.
        var times = new List<double>(repetitions);
        var allocations = new List<long>(repetitions);
        Sample? sample = null;
        for (int i = 0; i < repetitions; i++)
        {
            long initialAllocations = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            sample = run();
            watch.Stop();
            times.Add(watch.Elapsed.TotalMilliseconds);
            allocations.Add(GC.GetAllocatedBytesForCurrentThread() - initialAllocations);
        }
        times.Sort(); allocations.Sort();
        return new CaseResult(scenario, members, 3 * (members + 1), 3 * members,
            sample!.Error, sample.TipUx, sample.CriticalMultiplier,
            times[times.Count / 2], allocations[allocations.Count / 2]);
    }

    private static StructuralModel Cantilever(int members, double compression)
    {
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("STEEL", "Steel", E))
            .AddSection(new StructuralSection("SECTION", "Section", A, I))
            .AddLoadCase(new StructuralLoadCase("LC", "Tip loads"));
        for (int i = 0; i <= members; i++)
            model.AddNode(new StructuralNode($"N{i}", 0d, L * i / members));
        for (int i = 0; i < members; i++)
            model.AddMember(new StructuralMember($"M{i + 1}", $"N{i}", $"N{i + 1}", "STEEL", "SECTION"));
        model.AddSupport(StructuralSupport.Fixed("BASE", "N0"));
        model.AddLoad(StructuralLoad.NodalForce("H", "LC", $"N{members}", StructuralLoadDirection.GlobalX, H));
        if (compression > 0d)
            model.AddLoad(StructuralLoad.NodalForce("P", "LC", $"N{members}", StructuralLoadDirection.GlobalY, -compression));
        return model;
    }

    private static Sample CheckFirstOrder(Frame2DAnalyzer solver, StructuralModel model)
    {
        var solution = solver.Analyze(model, "LC");
        int n = model.Members.Count;
        double expectedUx = H * L * L * L / (3d * E * I);
        double actualUx = solution.GetDisplacement($"N{n}").Ux;
        double maxError = RelativeError(actualUx, expectedUx);
        maxError = Math.Max(maxError, RelativeError(solution.Reactions.Sum(x => x.Fx), -H));
        maxError = Math.Max(maxError, Math.Abs(solution.Reactions.Sum(x => x.Fy)));
        Ensure(maxError < 1e-6, $"Frame2D first-order analytic/equilibrium mismatch (n={n})", maxError);
        return new Sample(maxError, actualUx, null);
    }

    private static Sample CheckM82(Frame2DGlobalSecondOrderM82 solver, StructuralModel model)
    {
        var solution = solver.Analyze(model, "LC");
        int n = model.Members.Count;
        double reference = H * L * L * L / (3d * E * I);
        double actualUx = solution.GetDisplacement($"N{n}").Ux;
        double maxError = RelativeError(solution.FirstOrder.GetDisplacement($"N{n}").Ux, reference);
        maxError = Math.Max(maxError, RelativeError(solution.Reactions.Sum(x => x.Fx), -H));
        maxError = Math.Max(maxError, RelativeError(solution.Reactions.Sum(x => x.Fy), P));
        foreach (var member in solution.MemberAxialReferences)
            maxError = Math.Max(maxError, RelativeError(member.AxialForce, -P));
        Ensure(double.IsFinite(actualUx) && actualUx > reference,
            $"M82 compressive amplification is not physical (n={n})");
        Ensure(solution.CriticalLoadMultiplier is > 1d,
            $"M82 did not report a stable critical multiplier (n={n})");
        Ensure(maxError < 1e-6, $"M82 first-order/axial/equilibrium mismatch (n={n})", maxError);
        // No arbitrary second-order displacement reference: M82 is a LINEARIZED
        // prestress model; closed-form Euler cantilever is used as an external
        // qualitative check on amplification, not presented as an exact M82 value.
        return new Sample(maxError, actualUx, solution.CriticalLoadMultiplier);
    }

    private static double RelativeError(double actual, double expected) =>
        Math.Abs(actual - expected) / Math.Max(1d, Math.Abs(expected));

    private static void Ensure(bool condition, string message, double? value = null)
    {
        if (!condition)
            throw new InvalidOperationException(value.HasValue ? $"{message}: {value.Value:G17}" : message);
    }
}
