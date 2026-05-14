using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Reporting.Pdf;

/// <summary>
/// Exports a compact technical structural analysis report as a self-contained PDF file.
/// The implementation writes a minimal PDF document directly and does not require external PDF libraries.
/// </summary>
public sealed class PdfTechnicalReportExporter
{
    private const double PageWidth = 595.0;
    private const double PageHeight = 842.0;
    private const double MarginLeft = 48.0;
    private const double MarginRight = 48.0;
    private const double MarginTop = 54.0;
    private const double MarginBottom = 48.0;
    private const double LineHeight = 13.0;
    private const int BodyWrapLength = 94;
    private const int TableWrapLength = 112;

    /// <summary>
    /// Exports the supplied model and analysis results to PDF bytes.
    /// </summary>
    public byte[] Export(
        StructuralModel model,
        StructuralAnalysisResult result,
        IReadOnlyList<MemberInternalForceDiagram> internalForceDiagrams,
        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams,
        StructuralAnalysisSummary summary,
        PdfTechnicalReportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(internalForceDiagrams);
        ArgumentNullException.ThrowIfNull(displacementDiagrams);
        ArgumentNullException.ThrowIfNull(summary);

        options ??= new PdfTechnicalReportOptions();
        options.Validate();

        List<ReportLine> lines = BuildReportLines(model, result, internalForceDiagrams, displacementDiagrams, summary, options);
        List<string> pageStreams = Paginate(lines, options.Title);

        return BuildPdf(pageStreams);
    }

