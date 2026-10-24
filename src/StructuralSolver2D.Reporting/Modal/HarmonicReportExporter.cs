using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.PublicApi;

namespace StructuralSolver2D.Reporting.Modal;

/// <summary>Frequency response function tables, invariant culture and explicit phasor conventions.</summary>
public sealed class HarmonicReportExporter
{
    private static string Num(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
    private static string SafeCsv(string value)
    {
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0])) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    public string ExportCsv(StructuralHarmonicAnalysisOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var text = new StringBuilder("FrequencyHz,NodeId,Component,Amplitude,PhaseDegrees,Real,Imaginary,Unit\n");
        foreach (var sample in output.Samples)
            foreach (var node in sample.Nodes)
            {
                Append("Ux", node.Ux, "m");
                Append("Uy", node.Uy, "m");
                Append("Rz", node.Rz, "rad");
                void Append(string component, StructuralHarmonicComponent value, string unit) =>
                    text.Append(Num(sample.FrequencyHertz)).Append(',').Append(SafeCsv(node.NodeId)).Append(',')
                    .Append(component).Append(',').Append(Num(value.Amplitude)).Append(',')
                    .Append(Num(value.PhaseDegrees)).Append(',').Append(Num(value.Real)).Append(',')
                    .Append(Num(value.Imaginary)).Append(',').Append(unit).Append('\n');
            }
        return text.ToString();
    }

    public string ExportMarkdown(StructuralHarmonicAnalysisOutput output, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        var text = new StringBuilder("# ").AppendLine((title ?? "Linear harmonic response").Replace("\r", " ").Replace("\n", " "));
        text.AppendLine().AppendLine("## Sinusoidal conventions")
            .AppendLine(output.ForceConvention).AppendLine(output.ResponseConvention)
            .AppendLine("Fx, Fy in kN; Mz in kN*m; Ux, Uy in m; Rz in rad. Phase in degrees.")
            .AppendLine().AppendLine("| Frequency [Hz] | Node | DOF | Amplitude | Phase [deg] | Unit |")
            .AppendLine("|---:|---|---|---:|---:|---|");
        foreach (var sample in output.Samples)
            foreach (var node in sample.Nodes)
            {
                Append("Ux", node.Ux, "m"); Append("Uy", node.Uy, "m"); Append("Rz", node.Rz, "rad");
                void Append(string component, StructuralHarmonicComponent value, string unit) =>
                    text.Append("| ").Append(Num(sample.FrequencyHertz)).Append(" | ")
                        .Append(node.NodeId.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " "))
                        .Append(" | ").Append(component).Append(" | ").Append(Num(value.Amplitude)).Append(" | ")
                        .Append(Num(value.PhaseDegrees)).Append(" | ").Append(unit).AppendLine(" |");
            }
        text.AppendLine().AppendLine("## Assumptions and limitations");
        foreach (string warning in output.Warnings) text.Append("- ").AppendLine(warning);
        return text.ToString();
    }
}
