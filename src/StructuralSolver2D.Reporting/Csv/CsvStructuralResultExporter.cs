using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.Results;

namespace StructuralSolver2D.Reporting.Csv;

/// <summary>
/// Exports structural analysis results to small, spreadsheet-friendly CSV tables.
/// </summary>
public sealed class CsvStructuralResultExporter
{
    /// <summary>
    /// Exports nodal displacements.
    /// </summary>
    public string ExportNodalDisplacements(StructuralAnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        WriteRow(builder, "LoadCaseId", "NodeId", "Ux_m", "Uy_m", "Rz_rad");

        foreach (NodalDisplacementResult displacement in result.Displacements)
        {
            WriteRow(
                builder,
                result.LoadCaseId,
                displacement.NodeId,
                Format(displacement.Ux),
                Format(displacement.Uy),
                Format(displacement.Rz));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Exports support reactions.
    /// </summary>
    public string ExportSupportReactions(StructuralAnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        WriteRow(builder, "LoadCaseId", "SupportId", "NodeId", "Fx_kN", "Fy_kN", "Mz_kNm");

        foreach (SupportReactionResult reaction in result.Reactions)
        {
            WriteRow(
                builder,
                result.LoadCaseId,
                reaction.SupportId,
                reaction.NodeId,
                Format(reaction.Fx),
                Format(reaction.Fy),
                Format(reaction.Mz));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Exports local member end forces.
    /// </summary>
    public string ExportMemberEndForces(StructuralAnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        WriteRow(builder, "LoadCaseId", "MemberId", "StartAxial_kN", "StartShear_kN", "StartMoment_kNm", "EndAxial_kN", "EndShear_kN", "EndMoment_kNm");

        foreach (MemberEndForceResult force in result.MemberEndForces)
        {
            WriteRow(
                builder,
                result.LoadCaseId,
                force.MemberId,
                Format(force.StartAxial),
                Format(force.StartShear),
                Format(force.StartMoment),
                Format(force.EndAxial),
                Format(force.EndShear),
                Format(force.EndMoment));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Exports sampled member internal-force diagrams.
    /// </summary>
    public string ExportInternalForceSamples(
        string analysisId,
        IReadOnlyList<MemberInternalForceDiagram> diagrams)
    {
        ArgumentNullException.ThrowIfNull(diagrams);

        var builder = new StringBuilder();
        WriteRow(builder, "AnalysisId", "MemberId", "Position", "Distance_m", "NormalForce_kN", "ShearForce_kN", "BendingMoment_kNm");

        foreach (MemberInternalForceDiagram diagram in diagrams)
        {
            foreach (MemberInternalForceSample sample in diagram.Samples)
            {
                WriteRow(
                    builder,
                    analysisId,
                    sample.MemberId,
                    Format(sample.Position),
                    Format(sample.Distance),
                    Format(sample.NormalForce),
                    Format(sample.ShearForce),
                    Format(sample.BendingMoment));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Exports sampled member displacement/deformed-shape diagrams.
    /// </summary>
    public string ExportDisplacementSamples(
        string analysisId,
        IReadOnlyList<MemberDisplacementDiagram> diagrams)
    {
        ArgumentNullException.ThrowIfNull(diagrams);

        var builder = new StringBuilder();
        WriteRow(builder, "AnalysisId", "MemberId", "Position", "Distance_m", "LocalUx_m", "LocalUy_m", "LocalRz_rad", "GlobalUx_m", "GlobalUy_m");

        foreach (MemberDisplacementDiagram diagram in diagrams)
        {
            foreach (MemberDisplacementSample sample in diagram.Samples)
            {
                WriteRow(
                    builder,
                    analysisId,
                    sample.MemberId,
                    Format(sample.NormalizedPosition),
                    Format(sample.Distance),
                    Format(sample.LocalUx),
                    Format(sample.LocalUy),
                    Format(sample.LocalRz),
                    Format(sample.GlobalUx),
                    Format(sample.GlobalUy));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Exports compact governing result values.
    /// </summary>
    public string ExportSummary(StructuralAnalysisSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var builder = new StringBuilder();
        WriteRow(builder, "AnalysisId", "Quantity", "EntityId", "MemberId", "Position", "Distance_m", "Value", "Unit");

        WriteResultExtreme(builder, summary.LoadCaseId, "MaxAbsUx", summary.MaxAbsUx, "m");
        WriteResultExtreme(builder, summary.LoadCaseId, "MaxAbsUy", summary.MaxAbsUy, "m");
        WriteResultExtreme(builder, summary.LoadCaseId, "MaxAbsRz", summary.MaxAbsRz, "rad");
        WriteResultExtreme(builder, summary.LoadCaseId, "MaxAbsReactionFx", summary.MaxAbsReactionFx, "kN");
        WriteResultExtreme(builder, summary.LoadCaseId, "MaxAbsReactionFy", summary.MaxAbsReactionFy, "kN");
        WriteResultExtreme(builder, summary.LoadCaseId, "MaxAbsReactionMz", summary.MaxAbsReactionMz, "kNm");
        WriteInternalForceExtreme(builder, summary.LoadCaseId, "MaxAbsNormalForce", summary.MaxAbsNormalForce, "kN");
        WriteInternalForceExtreme(builder, summary.LoadCaseId, "MaxAbsShearForce", summary.MaxAbsShearForce, "kN");
        WriteInternalForceExtreme(builder, summary.LoadCaseId, "MaxAbsBendingMoment", summary.MaxAbsBendingMoment, "kNm");

        return builder.ToString();
    }

    private static void WriteResultExtreme(
        StringBuilder builder,
        string analysisId,
        string quantity,
        AnalysisResultExtreme extreme,
        string unit) =>
        WriteRow(
            builder,
            analysisId,
            quantity,
            extreme.EntityId,
            string.Empty,
            string.Empty,
            string.Empty,
            Format(extreme.Value),
            unit);

    private static void WriteInternalForceExtreme(
        StringBuilder builder,
        string analysisId,
        string quantity,
        InternalForceExtreme extreme,
        string unit) =>
        WriteRow(
            builder,
            analysisId,
            quantity,
            string.Empty,
            extreme.MemberId,
            Format(extreme.Position),
            Format(extreme.Distance),
            Format(extreme.Value),
            unit);

    private static void WriteRow(StringBuilder builder, params string[] values)
    {
        builder.AppendLine(string.Join(",", values.Select(Escape)));
    }

    private static string Format(double value) =>
        value.ToString("0.000000", CultureInfo.InvariantCulture);

    private static string Escape(string value)
    {
        if (value.Length == 0)
        {
            return string.Empty;
        }

        bool mustQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!mustQuote)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
