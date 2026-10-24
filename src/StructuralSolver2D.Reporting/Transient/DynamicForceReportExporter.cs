using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.PublicApi;

namespace StructuralSolver2D.Reporting.Transient;

/// <summary>Invariant, opt-in M67 LINEAR force-history report/export (not section force diagrams).</summary>
public sealed class DynamicForceReportExporter
{
    private static string N(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
    private static string Csv(string s) => "\"" + (s.Length > 0 && "=+-@\t\r".Contains(s[0]) ? "'" : "") + s.Replace("\"", "\"\"") + "\"";
    private static string Md(string s) => s.Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|");

    public string ExportReactionCsv(StructuralDynamicForceRecoveryOutput result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var sb = new StringBuilder("TimeSeconds,Step,NodeId,ReactionFx_kN,ReactionFy_kN,ReactionMz_kNm,AdmissibleResidual\n");
        foreach (var step in result.Steps)
            foreach (var reaction in step.SupportNodeReactions)
                sb.Append(N(step.TimeSeconds)).Append(',').Append(step.StepIndex).Append(',')
                    .Append(Csv(reaction.NodeId)).Append(',')
                    .Append(string.Join(",", new[]{reaction.Fx,reaction.Fy,reaction.Mz,step.MaxAdmissibleEquilibriumResidual}.Select(N)))
                    .Append('\n');
        return sb.ToString();
    }

    public string ExportMemberCsv(StructuralDynamicForceRecoveryOutput result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var sb = new StringBuilder("TimeSeconds,Step,MemberId,ElasticN1_kN,ElasticV1_kN,ElasticM1_kNm,ElasticN2_kN,ElasticV2_kN,ElasticM2_kNm\n");
        foreach (var step in result.Steps)
            foreach (var member in step.MemberElasticEndForces)
                sb.Append(N(step.TimeSeconds)).Append(',').Append(step.StepIndex).Append(',')
                    .Append(Csv(member.MemberId)).Append(',')
                    .Append(string.Join(",",new[]{member.StartAxial,member.StartShear,member.StartMoment,
                        member.EndAxial,member.EndShear,member.EndMoment}.Select(N)))
                    .Append('\n');
        return sb.ToString();
    }

    public string ExportMarkdown(StructuralDynamicForceRecoveryOutput result, string? title)
    {
        ArgumentNullException.ThrowIfNull(result);
        var sb = new StringBuilder("# ").AppendLine(Md(title ?? "Dynamic member actions"))
            .AppendLine().AppendLine("## M67 — Linear dynamic support-node reactions and ELASTIC member end actions")
            .AppendLine("Units: Fx/Fy/N/V in kN; Mz/member end moments in kN*m; time in seconds.")
            .AppendLine("Net reactions follow the global residual M*a + C*v + K*u - F; they are not split between redundant supports.")
            .AppendLine("Member end actions K_local*u_local do NOT include distributed inertia/damping contributions to section diagrams.")
            .AppendLine().Append("Maximum reduced equilibrium residual [generalized force units]: ")
            .AppendLine(N(result.MaximumAdmissibleEquilibriumResidual))
            .AppendLine().AppendLine("## Maximum absolute nodal support resultants")
            .AppendLine("| Node | max abs(Fx) [kN] | t [s] | max abs(Fy) [kN] | t [s] | max abs(Mz) [kNm] | t [s] |")
            .AppendLine("|---|---:|---:|---:|---:|---:|---:|");
        foreach (var r in result.ReactionEnvelopes)
            sb.Append("| ").Append(Md(r.NodeId)).Append(" | ").Append(N(r.Fx.MaxAbsolute)).Append(" | ")
                .Append(N(r.Fx.TimeSeconds)).Append(" | ").Append(N(r.Fy.MaxAbsolute)).Append(" | ")
                .Append(N(r.Fy.TimeSeconds)).Append(" | ").Append(N(r.Mz.MaxAbsolute)).Append(" | ")
                .Append(N(r.Mz.TimeSeconds)).AppendLine(" |");
        sb.AppendLine().AppendLine("## Maximum absolute local ELASTIC member end actions")
            .AppendLine("| Member | max abs(N1) [kN] | max abs(V1) [kN] | max abs(M1) [kNm] | max abs(N2) [kN] | max abs(V2) [kN] | max abs(M2) [kNm] |")
            .AppendLine("|---|---:|---:|---:|---:|---:|---:|");
        foreach (var m in result.MemberEnvelopes)
            sb.Append("| ").Append(Md(m.MemberId)).Append(" | ")
                .Append(string.Join(" | ", new[]{m.StartAxial.MaxAbsolute,m.StartShear.MaxAbsolute,
                    m.StartMoment.MaxAbsolute,m.EndAxial.MaxAbsolute,m.EndShear.MaxAbsolute,
                    m.EndMoment.MaxAbsolute}.Select(N))).AppendLine(" |");
        sb.AppendLine().AppendLine("## Assumptions and limitations");
        foreach (string warning in result.Warnings) sb.Append("- ").AppendLine(warning);
        return sb.ToString();
    }
}
