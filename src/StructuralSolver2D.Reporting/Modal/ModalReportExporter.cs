using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.PublicApi;

namespace StructuralSolver2D.Reporting.Modal;

/// <summary>Deterministic, invariant-culture modal tables, independent of static reports.</summary>
public sealed class ModalReportExporter
{
    private static string Number(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
    private static string CsvText(string? text)
    {
        string value = text ?? "";
        // Spreadsheet formula injection protection for untrusted node labels/IDs.
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0])) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    private static string MarkdownText(string? text) =>
        (text ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

    public string ExportSummaryCsv(StructuralModalAnalysisOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var b = new StringBuilder("Mode,FrequencyHz,PeriodSeconds,AngularFrequencyRadiansPerSecond,RelativeResidual,RayleighQuotient\n");
        foreach (var mode in output.Modes)
            b.Append(mode.ModeNumber).Append(',').Append(Number(mode.FrequencyHertz)).Append(',')
                .Append(Number(mode.PeriodSeconds)).Append(',').Append(Number(mode.AngularFrequencyRadiansPerSecond))
                .Append(',').Append(Number(mode.RelativeResidual)).Append(',').Append(Number(mode.RayleighQuotient)).Append('\n');
        return b.ToString();
    }

    public string ExportShapesCsv(StructuralModalAnalysisOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var b = new StringBuilder("Mode,NodeId,NormalizedUx,NormalizedUy,NormalizedRz\n");
        foreach (var mode in output.Modes)
            foreach (var node in mode.Shape)
                b.Append(mode.ModeNumber).Append(',').Append(CsvText(node.NodeId)).Append(',')
                    .Append(Number(node.Ux)).Append(',').Append(Number(node.Uy)).Append(',')
                    .Append(Number(node.Rz)).Append('\n');
        return b.ToString();
    }

    public string ExportMarkdown(StructuralModalAnalysisOutput output, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        var b = new StringBuilder();
        b.Append("# ").AppendLine(MarkdownText(title ?? "Modal analysis"));
        b.AppendLine().AppendLine("Linear elastic free-vibration eigenanalysis (2D). Frequencies are in Hz, periods in seconds, and angular frequencies in rad/s.");
        // M57-FIX1: report the normalization contract from the computed modal result.
        b.AppendLine().Append("**Modal normalization:** ").Append(MarkdownText(output.Normalization))
            .AppendLine(". Component amplitudes and signs are not physical response displacements; animation timing is illustrative.");
        b.AppendLine().AppendLine("| Mode | Frequency [Hz] | Period [s] | Angular frequency [rad/s] | Relative residual |");
        b.AppendLine("|---:|---:|---:|---:|---:|");
        foreach (var mode in output.Modes)
            b.Append("| ").Append(mode.ModeNumber).Append(" | ").Append(Number(mode.FrequencyHertz))
                .Append(" | ").Append(Number(mode.PeriodSeconds)).Append(" | ")
                .Append(Number(mode.AngularFrequencyRadiansPerSecond)).Append(" | ")
                .Append(Number(mode.RelativeResidual)).AppendLine(" |");
        b.AppendLine().Append("Available DOFs: ").AppendLine(output.AvailableModeCount.ToString(CultureInfo.InvariantCulture));
        b.Append("Maximum mass-orthogonality error: ").AppendLine(Number(output.MaximumMassOrthogonalityError));
        b.Append("Maximum stiffness-orthogonality error: ").AppendLine(Number(output.MaximumStiffnessOrthogonalityError));
        if (output.Warnings.Count > 0)
        {
            b.AppendLine().AppendLine("## Limitations and diagnostics");
            foreach (var warning in output.Warnings) b.Append("- ").AppendLine(MarkdownText(warning));
        }
        return b.ToString();
    }
}