    private static List<ReportLine> BuildReportLines(
        StructuralModel model,
        StructuralAnalysisResult result,
        IReadOnlyList<MemberInternalForceDiagram> internalForceDiagrams,
        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams,
        StructuralAnalysisSummary summary,
        PdfTechnicalReportOptions options)
    {
        List<ReportLine> lines = new()
        {
            ReportLine.Title(options.Title),
        };

        if (!string.IsNullOrWhiteSpace(options.Description))
        {
            lines.AddRange(WrapBody(options.Description));
        }

        if (!string.IsNullOrWhiteSpace(options.SourceLabel))
        {
            lines.Add(ReportLine.Body($"Source: {options.SourceLabel}"));
        }

        lines.Add(ReportLine.Body($"Analysis id: {result.LoadCaseId}"));
        lines.Add(ReportLine.Body($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm zzz}"));
        lines.Add(ReportLine.Blank());

        lines.Add(ReportLine.Heading("Model overview"));
        lines.Add(ReportLine.Body($"Nodes: {model.Nodes.Count}"));
        lines.Add(ReportLine.Body($"Members: {model.Members.Count}"));
        lines.Add(ReportLine.Body($"Supports: {model.Supports.Count}"));
        lines.Add(ReportLine.Body($"Load cases: {model.LoadCases.Count}"));
        lines.Add(ReportLine.Body($"Load combinations: {model.LoadCombinations.Count}"));
        lines.Add(ReportLine.Body($"Loads: {model.Loads.Count}"));
        lines.Add(ReportLine.Blank());

        lines.Add(ReportLine.Heading("Assumptions and limitations"));
        lines.AddRange(WrapBody("First-order linear elastic 2D analysis. Values are reported in StructuralSolver2D internal units: displacements in m, rotations in rad, forces in kN and moments in kNm."));
        lines.AddRange(WrapBody("This PDF is a technical computation report. It is not a complete code-compliance document and does not replace engineering judgement or project-specific normative checks."));
        lines.Add(ReportLine.Blank());

        lines.Add(ReportLine.Heading("Executive summary"));
        AddSummaryRows(lines, summary);
        lines.Add(ReportLine.Blank());

        lines.Add(ReportLine.Heading("Nodal displacements"));
        lines.Add(ReportLine.Table("Node        Ux [m]          Uy [m]        Rz [rad]"));
        foreach (NodalDisplacementResult item in result.Displacements.Take(options.MaxRowsPerSection))
        {
            lines.Add(ReportLine.Table($"{item.NodeId,-8} {Format(item.Ux),14} {Format(item.Uy),14} {Format(item.Rz),14}"));
        }

        AddTruncationNotice(lines, result.Displacements.Count, options.MaxRowsPerSection);
        lines.Add(ReportLine.Blank());

        lines.Add(ReportLine.Heading("Support reactions"));
        lines.Add(ReportLine.Table("Support  Node        Fx [kN]        Fy [kN]       Mz [kNm]"));
        foreach (SupportReactionResult item in result.Reactions.Take(options.MaxRowsPerSection))
        {
            lines.Add(ReportLine.Table($"{item.SupportId,-8} {item.NodeId,-6} {Format(item.Fx),14} {Format(item.Fy),14} {Format(item.Mz),14}"));
        }

        AddTruncationNotice(lines, result.Reactions.Count, options.MaxRowsPerSection);
        lines.Add(ReportLine.Blank());

        lines.Add(ReportLine.Heading("Member end forces"));
        lines.Add(ReportLine.Table("Member      N1 [kN]       V1 [kN]      M1 [kNm]       N2 [kN]       V2 [kN]      M2 [kNm]"));
        foreach (MemberEndForceResult item in result.MemberEndForces.Take(options.MaxRowsPerSection))
        {
            lines.Add(ReportLine.Table($"{item.MemberId,-8} {Format(item.StartAxial),14} {Format(item.StartShear),14} {Format(item.StartMoment),14} {Format(item.EndAxial),14} {Format(item.EndShear),14} {Format(item.EndMoment),14}"));
        }

        AddTruncationNotice(lines, result.MemberEndForces.Count, options.MaxRowsPerSection);
        lines.Add(ReportLine.Blank());

        if (options.IncludeInternalForceSamples)
        {
            lines.Add(ReportLine.Heading("Internal-force samples"));
            lines.Add(ReportLine.Table("Member     Pos      x [m]        N [kN]        V [kN]       M [kNm]"));
            int written = 0;
            foreach (MemberInternalForceSample sample in internalForceDiagrams.SelectMany(diagram => diagram.Samples).Take(options.MaxRowsPerSection))
            {
                lines.Add(ReportLine.Table($"{sample.MemberId,-8} {Format(sample.Position),8} {Format(sample.Distance),10} {Format(sample.NormalForce),13} {Format(sample.ShearForce),13} {Format(sample.BendingMoment),13}"));
                written++;
            }

            int totalSamples = internalForceDiagrams.Sum(diagram => diagram.Samples.Count);
            AddTruncationNotice(lines, totalSamples, options.MaxRowsPerSection);
            if (written == 0)
            {
                lines.Add(ReportLine.Body("No internal-force samples available."));
            }

            lines.Add(ReportLine.Blank());
        }

        if (options.IncludeDisplacementSamples)
        {
            lines.Add(ReportLine.Heading("Displacement samples"));
            lines.Add(ReportLine.Table("Member     Pos      x [m]      GUx [m]      GUy [m]"));
            int written = 0;
            foreach (MemberDisplacementSample sample in displacementDiagrams.SelectMany(diagram => diagram.Samples).Take(options.MaxRowsPerSection))
            {
                lines.Add(ReportLine.Table($"{sample.MemberId,-8} {Format(sample.NormalizedPosition),8} {Format(sample.Distance),10} {Format(sample.GlobalUx),12} {Format(sample.GlobalUy),12}"));
                written++;
            }

            int totalSamples = displacementDiagrams.Sum(diagram => diagram.Samples.Count);
            AddTruncationNotice(lines, totalSamples, options.MaxRowsPerSection);
            if (written == 0)
            {
                lines.Add(ReportLine.Body("No displacement samples available."));
            }
        }

        return lines;
    }

    private static void AddSummaryRows(List<ReportLine> lines, StructuralAnalysisSummary summary)
    {
        lines.Add(ReportLine.Table("Metric                Entity/Member      Position       Distance          Value       Unit"));
        AddSummaryRow(lines, "Max |Ux|", summary.MaxAbsUx.EntityId, string.Empty, string.Empty, summary.MaxAbsUx.Value, "m");
        AddSummaryRow(lines, "Max |Uy|", summary.MaxAbsUy.EntityId, string.Empty, string.Empty, summary.MaxAbsUy.Value, "m");
        AddSummaryRow(lines, "Max |Rz|", summary.MaxAbsRz.EntityId, string.Empty, string.Empty, summary.MaxAbsRz.Value, "rad");
        AddSummaryRow(lines, "Max |Fx|", summary.MaxAbsReactionFx.EntityId, string.Empty, string.Empty, summary.MaxAbsReactionFx.Value, "kN");
        AddSummaryRow(lines, "Max |Fy|", summary.MaxAbsReactionFy.EntityId, string.Empty, string.Empty, summary.MaxAbsReactionFy.Value, "kN");
        AddSummaryRow(lines, "Max |Mz|", summary.MaxAbsReactionMz.EntityId, string.Empty, string.Empty, summary.MaxAbsReactionMz.Value, "kNm");
        AddSummaryRow(lines, "Max |N|", summary.MaxAbsNormalForce.MemberId, Format(summary.MaxAbsNormalForce.Position), Format(summary.MaxAbsNormalForce.Distance), summary.MaxAbsNormalForce.Value, "kN");
        AddSummaryRow(lines, "Max |V|", summary.MaxAbsShearForce.MemberId, Format(summary.MaxAbsShearForce.Position), Format(summary.MaxAbsShearForce.Distance), summary.MaxAbsShearForce.Value, "kN");
        AddSummaryRow(lines, "Max |M|", summary.MaxAbsBendingMoment.MemberId, Format(summary.MaxAbsBendingMoment.Position), Format(summary.MaxAbsBendingMoment.Distance), summary.MaxAbsBendingMoment.Value, "kNm");
    }

    private static void AddSummaryRow(
        List<ReportLine> lines,
        string metric,
        string entityOrMember,
        string position,
        string distance,
        double value,
        string unit)
    {
        lines.Add(ReportLine.Table($"{metric,-21} {entityOrMember,-16} {position,10} {distance,12} {Format(value),14} {unit,8}"));
    }

    private static void AddTruncationNotice(List<ReportLine> lines, int totalRows, int maxRowsPerSection)
    {
        if (totalRows > maxRowsPerSection)
        {
            lines.Add(ReportLine.Body($"Only the first {maxRowsPerSection} of {totalRows} rows are shown in this PDF. Use CSV/XLSX exports for full tabular data."));
        }
    }

    private static IEnumerable<ReportLine> WrapBody(string text)
    {
        foreach (string line in Wrap(text, BodyWrapLength))
        {
            yield return ReportLine.Body(line);
        }
    }

    private static List<string> Paginate(IReadOnlyList<ReportLine> lines, string documentTitle)
    {
        List<string> pageStreams = new();
        StringBuilder currentPage = new();
        int pageNumber = 1;
        double y = PageHeight - MarginTop;

        StartPage(currentPage, documentTitle, pageNumber);

        foreach (ReportLine line in lines)
        {
            if (y < MarginBottom + 24.0)
            {
                EndPage(currentPage, pageNumber);
                pageStreams.Add(currentPage.ToString());
                currentPage.Clear();
                pageNumber++;
                y = PageHeight - MarginTop;
                StartPage(currentPage, documentTitle, pageNumber);
            }

            foreach (string wrapped in Wrap(line.Text, line.Kind == ReportLineKind.Table ? TableWrapLength : BodyWrapLength))
            {
                AppendTextLine(currentPage, line with { Text = wrapped }, y);
                y -= GetLineHeight(line);
            }

            if (line.Kind is ReportLineKind.Title or ReportLineKind.Heading)
            {
                y -= 4.0;
            }
        }

        EndPage(currentPage, pageNumber);
        pageStreams.Add(currentPage.ToString());
        return pageStreams;
    }

    private static void StartPage(StringBuilder builder, string documentTitle, int pageNumber)
    {
        builder.AppendLine("0.92 0.96 1 rg");
        builder.AppendLine($"0 {FormatPdf(PageHeight - 34.0)} {FormatPdf(PageWidth)} 34 re f");
        builder.AppendLine("BT /F1 9 Tf 0.20 0.25 0.35 rg");
        builder.AppendLine($"{FormatPdf(MarginLeft)} {FormatPdf(PageHeight - 22.0)} Td ({EscapePdfText(documentTitle)}) Tj");
        builder.AppendLine("ET");
        builder.AppendLine("0.82 0.86 0.92 RG 0.5 w");
        builder.AppendLine($"{FormatPdf(MarginLeft)} {FormatPdf(PageHeight - 38.0)} m {FormatPdf(PageWidth - MarginRight)} {FormatPdf(PageHeight - 38.0)} l S");
    }

    private static void EndPage(StringBuilder builder, int pageNumber)
    {
        builder.AppendLine("0.82 0.86 0.92 RG 0.5 w");
        builder.AppendLine($"{FormatPdf(MarginLeft)} {FormatPdf(MarginBottom - 14.0)} m {FormatPdf(PageWidth - MarginRight)} {FormatPdf(MarginBottom - 14.0)} l S");
        builder.AppendLine("BT /F1 9 Tf 0.35 0.39 0.45 rg");
        builder.AppendLine($"{FormatPdf(PageWidth - MarginRight - 48.0)} {FormatPdf(MarginBottom - 30.0)} Td (Page {pageNumber}) Tj");
        builder.AppendLine("ET");
    }

    private static void AppendTextLine(StringBuilder builder, ReportLine line, double y)
    {
        string font = line.Kind == ReportLineKind.Table ? "/F2" : "/F1";
        double fontSize = line.Kind switch
        {
            ReportLineKind.Title => 18.0,
            ReportLineKind.Heading => 13.0,
            ReportLineKind.Table => 8.5,
            _ => 10.0,
        };

        string color = line.Kind switch
        {
            ReportLineKind.Title => "0.05 0.09 0.16 rg",
            ReportLineKind.Heading => "0.10 0.18 0.32 rg",
            ReportLineKind.Table => "0.10 0.10 0.10 rg",
            _ => "0.18 0.21 0.27 rg",
        };

        builder.AppendLine($"BT {font} {FormatPdf(fontSize)} Tf {color}");
        builder.AppendLine($"{FormatPdf(MarginLeft)} {FormatPdf(y)} Td ({EscapePdfText(line.Text)}) Tj");
        builder.AppendLine("ET");
    }

    private static double GetLineHeight(ReportLine line) =>
        line.Kind switch
        {
            ReportLineKind.Title => 22.0,
            ReportLineKind.Heading => 17.0,
            ReportLineKind.Table => 10.5,
            _ => LineHeight,
        };

    private static IEnumerable<string> Wrap(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield return string.Empty;
            yield break;
        }

        string remaining = text;
        while (remaining.Length > maxLength)
        {
            int splitAt = remaining.LastIndexOf(' ', maxLength);
            if (splitAt <= 0)
            {
                splitAt = maxLength;
            }

            yield return remaining[..splitAt].TrimEnd();
            remaining = remaining[splitAt..].TrimStart();
        }

        yield return remaining;
    }

