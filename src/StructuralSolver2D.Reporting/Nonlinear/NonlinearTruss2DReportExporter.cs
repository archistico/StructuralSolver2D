using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.PublicApi;

namespace StructuralSolver2D.Reporting.Nonlinear;

/// <summary>M68 invariant research-only markdown and CSV reports, with explicit completion status.</summary>
public sealed class NonlinearTruss2DReportExporter
{
    private static string N(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
    private static string Csv(string value) => "\"" + (value.Length > 0 && "=+-@\t\r".Contains(value[0]) ? "'" : "") +
        value.Replace("\"", "\"\"") + "\"";
    private static string Md(string value) => value.Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|");
    private static void Start(StringBuilder sb, string? title, bool completed, string? failure, string method)
    {
        sb.Append("# ").AppendLine(Md(title ?? "Nonlinear Truss2D"))
            .AppendLine().Append("## M68 — ").AppendLine(method)
            .AppendLine("Research-only, small strains, linear isotropic hardening, no geometric nonlinearity or collapse checks.")
            .AppendLine("Units: kN, m, s; axial forces/reactions in kN; plastic strains dimensionless; energy in kN*m.")
            .Append("**Analysis completed:** ").AppendLine(completed ? "YES" : "NO — partial accepted history only");
        if (!completed) sb.Append("**Failure:** ").AppendLine(Md(failure ?? "Unspecified failure"));
    }

    public string StaticMarkdown(StructuralNonlinearStaticOutput output, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        var sb = new StringBuilder();
        Start(sb, title, output.Completed, output.Failure, "Incremental static elastoplastic Truss2D");
        sb.AppendLine().AppendLine("| Accepted step | Load factor | Newton iterations | Residual [kN] | Plastic dissipation accumulated [kN*m] |")
            .AppendLine("|---:|---:|---:|---:|---:|");
        foreach (var s in output.Steps)
            sb.Append("| ").Append(s.Index).Append(" | ").Append(N(s.LoadFactor)).Append(" | ")
                .Append(s.NewtonIterations).Append(" | ").Append(N(s.ResidualNorm)).Append(" | ")
                .Append(N(s.CumulativePlasticDissipation)).AppendLine(" |");
        if (output.Steps.Count > 0)
        {
            var last = output.Steps[^1];
            sb.AppendLine().AppendLine("## Final accepted nodal response")
                .AppendLine("| Node | Ux [m] | Uy [m] | Reaction Fx [kN] | Reaction Fy [kN] |")
                .AppendLine("|---|---:|---:|---:|---:|");
            for (int i = 0; i < output.NodeIds.Count; i++)
                sb.Append("| ").Append(Md(output.NodeIds[i])).Append(" | ")
                    .Append(N(last.Displacements[2*i])).Append(" | ").Append(N(last.Displacements[2*i+1]))
                    .Append(" | ").Append(N(last.Reactions[2*i])).Append(" | ")
                    .Append(N(last.Reactions[2*i+1])).AppendLine(" |");
            sb.AppendLine().AppendLine("## Final accepted member states")
                .AppendLine("| Member | Axial force [kN] | Plastic strain | Accumulated plastic strain |")
                .AppendLine("|---|---:|---:|---:|");
            for (int i = 0; i < output.MemberIds.Count; i++)
                sb.Append("| ").Append(Md(output.MemberIds[i])).Append(" | ")
                    .Append(N(last.AxialForces[i])).Append(" | ")
                    .Append(N(last.MaterialStates[i].PlasticStrain)).Append(" | ")
                    .Append(N(last.MaterialStates[i].AccumulatedPlasticStrain)).AppendLine(" |");
        }
        sb.AppendLine().AppendLine("Member forces and plastic states are also available in the CSV export.")
            .AppendLine("Unaccepted Newton trials are never exported as converged results.");
        return sb.ToString();
    }

    public string TimeMarkdown(StructuralNonlinearTimeOutput output, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        var sb = new StringBuilder();
        Start(sb, title, output.Completed, output.Failure, "Newmark–Newton elastoplastic Truss2D time history");
        sb.AppendLine().AppendLine("| Accepted step | Time [s] | Load factor | Iterations | Residual [kN] | Plastic dissipation [kN*m] | Physical energy residual [kN*m] |")
            .AppendLine("|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var s in output.Steps)
            sb.Append("| ").Append(s.Index).Append(" | ").Append(N(s.Time)).Append(" | ")
                .Append(N(s.LoadFactor)).Append(" | ").Append(s.NewtonIterations).Append(" | ")
                .Append(N(s.ReducedResidualNorm)).Append(" | ")
                .Append(N(s.CumulativePlasticDissipation)).Append(" | ")
                .Append(N(s.PhysicalEnergyResidual)).AppendLine(" |");
        if (output.Steps.Count > 0)
        {
            var last = output.Steps[^1];
            sb.AppendLine().AppendLine("## Last accepted energy state")
                .Append("Kinetic energy [kN*m]: ").AppendLine(N(last.KineticEnergy))
                .Append("Recoverable energy [kN*m]: ").AppendLine(N(last.RecoverableEnergy))
                .Append("Hardening energy [kN*m]: ").AppendLine(N(last.HardeningEnergy))
                .Append("Viscous dissipation [kN*m]: ").AppendLine(N(last.CumulativeViscousDissipation))
                .Append("Plastic dissipation [kN*m]: ").AppendLine(N(last.CumulativePlasticDissipation));
        }
        sb.AppendLine().AppendLine("Nodal displacement/velocity/acceleration, reactions and element histories are in the CSV export.")
            .AppendLine("No Frame2D bending, code checks or member failure assessment.");
        return sb.ToString();
    }

    public string StaticNodesCsv(StructuralNonlinearStaticOutput o)
    {
        ArgumentNullException.ThrowIfNull(o);
        var sb = new StringBuilder("Step,LoadFactor,NodeId,Ux_m,Uy_m,ReactionFx_kN,ReactionFy_kN\n");
        foreach (var s in o.Steps)
            for (int i = 0; i < o.NodeIds.Count; i++)
                sb.Append(s.Index).Append(',').Append(N(s.LoadFactor)).Append(',').Append(Csv(o.NodeIds[i])).Append(',')
                    .Append(N(s.Displacements[2 * i])).Append(',').Append(N(s.Displacements[2 * i + 1])).Append(',')
                    .Append(N(s.Reactions[2 * i])).Append(',').Append(N(s.Reactions[2 * i + 1])).Append('\n');
        return sb.ToString();
    }

    public string StaticMembersCsv(StructuralNonlinearStaticOutput o)
    {
        ArgumentNullException.ThrowIfNull(o);
        var sb = new StringBuilder("Step,LoadFactor,MemberId,AxialForce_kN,PlasticStrain,AccumulatedPlasticStrain,StepGlobalPlasticDissipation_kNm,AccumulatedGlobalPlasticDissipation_kNm\n");
        foreach (var s in o.Steps)
            for (int i = 0; i < o.MemberIds.Count; i++)
                sb.Append(s.Index).Append(',').Append(N(s.LoadFactor)).Append(',').Append(Csv(o.MemberIds[i])).Append(',')
                    .Append(N(s.AxialForces[i])).Append(',').Append(N(s.MaterialStates[i].PlasticStrain)).Append(',')
                    .Append(N(s.MaterialStates[i].AccumulatedPlasticStrain)).Append(',')
                    .Append(N(s.IncrementalPlasticDissipation)).Append(',').Append(N(s.CumulativePlasticDissipation)).Append('\n');
        return sb.ToString();
    }

    public string TimeNodesCsv(StructuralNonlinearTimeOutput o)
    {
        ArgumentNullException.ThrowIfNull(o);
        var sb = new StringBuilder("Step,Time_s,LoadFactor,NodeId,Ux_m,Uy_m,Vx_m_s,Vy_m_s,Ax_m_s2,Ay_m_s2,ReactionFx_kN,ReactionFy_kN\n");
        foreach (var s in o.Steps)
            for (int i = 0; i < o.NodeIds.Count; i++)
                sb.Append(s.Index).Append(',').Append(N(s.Time)).Append(',').Append(N(s.LoadFactor)).Append(',')
                    .Append(Csv(o.NodeIds[i])).Append(',')
                    .Append(string.Join(",", new[]{s.Displacements[2*i],s.Displacements[2*i+1],
                        s.Velocities[2*i],s.Velocities[2*i+1],s.Accelerations[2*i],s.Accelerations[2*i+1],
                        s.Reactions[2*i],s.Reactions[2*i+1]}.Select(N))).Append('\n');
        return sb.ToString();
    }

    public string TimeMembersCsv(StructuralNonlinearTimeOutput o)
    {
        ArgumentNullException.ThrowIfNull(o);
        var sb = new StringBuilder("Step,Time_s,MemberId,AxialForce_kN,PlasticStrain,AccumulatedPlasticStrain,CumulativeGlobalPlasticDissipation_kNm,DiscreteWorkResidual_kNm,PhysicalEnergyResidual_kNm\n");
        foreach (var s in o.Steps)
            for (int i = 0; i < o.MemberIds.Count; i++)
                sb.Append(s.Index).Append(',').Append(N(s.Time)).Append(',').Append(Csv(o.MemberIds[i])).Append(',')
                    .Append(N(s.AxialForces[i])).Append(',').Append(N(s.MaterialStates[i].PlasticStrain)).Append(',')
                    .Append(N(s.MaterialStates[i].AccumulatedPlasticStrain)).Append(',')
                    .Append(N(s.CumulativePlasticDissipation)).Append(',').Append(N(s.DiscreteWorkResidual)).Append(',')
                    .Append(N(s.PhysicalEnergyResidual)).Append('\n');
        return sb.ToString();
    }
}
