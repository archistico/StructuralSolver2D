using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.PublicApi;

namespace StructuralSolver2D.Reporting.Transient;

/// <summary>Culture-invariant linear transient export with explicit physical units and energy terms.</summary>
public sealed class TimeHistoryReportExporter
{
    private static string N(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
    private static string CsvText(string x)
    {
        if (x.Length > 0 && "=+-@\t\r".Contains(x[0])) x = "'" + x;
        return "\"" + x.Replace("\"", "\"\"") + "\"";
    }
    public string ExportCsv(StructuralTimeHistoryAnalysisOutput result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var csv = new StringBuilder("TimeSeconds,Step,NodeId,Ux_m,Uy_m,Rz_rad,Vx_m_s,Vy_m_s,VrZ_rad_s,Ax_m_s2,Ay_m_s2,ArZ_rad_s2,Fx_kN,Fy_kN,Mz_kNm\n");
        foreach (StructuralTimeStepResult step in result.Steps)
            foreach (StructuralTimeNodeResult n in step.Nodes)
                csv.Append(N(step.TimeSeconds)).Append(',').Append(step.StepIndex).Append(',')
                    .Append(CsvText(n.NodeId)).Append(',').Append(string.Join(",", new[] {
                        n.Ux, n.Uy, n.Rz, n.Vx, n.Vy, n.VrZ, n.Ax, n.Ay, n.ArZ, n.Fx, n.Fy, n.Mz }.Select(N)))
                    .Append('\n');
        return csv.ToString();
    }
    public string ExportEnergyCsv(StructuralTimeHistoryAnalysisOutput result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var csv = new StringBuilder("TimeSeconds,Kinetic_kNm,Elastic_kNm,Total_kNm,ExternalWork_kNm,Dissipated_kNm,BalanceResidual_kNm\n");
        foreach (StructuralTimeStepResult s in result.Steps)
            csv.Append(string.Join(",", new[] { s.TimeSeconds, s.Energy.Kinetic, s.Energy.Elastic,
                s.Energy.Total, s.Energy.ExternalWork, s.Energy.DissipatedEnergy, s.Energy.BalanceResidual }.Select(N))).Append('\n');
        return csv.ToString();
    }
    public string ExportMarkdown(StructuralTimeHistoryAnalysisOutput result, string title)
    {
        ArgumentNullException.ThrowIfNull(result);
        var sb = new StringBuilder();
        sb.Append("# ").AppendLine((title ?? "Time-history analysis").Replace("\r", " ").Replace("\n", " "))
            .AppendLine().AppendLine("## Linear transient analysis (M59)")
            .AppendLine(result.Integration).AppendLine(result.LoadConvention)
            .AppendLine("Physical Ux/Uy [m], Rz [rad]; Vx/Vy [m/s], VrZ [rad/s]; Ax/Ay [m/s²], ArZ [rad/s²]; force [kN], moment [kN*m].")
            .AppendLine("Energy and work are in kN*m, not kg*m²/s² (multiply by 1000 for J).")
            .AppendLine().AppendLine("## Energy diagnostic")
            .AppendLine("| Time [s] | Kinetic [kN*m] | Elastic [kN*m] | External work [kN*m] | Dissipated [kN*m] | Balance residual [kN*m] |")
            .AppendLine("|---:|---:|---:|---:|---:|---:|");
        int stepStride = Math.Max(1, (result.Steps.Count - 1) / 20);
        foreach (StructuralTimeStepResult step in result.Steps.Where((s, i) => i % stepStride == 0 || i == result.Steps.Count - 1))
            sb.Append("| ").Append(N(step.TimeSeconds)).Append(" | ")
                .Append(N(step.Energy.Kinetic)).Append(" | ").Append(N(step.Energy.Elastic))
                .Append(" | ").Append(N(step.Energy.ExternalWork)).Append(" | ")
                .Append(N(step.Energy.DissipatedEnergy)).Append(" | ")
                .Append(N(step.Energy.BalanceResidual)).AppendLine(" |");
        sb.AppendLine().AppendLine("## Absolute nodal envelopes")
            .AppendLine("| Node | max |Ux| [m] | max |Uy| [m] | max |Rz| [rad] | max |Vx| [m/s] | max |Vy| [m/s] | max |Ax| [m/s²] | max |Ay| [m/s²] |")
            .AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|");
        foreach (StructuralTimeNodeEnvelope env in result.Envelopes)
            sb.Append("| ").Append(env.NodeId.Replace("|", "\\|").Replace("\n", " "))
                .Append(" | ").Append(N(env.MaxAbsUx)).Append(" | ").Append(N(env.MaxAbsUy))
                .Append(" | ").Append(N(env.MaxAbsRz)).Append(" | ").Append(N(env.MaxAbsVx))
                .Append(" | ").Append(N(env.MaxAbsVy)).Append(" | ").Append(N(env.MaxAbsAx))
                .Append(" | ").Append(N(env.MaxAbsAy)).AppendLine(" |");
        sb.AppendLine().AppendLine("## Scope and limitations");
        foreach (string warning in result.Warnings) sb.Append("- ").AppendLine(warning);
        return sb.ToString();
    }
}
