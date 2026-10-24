using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.PublicApi;

namespace StructuralSolver2D.Reporting.Seismic;

/// <summary>Explicitly non-normative reports; never imply building-code compliance.</summary>
public sealed class SeismicReportExporter
{
    private static readonly CultureInfo C = CultureInfo.InvariantCulture;
    private static string V(double value) => value.ToString("G17", C);
    private const string Notice = "**NON-NORMATIVE EXPLORATORY RESULTS — NOT A SEISMIC SAFETY OR BUILDING-CODE VERIFICATION.**";

    public string ExportSpectrumMarkdown(StructuralSeismicSpectrumOutput output, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}").AppendLine().AppendLine(Notice).AppendLine();
        var method = output.Combination == SeismicModalCombination.Cqc ? "CQC" : "SRSS";
        sb.AppendLine(output.Combination == SeismicModalCombination.Cqc
            ? "## M70 elastic pseudo-response spectrum / CQC"
            : "## M60 elastic pseudo-response spectrum / SRSS");
        if (output.Combination == SeismicModalCombination.Cqc)
            sb.AppendLine($"Equal modal damping ratio for CQC correlation: {V(output.ModalDampingRatio!.Value)}. The supplied spectrum must use physically consistent damping.");
        sb.AppendLine($"Direction: {output.Direction}. Total ground mass r^T M r: {V(output.GroundMass)} kN*s²/m.");
        sb.AppendLine($"Retained effective mass: {V(output.RetainedEffectiveMass)} kN*s²/m ({V(output.RetainedMassPercentage)}% of unconstrained ground mass).\n");
        sb.AppendLine("| Mode | T [s] | f [Hz] | Gamma | Effective mass [kN*s²/m] | Cumulative [%] | Sa [m/s²] | Sd [m] |");
        sb.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var m in output.Modes)
            sb.AppendLine($"| {m.ModeNumber} | {V(m.PeriodSeconds)} | {V(m.FrequencyHertz)} | {V(m.ParticipationFactor)} | {V(m.EffectiveMass)} | {V(m.CumulativePercentageOfGroundMass)} | {V(m.PseudoAccelerationMetersPerSecondSquared!.Value)} | {V(m.SpectralDisplacementMeters!.Value)} |");
        sb.AppendLine($"\n## {method} physical nodal envelopes (unsigned and non-simultaneous)");
        sb.AppendLine("| Node | abs Ux [m] | abs Uy [m] | abs Rz [rad] |\n|---|---:|---:|---:|");
        foreach (var n in output.NodeEnvelopes)
            sb.AppendLine($"| {n.NodeId} | {V(n.AbsUxMeters)} | {V(n.AbsUyMeters)} | {V(n.AbsRzRadians)} |");
        AppendWarnings(sb, output.Warnings);
        return sb.ToString();
    }

    public string ExportSpectrumModesCsv(StructuralSeismicSpectrumOutput output)
    {
        var sb = new StringBuilder("Mode,Period_s,Frequency_Hz,Gamma,EffectiveMass_kNs2m,CumulativePercent,Sa_ms2,Sd_m\n");
        foreach (var m in output.Modes)
            sb.AppendLine($"{m.ModeNumber},{V(m.PeriodSeconds)},{V(m.FrequencyHertz)},{V(m.ParticipationFactor)},{V(m.EffectiveMass)},{V(m.CumulativePercentageOfGroundMass)},{V(m.PseudoAccelerationMetersPerSecondSquared!.Value)},{V(m.SpectralDisplacementMeters!.Value)}");
        return sb.ToString();
    }

    public string ExportSpectrumNodesCsv(StructuralSeismicSpectrumOutput output)
    {
        string method = output.Combination == SeismicModalCombination.Cqc ? "CQC" : "SRSS";
        var sb = new StringBuilder($"NodeId,{method}AbsUx_m,{method}AbsUy_m,{method}AbsRz_rad\n");
        foreach (var n in output.NodeEnvelopes)
            sb.AppendLine($"{n.NodeId},{V(n.AbsUxMeters)},{V(n.AbsUyMeters)},{V(n.AbsRzRadians)}");
        return sb.ToString();
    }

    public string ExportTimeMarkdown(StructuralSeismicTimeOutput output, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}").AppendLine().AppendLine(Notice).AppendLine();
        sb.AppendLine("## M60 uniform base acceleration / relative-coordinate Newmark response");
        sb.AppendLine($"Direction: {output.Direction}. Ground mass r^T M r: {V(output.GroundMass)} kN*s²/m.");
        sb.AppendLine($"Time samples: {output.RelativeResponse.Steps.Count}. Relative responses use m, m/s, m/s² and rad derivatives; input ground acceleration in m/s².");
        sb.AppendLine("Absolute translational accelerations equal relative acceleration + imposed ground acceleration along the selected axis.\n");
        sb.AppendLine("| t [s] | ground acceleration [m/s²] | node | relative Ux [m] | relative Uy [m] | relative Ax [m/s²] | relative Ay [m/s²] | absolute Ax [m/s²] | absolute Ay [m/s²] |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|");
        // Only display first and last snapshot to keep console output manageable; full data in CSV.
        foreach (int index in new[] { 0, output.RelativeResponse.Steps.Count - 1 }.Distinct())
        {
            var step = output.RelativeResponse.Steps[index];
            var absolute = output.GroundAndAbsoluteAccelerations[index];
            for (int n = 0; n < step.Nodes.Count; n++)
            {
                var rel = step.Nodes[n]; var abs = absolute.AbsoluteAccelerations[n];
                sb.AppendLine($"| {V(step.TimeSeconds)} | {V(absolute.GroundAccelerationMetersPerSecondSquared)} | {rel.NodeId} | {V(rel.Ux)} | {V(rel.Uy)} | {V(rel.Ax)} | {V(rel.Ay)} | {V(abs.AxMetersPerSecondSquared)} | {V(abs.AyMetersPerSecondSquared)} |");
            }
        }
        AppendWarnings(sb, output.Warnings.Concat(output.RelativeResponse.Warnings
            .Where(s => !s.StartsWith("M59 linear first-order elastic motion only;", StringComparison.Ordinal))));
        return sb.ToString();
    }

    public string ExportTimeCsv(StructuralSeismicTimeOutput output)
    {
        var sb = new StringBuilder("Time_s,GroundAcceleration_ms2,NodeId,RelativeUx_m,RelativeUy_m,RelativeRz_rad,RelativeVx_ms,RelativeVy_ms,RelativeAx_ms2,RelativeAy_ms2,AbsoluteAx_ms2,AbsoluteAy_ms2\n");
        for (int t = 0; t < output.RelativeResponse.Steps.Count; t++)
        {
            var step = output.RelativeResponse.Steps[t]; var ground = output.GroundAndAbsoluteAccelerations[t];
            for (int i = 0; i < step.Nodes.Count; i++)
            {
                var n = step.Nodes[i]; var abs = ground.AbsoluteAccelerations[i];
                sb.AppendLine($"{V(step.TimeSeconds)},{V(ground.GroundAccelerationMetersPerSecondSquared)},{n.NodeId},{V(n.Ux)},{V(n.Uy)},{V(n.Rz)},{V(n.Vx)},{V(n.Vy)},{V(n.Ax)},{V(n.Ay)},{V(abs.AxMetersPerSecondSquared)},{V(abs.AyMetersPerSecondSquared)}");
            }
        }
        return sb.ToString();
    }

    private static void AppendWarnings(StringBuilder sb, IEnumerable<string> warnings)
    {
        sb.AppendLine("\n## Scope and limitations");
        foreach (string warning in warnings) sb.AppendLine($"- {warning}");
    }
}
