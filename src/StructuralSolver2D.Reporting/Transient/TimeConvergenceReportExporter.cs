using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.PublicApi;

namespace StructuralSolver2D.Reporting.Transient;

/// <summary>Numerical time-grid sensitivity report; it never issues a structural design verdict.</summary>
public sealed class TimeConvergenceReportExporter
{
    private static readonly CultureInfo I = CultureInfo.InvariantCulture;
    private static string N(double x) => x.ToString("G9", I);

    public string ExportMarkdown(StructuralTimeConvergenceOutput result, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        var s = new StringBuilder();
        s.AppendLine($"# {title ?? "Time-history convergence"} — M59.1");
        s.AppendLine();
        s.AppendLine($"**Method:** {result.Method}.");
        s.AppendLine($"**Interval:** 0..{N(result.EndTimeSeconds)} s; coarse steps={result.CoarseStepCount}.");
        s.AppendLine($"**dt:** {N(result.CoarseTimeStepSeconds)}, {N(result.CoarseTimeStepSeconds/2)}, {N(result.CoarseTimeStepSeconds/4)} s.");
        s.AppendLine($"**Interpretation:** {result.Interpretation}");
        s.AppendLine();
        s.AppendLine("## Comparison at matching timestamps");
        s.AppendLine();
        s.AppendLine("Each error is max |difference| divided by the peak absolute fine-grid amplitude of the **same component** over all nodes and times. Inactive signals are excluded; values are dimensionless.");
        s.AppendLine();
        s.AppendLine("| Component | Unit | Active | coarse/medium | medium/fine | Observed order | Fine-grid envelope |");
        s.AppendLine("|---|---|---|---:|---:|---:|---:|");
        foreach (StructuralTimeComponentComparison c in result.Components)
            s.AppendLine($"| {c.Component} | {c.Unit} | {(c.HasResolvedSignal ? "yes" : "no")} | {N(c.CoarseMediumRelativeDifference)} | {N(c.MediumFineRelativeDifference)} | {(c.ObservedOrder.HasValue ? N(c.ObservedOrder.Value) : "n/a")} | {N(c.FineAbsoluteEnvelope)} |");
        s.AppendLine();
        s.AppendLine($"**Active response:** {result.HasMeaningfulResponse}; **successive differences decrease:** {result.DifferencesDecrease}; **illustrative tolerance:** {result.MeetsIllustrativeTolerances}; **all modes below dt*f=0.1 on fine grid:** {result.FineGridResolvesAllModes}.");
        s.AppendLine("Illustrative tolerance checks: 1% displacement/rotation, 2% velocity and 5% acceleration on medium/fine common-time discrepancies. These are not validated engineering acceptance criteria.");
        s.AppendLine();
        StructuralTimePeakComparison p = result.FocusPeak;
        s.AppendLine($"## Sampled peak: {p.NodeId} {p.Component} [{p.Unit}]");
        s.AppendLine();
        s.AppendLine("| Grid | Absolute peak | Time [s] |");
        s.AppendLine("|---|---:|---:|");
        s.AppendLine($"| coarse | {N(p.CoarseAmplitude)} | {N(p.CoarseTimeSeconds)} |");
        s.AppendLine($"| medium | {N(p.MediumAmplitude)} | {N(p.MediumTimeSeconds)} |");
        s.AppendLine($"| fine | {N(p.FineAmplitude)} | {N(p.FineTimeSeconds)} |");
        s.AppendLine($"Peak amplitude deviations relative to fine: coarse/fine={N(p.CoarseFinePeakRelativeDifference)}, medium/fine={N(p.MediumFinePeakRelativeDifference)}. Occurrence times are quantized to their respective grids.");
        s.AppendLine();
        s.AppendLine("## Natural-frequency resolution");
        s.AppendLine();
        s.AppendLine("Flag any ratio dt*f > 0.1 (fewer than 10 samples/cycle). This is a conservative heuristic applied to **every** mode, including modes with negligible excitation.");
        s.AppendLine();
        s.AppendLine("| Mode | Frequency [Hz] | coarse dt*f | medium dt*f | fine dt*f | Coarse warning | Fine warning |");
        s.AppendLine("|---:|---:|---:|---:|---:|---|---|");
        foreach (StructuralTimeFrequencyResolution mode in result.ModalResolution)
            s.AppendLine($"| {mode.Mode} | {N(mode.FrequencyHertz)} | {N(mode.CoarseCyclesPerStep)} | {N(mode.MediumCyclesPerStep)} | {N(mode.FineCyclesPerStep)} | {(mode.CoarseCyclesPerStep > .1 ? "yes" : "no")} | {(mode.FineCyclesPerStep > .1 ? "yes" : "no")} |");
        s.AppendLine();
        s.AppendLine("## Diagnostic limitations");
        foreach (string warning in result.Warnings) s.AppendLine($"- {warning}");
        return s.ToString();
    }

    public string ExportCsv(StructuralTimeConvergenceOutput result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var s = new StringBuilder();
        s.AppendLine("Component,Unit,Active,CoarseMediumRelative,MediumFineRelative,ObservedOrder,FineAbsoluteEnvelope");
        foreach (StructuralTimeComponentComparison c in result.Components)
            s.AppendLine($"{c.Component},{c.Unit},{(c.HasResolvedSignal ? "true" : "false")},{N(c.CoarseMediumRelativeDifference)},{N(c.MediumFineRelativeDifference)},{(c.ObservedOrder.HasValue ? N(c.ObservedOrder.Value) : "")},{N(c.FineAbsoluteEnvelope)}");
        return s.ToString();
    }
}
