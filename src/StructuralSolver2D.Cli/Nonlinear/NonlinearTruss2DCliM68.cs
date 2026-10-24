using System.Text.Json;
using System.Text.Json.Serialization;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Reporting.Nonlinear;

namespace StructuralSolver2D.Cli.Nonlinear;

internal sealed record NonlinearTruss2DCliDocument(string? Title, NonlinearTruss2DCliBlock? NonlinearTruss2D);
internal sealed record NonlinearTruss2DCliBlock(
    string? Mode,
    NonlinearTruss2DModel? Model,
    IReadOnlyList<double>? LoadFactors,
    double? TimeStep = null,
    double? MassDamping = null,
    double? InitialStiffnessDamping = null,
    IReadOnlyList<double>? InitialVelocity = null,
    int? MaxIterations = null,
    double? AbsoluteForceTolerance = null,
    double? RelativeForceTolerance = null);

/// <summary>Eight opt-in commands (M68 + M69), separate from linear/static/modal JSON.</summary>
internal static class NonlinearTruss2DCliM68
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        NumberHandling = JsonNumberHandling.Strict
    };

    internal static bool IsCommand(string command) => command.ToLowerInvariant() is
        "nonlinear-static" or "nonlinear-static-report" or "nonlinear-static-csv" or
        "nonlinear-time" or "nonlinear-time-report" or "nonlinear-time-csv" or
        "nonlinear-static-viewer" or "nonlinear-time-viewer";

    internal static int Run(string[] args)
    {
        string command = args[0].ToLowerInvariant();
        bool isViewer = command.EndsWith("-viewer", StringComparison.Ordinal);
        bool export = isViewer || command.EndsWith("-report", StringComparison.Ordinal) || command.EndsWith("-csv", StringComparison.Ordinal);
        if (args.Length != (export ? 3 : 2))
            throw new ArgumentException($"M68_ARGS: {command} requires an input JSON" + (export ? " and an output path." : "."));
        string inputPath = args[1];
        NonlinearTruss2DCliDocument document = JsonSerializer.Deserialize<NonlinearTruss2DCliDocument>(
            File.ReadAllText(inputPath), JsonOptions) ?? throw new ArgumentException("M68_JSON: Invalid JSON document.");
        var block = document.NonlinearTruss2D ?? throw new ArgumentException("M68_BLOCK: Missing nonlinearTruss2D block.");
        if (block.Model is null || block.LoadFactors is null)
            throw new ArgumentException("M68_INPUT: Nonlinear model and load factors are required.");
        bool isStatic = command.StartsWith("nonlinear-static", StringComparison.Ordinal);
        if (!string.Equals(block.Mode, isStatic ? "static" : "time", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("M68_MODE: Command does not match nonlinearTruss2D.mode (static/time).");
        var solver = new StructuralSolver2DService();
        var report = new NonlinearTruss2DReportExporter();
        string markdown;
        string? nodes = null, members = null;
        string? html = null;
        bool completed;
        if (isStatic)
        {
            if (block.TimeStep is not null || block.InitialVelocity is not null || block.MassDamping is not null ||
                block.InitialStiffnessDamping is not null)
                throw new ArgumentException("M68_STATIC_FIELDS: Time-only settings are not allowed in static mode.");
            var result = solver.AnalyzeNonlinearTrussStatic(new StructuralNonlinearStaticRequest(block.Model,
                block.LoadFactors, block.MaxIterations ?? 35, block.AbsoluteForceTolerance ?? 1e-9,
                block.RelativeForceTolerance ?? 1e-9));
            markdown = report.StaticMarkdown(result, document.Title);
            if (isViewer) html = new InteractiveHtmlNonlinearTruss2DViewerExporter().ExportStatic(block.Model, result, document.Title);
            if (command.EndsWith("-csv", StringComparison.Ordinal))
            { nodes = report.StaticNodesCsv(result); members = report.StaticMembersCsv(result); }
            completed = result.Completed;
        }
        else
        {
            if (block.TimeStep is null) throw new ArgumentException("M68_DT: timeStep is required.");
            var result = solver.AnalyzeNonlinearTrussTime(new StructuralNonlinearTimeRequest(block.Model,
                block.LoadFactors, block.TimeStep.Value, block.MassDamping ?? 0,
                block.InitialStiffnessDamping ?? 0, block.InitialVelocity,
                block.MaxIterations ?? 30, block.AbsoluteForceTolerance ?? 1e-9,
                block.RelativeForceTolerance ?? 1e-9));
            markdown = report.TimeMarkdown(result, document.Title);
            if (isViewer) html = new InteractiveHtmlNonlinearTruss2DViewerExporter().ExportTime(block.Model, result, document.Title);
            if (command.EndsWith("-csv", StringComparison.Ordinal))
            { nodes = report.TimeNodesCsv(result); members = report.TimeMembersCsv(result); }
            completed = result.Completed;
        }
        if (!export) Console.Write(markdown);
        else
        {
            string output = args[2];
            if (string.Equals(Path.GetFullPath(inputPath), Path.GetFullPath(output),
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                throw new ArgumentException("M68_OUTPUT: Input and output paths must differ.");
            if (nodes is not null && members is not null)
            {
                if (File.Exists(output)) throw new ArgumentException("M68_CSV_DIR: CSV output must be a directory.");
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "nonlinear-nodes.csv"), nodes);
                File.WriteAllText(Path.Combine(output, "nonlinear-members.csv"), members);
                // The status file makes a partial, nonconverged run unmistakable in CSV-only exports.
                File.WriteAllText(Path.Combine(output, "nonlinear-status.md"), markdown);
            }
            else
            {
                string? parent = Path.GetDirectoryName(Path.GetFullPath(output));
                if (parent is not null) Directory.CreateDirectory(parent);
                File.WriteAllText(output, html ?? markdown);
            }
            Console.WriteLine($"{(isViewer ? "M69" : "M68")} output written to: {output}");
        }
        // A failed nonlinear solve must never exit as success, even if a partial report was exported.
        return completed ? 0 : 2;
    }
}
