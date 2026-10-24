using StructuralSolver2D.PerformanceM73;

try
{
    bool quick = false;
    bool m83 = false;
    bool explicitOutput = false;
    int repetitions = 3;
    string output = Path.Combine("artifacts", "benchmarks", "m73");
    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--quick": quick = true; break;
            case "--m83": m83 = true; break;
            case "--output" when i + 1 < args.Length: output = args[++i]; explicitOutput = true; break;
            case "--repetitions" when i + 1 < args.Length &&
                int.TryParse(args[i + 1], out int parsed) && parsed >= 1 && parsed <= 20:
                repetitions = parsed; i++; break;
            default: throw new ArgumentException("BENCH_ARGS: Use [--m83] [--quick] [--repetitions 1..20] [--output path].");
        }
    }

    if (m83)
    {
        if (!explicitOutput) output = Path.Combine("artifacts", "benchmarks", "m83");
        var m83Report = M83ScalabilitySuite.Run(quick, repetitions);
        M83ScalabilitySuite.Save(m83Report, output);
        Console.WriteLine($"M83-A Frame2D scalability profiling complete: {m83Report.Cases.Count} cases.");
        foreach (var item in m83Report.Cases)
            Console.WriteLine($"{item.Scenario,-23} members={item.MemberCount,3} DOFs={item.FreeDofs,3} " +
                $"median={item.MedianMilliseconds:F3} ms alloc={item.MedianAllocatedBytes} bytes " +
                $"numeric-error={item.MaxNumericalError:E3} critical-multiplier={item.CriticalMultiplier?.ToString("F3") ?? "N/A"}");
        Console.WriteLine($"Reports: {Path.GetFullPath(output)} (CSV and JSON)");
        return;
    }

    var report = M73BenchmarkSuite.Run(quick, repetitions);
    M73BenchmarkSuite.Save(report, output);
    Console.WriteLine($"M73 profiling complete: {report.Cases.Count} cases, .NET {Environment.Version}, {Environment.OSVersion}.");
    foreach (var item in report.Cases)
        Console.WriteLine($"{item.Scenario,-22} members/size={item.Size,3} DOFs={item.ActiveDofs,3} " +
            $"median={item.MedianMilliseconds:F3} ms alloc={item.MedianAllocatedBytes} bytes " +
            $"iterations={item.Iterations} max-error={item.MaxNumericalError:E3}");
    Console.WriteLine($"Reports: {Path.GetFullPath(output)} (CSV and JSON)");
}
catch (Exception ex)
{
    Console.Error.WriteLine("M73 benchmark FAILED: " + ex);
    Environment.ExitCode = 1;
}