    private static byte[] BuildPdf(IReadOnlyList<string> pageStreams)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
        };

        int font1Id = 3;
        int font2Id = 4;
        int firstPageObjectId = 5;
        int firstContentObjectId = firstPageObjectId + pageStreams.Count;
        string kids = string.Join(" ", Enumerable.Range(0, pageStreams.Count).Select(index => $"{firstPageObjectId + index} 0 R"));

        objects.Add($"<< /Type /Pages /Kids [{kids}] /Count {pageStreams.Count} >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");

        for (int index = 0; index < pageStreams.Count; index++)
        {
            int contentId = firstContentObjectId + index;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {FormatPdf(PageWidth)} {FormatPdf(PageHeight)}] /Resources << /Font << /F1 {font1Id} 0 R /F2 {font2Id} 0 R >> >> /Contents {contentId} 0 R >>");
        }

        foreach (string pageStream in pageStreams)
        {
            int length = Encoding.ASCII.GetByteCount(pageStream);
            objects.Add($"<< /Length {length} >>\nstream\n{pageStream}endstream");
        }

        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n%\u00E2\u00E3\u00CF\u00D3\n");
        var offsets = new List<long> { 0 };

        for (int index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        long xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        for (int index = 1; index < offsets.Count; index++)
        {
            WriteAscii(stream, $"{offsets[index]:0000000000} 00000 n \n");
        }

        WriteAscii(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string EscapePdfText(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char character in value)
        {
            builder.Append(character switch
            {
                '(' => "\\(",
                ')' => "\\)",
                '\\' => "\\\\",
                >= ' ' and <= '~' => character.ToString(),
                _ => "?",
            });
        }

        return builder.ToString();
    }

    private static string Format(double value) =>
        value.ToString("0.000000", CultureInfo.InvariantCulture);

    private static string FormatPdf(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private sealed record ReportLine(string Text, ReportLineKind Kind)
    {
        public static ReportLine Title(string text) => new(text, ReportLineKind.Title);

        public static ReportLine Heading(string text) => new(text, ReportLineKind.Heading);

        public static ReportLine Body(string text) => new(text, ReportLineKind.Body);

        public static ReportLine Table(string text) => new(text, ReportLineKind.Table);

        public static ReportLine Blank() => new(string.Empty, ReportLineKind.Body);
    }

    private enum ReportLineKind
    {
        Title,
        Heading,
        Body,
        Table,
    }
}
