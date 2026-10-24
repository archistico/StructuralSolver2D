using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;

namespace StructuralSolver2D.Reporting.Envelope;

/// <summary>
/// Portable, non-normative exports of M79 global N/V/M extrema. One row per
/// member, quantity and extreme type, with provenance of the governing scenario.
/// </summary>
public sealed class StaticForceEnvelopeReportM79
{
    private static readonly InternalForceQuantity[] Quantities =
    {
        InternalForceQuantity.NormalForce,
        InternalForceQuantity.ShearForce,
        InternalForceQuantity.BendingMoment,
    };

    private static string Number(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string Csv(string input)
    {
        // Escape spreadsheet formulas in user-authored identifiers while preserving CSV syntax.
        bool unsafeStart = input.Length > 0 && "=+-@\t\r\n".Contains(input[0]);
        string content = (unsafeStart ? "'" : "") + input;
        return "\"" + content.Replace("\"", "\"\"") + "\"";
    }

    private static string Markdown(string input) =>
        input.Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|");

    public string ToCsv(StructuralStaticForceEnvelopeM79 envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        StringBuilder sb = new("MemberId,Quantity,Extreme,Value,Unit,Distance_m,Position,Side,TargetKind,ResultId\n");
        foreach (FrameMemberForceEnvelopeM79 member in envelope.Members)
            foreach (InternalForceQuantity quantity in Quantities)
            {
                AppendRow(sb, "Minimum", member.Minimum(quantity));
                AppendRow(sb, "Maximum", member.Maximum(quantity));
                AppendRow(sb, "MaximumAbsolute", member.MaximumAbsolute(quantity));
            }
        return sb.ToString();
    }

    public string ToMarkdown(StructuralStaticForceEnvelopeM79 envelope, string title = "Inviluppo N/V/M")
    {
        ArgumentNullException.ThrowIfNull(envelope);
        StringBuilder sb = new();
        sb.Append("# ").AppendLine(Markdown(title)).AppendLine()
            .AppendLine("M79 — estremi globali analitici Frame2D, statica elastica lineare di primo ordine.")
            .AppendLine("Non sono curve di inviluppo lungo tutta l'asta, combinazioni normative SLU/SLE o verifiche di resistenza.")
            .AppendLine("N e V in kN; M in kNm; ascisse in metri. Valori firmati.")
            .AppendLine()
            .AppendLine("| Asta | Grandezza | Estremo | Valore | Unita | x [m] | Lato | Tipo | Caso/Combinazione |")
            .AppendLine("|---|---|---|---:|---|---:|---|---|---|");
        foreach (FrameMemberForceEnvelopeM79 member in envelope.Members)
            foreach (InternalForceQuantity quantity in Quantities)
            {
                AppendMdRow(sb, "Min", member.Minimum(quantity));
                AppendMdRow(sb, "Max", member.Maximum(quantity));
                AppendMdRow(sb, "Max abs", member.MaximumAbsolute(quantity));
            }
        sb.AppendLine().AppendLine("## Scenari confrontati");
        foreach (StaticForceEnvelopeScenarioM79 scenario in envelope.Scenarios)
            sb.Append("- ").Append(scenario.Kind).Append(": ").AppendLine(Markdown(scenario.Id));
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, string extremeType, GoverningForceExtremeM79 e)
    {
        sb.Append(Csv(e.MemberId)).Append(',').Append(e.Quantity).Append(',')
            .Append(extremeType).Append(',').Append(Number(e.Value)).Append(',')
            .Append(e.Quantity == InternalForceQuantity.BendingMoment ? "kNm" : "kN")
            .Append(',').Append(Number(e.Distance)).Append(',').Append(Number(e.Position)).Append(',')
            .Append(e.Side).Append(',').Append(e.TargetKind).Append(',').Append(Csv(e.ResultId)).Append('\n');
    }

    private static void AppendMdRow(StringBuilder sb, string extremeType, GoverningForceExtremeM79 e)
    {
        sb.Append("| ").Append(Markdown(e.MemberId)).Append(" | ").Append(e.Quantity)
            .Append(" | ").Append(extremeType).Append(" | ").Append(Number(e.Value)).Append(" | ")
            .Append(e.Quantity == InternalForceQuantity.BendingMoment ? "kNm" : "kN")
            .Append(" | ").Append(Number(e.Distance)).Append(" | ").Append(e.Side)
            .Append(" | ").Append(e.TargetKind).Append(" | ").Append(Markdown(e.ResultId))
            .AppendLine(" |");
    }
}
