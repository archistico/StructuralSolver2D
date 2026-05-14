using System.Globalization;
using System.IO.Compression;
using System.Text;
using StructuralSolver2D.Analysis.Results;

namespace StructuralSolver2D.Reporting.Xlsx;

/// <summary>
/// Exports structural analysis results to a lightweight XLSX workbook.
/// The implementation writes a minimal OpenXML package directly and does not require external spreadsheet libraries.
/// </summary>
public sealed class XlsxStructuralResultExporter
{
    /// <summary>
    /// Exports the supplied analysis result bundle to an XLSX workbook.
    /// </summary>
    /// <param name="result">Primary solver result.</param>
    /// <param name="internalForceDiagrams">Sampled internal-force diagrams.</param>
    /// <param name="displacementDiagrams">Sampled displacement diagrams.</param>
    /// <param name="summary">Compact governing result summary.</param>
    /// <returns>XLSX file content as bytes.</returns>
    public byte[] Export(
        StructuralAnalysisResult result,
        IReadOnlyList<MemberInternalForceDiagram> internalForceDiagrams,
        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams,
        StructuralAnalysisSummary summary)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(internalForceDiagrams);
        ArgumentNullException.ThrowIfNull(displacementDiagrams);
        ArgumentNullException.ThrowIfNull(summary);

        List<WorksheetData> worksheets = new()
        {
            BuildSummaryWorksheet(summary),
            BuildNodalDisplacementsWorksheet(result),
            BuildSupportReactionsWorksheet(result),
            BuildMemberEndForcesWorksheet(result),
            BuildInternalForceSamplesWorksheet(result.LoadCaseId, internalForceDiagrams),
            BuildDisplacementSamplesWorksheet(result.LoadCaseId, displacementDiagrams),
        };

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddTextEntry(archive, "[Content_Types].xml", BuildContentTypes(worksheets.Count));
            AddTextEntry(archive, "_rels/.rels", BuildRootRelationships());
            AddTextEntry(archive, "xl/workbook.xml", BuildWorkbook(worksheets));
            AddTextEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationships(worksheets.Count));
            AddTextEntry(archive, "xl/styles.xml", BuildStyles());

            for (int index = 0; index < worksheets.Count; index++)
            {
                AddTextEntry(
                    archive,
                    $"xl/worksheets/sheet{index + 1}.xml",
                    BuildWorksheetXml(worksheets[index]));
            }
        }

        return stream.ToArray();
    }

    private static WorksheetData BuildSummaryWorksheet(StructuralAnalysisSummary summary)
    {
        var rows = new List<IReadOnlyList<CellValue>>
        {
            Row("Metric", "Entity", "Member", "Position", "Distance_m", "Value", "Unit"),
            Row("Analysis id", summary.LoadCaseId, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
            Row("Max |Ux|", summary.MaxAbsUx.EntityId, string.Empty, string.Empty, string.Empty, summary.MaxAbsUx.Value, "m"),
            Row("Max |Uy|", summary.MaxAbsUy.EntityId, string.Empty, string.Empty, string.Empty, summary.MaxAbsUy.Value, "m"),
            Row("Max |Rz|", summary.MaxAbsRz.EntityId, string.Empty, string.Empty, string.Empty, summary.MaxAbsRz.Value, "rad"),
            Row("Max |Fx|", summary.MaxAbsReactionFx.EntityId, string.Empty, string.Empty, string.Empty, summary.MaxAbsReactionFx.Value, "kN"),
            Row("Max |Fy|", summary.MaxAbsReactionFy.EntityId, string.Empty, string.Empty, string.Empty, summary.MaxAbsReactionFy.Value, "kN"),
            Row("Max |Mz|", summary.MaxAbsReactionMz.EntityId, string.Empty, string.Empty, string.Empty, summary.MaxAbsReactionMz.Value, "kNm"),
            Row("Max |N|", string.Empty, summary.MaxAbsNormalForce.MemberId, summary.MaxAbsNormalForce.Position, summary.MaxAbsNormalForce.Distance, summary.MaxAbsNormalForce.Value, "kN"),
            Row("Max |V|", string.Empty, summary.MaxAbsShearForce.MemberId, summary.MaxAbsShearForce.Position, summary.MaxAbsShearForce.Distance, summary.MaxAbsShearForce.Value, "kN"),
            Row("Max |M|", string.Empty, summary.MaxAbsBendingMoment.MemberId, summary.MaxAbsBendingMoment.Position, summary.MaxAbsBendingMoment.Distance, summary.MaxAbsBendingMoment.Value, "kNm"),
        };

        return new WorksheetData("Summary", rows);
    }

    private static WorksheetData BuildNodalDisplacementsWorksheet(StructuralAnalysisResult result)
    {
        var rows = new List<IReadOnlyList<CellValue>>
        {
            Row("NodeId", "Ux_m", "Uy_m", "Rz_rad"),
        };

        rows.AddRange(result.Displacements.Select(displacement => Row(
            displacement.NodeId,
            displacement.Ux,
            displacement.Uy,
            displacement.Rz)));

        return new WorksheetData("Nodal displacements", rows);
    }

    private static WorksheetData BuildSupportReactionsWorksheet(StructuralAnalysisResult result)
    {
        var rows = new List<IReadOnlyList<CellValue>>
        {
            Row("SupportId", "NodeId", "Fx_kN", "Fy_kN", "Mz_kNm"),
        };

        rows.AddRange(result.Reactions.Select(reaction => Row(
            reaction.SupportId,
            reaction.NodeId,
            reaction.Fx,
            reaction.Fy,
            reaction.Mz)));

        return new WorksheetData("Support reactions", rows);
    }

    private static WorksheetData BuildMemberEndForcesWorksheet(StructuralAnalysisResult result)
    {
        var rows = new List<IReadOnlyList<CellValue>>
        {
            Row("MemberId", "StartAxial_kN", "StartShear_kN", "StartMoment_kNm", "EndAxial_kN", "EndShear_kN", "EndMoment_kNm"),
        };

        rows.AddRange(result.MemberEndForces.Select(force => Row(
            force.MemberId,
            force.StartAxial,
            force.StartShear,
            force.StartMoment,
            force.EndAxial,
            force.EndShear,
            force.EndMoment)));

        return new WorksheetData("Member end forces", rows);
    }

    private static WorksheetData BuildInternalForceSamplesWorksheet(
        string analysisId,
        IReadOnlyList<MemberInternalForceDiagram> diagrams)
    {
        var rows = new List<IReadOnlyList<CellValue>>
        {
            Row("AnalysisId", "MemberId", "Position", "Distance_m", "NormalForce_kN", "ShearForce_kN", "BendingMoment_kNm"),
        };

        foreach (MemberInternalForceDiagram diagram in diagrams)
        {
            rows.AddRange(diagram.Samples.Select(sample => Row(
                analysisId,
                sample.MemberId,
                sample.Position,
                sample.Distance,
                sample.NormalForce,
                sample.ShearForce,
                sample.BendingMoment)));
        }

        return new WorksheetData("Internal force samples", rows);
    }

    private static WorksheetData BuildDisplacementSamplesWorksheet(
        string analysisId,
        IReadOnlyList<MemberDisplacementDiagram> diagrams)
    {
        var rows = new List<IReadOnlyList<CellValue>>
        {
            Row("AnalysisId", "MemberId", "Position", "Distance_m", "LocalUx_m", "LocalUy_m", "LocalRz_rad", "GlobalUx_m", "GlobalUy_m"),
        };

        foreach (MemberDisplacementDiagram diagram in diagrams)
        {
            rows.AddRange(diagram.Samples.Select(sample => Row(
                analysisId,
                sample.MemberId,
                sample.NormalizedPosition,
                sample.Distance,
                sample.LocalUx,
                sample.LocalUy,
                sample.LocalRz,
                sample.GlobalUx,
                sample.GlobalUy)));
        }

        return new WorksheetData("Displacement samples", rows);
    }

    private static string BuildWorksheetXml(WorksheetData worksheet)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        builder.AppendLine("  <sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
        builder.AppendLine("  <sheetData>");

        for (int rowIndex = 0; rowIndex < worksheet.Rows.Count; rowIndex++)
        {
            IReadOnlyList<CellValue> row = worksheet.Rows[rowIndex];
            int excelRow = rowIndex + 1;
            builder.AppendLine($"    <row r=\"{excelRow}\">");

            for (int columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                string reference = GetCellReference(columnIndex, excelRow);
                CellValue cell = row[columnIndex];
                string style = rowIndex == 0 ? " s=\"1\"" : string.Empty;

                if (cell.IsNumber)
                {
                    builder.AppendLine($"      <c r=\"{reference}\"{style}><v>{Format(cell.NumberValue)}</v></c>");
                }
                else
                {
                    builder.AppendLine($"      <c r=\"{reference}\" t=\"inlineStr\"{style}><is><t>{EscapeXml(cell.TextValue)}</t></is></c>");
                }
            }

            builder.AppendLine("    </row>");
        }

        builder.AppendLine("  </sheetData>");
        builder.AppendLine("</worksheet>");
        return builder.ToString();
    }

    private static string BuildWorkbook(IReadOnlyList<WorksheetData> worksheets)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
        builder.AppendLine("  <sheets>");

        for (int index = 0; index < worksheets.Count; index++)
        {
            builder.AppendLine($"    <sheet name=\"{EscapeXml(worksheets[index].Name)}\" sheetId=\"{index + 1}\" r:id=\"rId{index + 1}\"/>");
        }

        builder.AppendLine("  </sheets>");
        builder.AppendLine("</workbook>");
        return builder.ToString();
    }

    private static string BuildWorkbookRelationships(int worksheetCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");

        for (int index = 0; index < worksheetCount; index++)
        {
            builder.AppendLine($"  <Relationship Id=\"rId{index + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{index + 1}.xml\"/>");
        }

        builder.AppendLine($"  <Relationship Id=\"rId{worksheetCount + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
        builder.AppendLine("</Relationships>");
        return builder.ToString();
    }

    private static string BuildContentTypes(int worksheetCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
        builder.AppendLine("  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
        builder.AppendLine("  <Default Extension=\"xml\" ContentType=\"application/xml\"/>");
        builder.AppendLine("  <Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
        builder.AppendLine("  <Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");

        for (int index = 0; index < worksheetCount; index++)
        {
            builder.AppendLine($"  <Override PartName=\"/xl/worksheets/sheet{index + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        }

        builder.AppendLine("</Types>");
        return builder.ToString();
    }

    private static string BuildRootRelationships() =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n" +
        "  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>\n" +
        "</Relationships>\n";

    private static string BuildStyles() =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">\n" +
        "  <fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>\n" +
        "  <fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>\n" +
        "  <borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>\n" +
        "  <cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>\n" +
        "  <cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs>\n" +
        "</styleSheet>\n";

    private static IReadOnlyList<CellValue> Row(params object?[] values) =>
        values.Select(CellValue.From).ToList();

    private static void AddTextEntry(ZipArchive archive, string entryName, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using Stream stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static string GetCellReference(int zeroBasedColumnIndex, int row)
    {
        int columnNumber = zeroBasedColumnIndex + 1;
        var columnName = new StringBuilder();

        while (columnNumber > 0)
        {
            int modulo = (columnNumber - 1) % 26;
            columnName.Insert(0, (char)('A' + modulo));
            columnNumber = (columnNumber - modulo) / 26;
        }

        return columnName.ToString() + row.ToString(CultureInfo.InvariantCulture);
    }

    private static string EscapeXml(string? value) =>
        string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("'", "&apos;", StringComparison.Ordinal);

    private static string Format(double value) =>
        value.ToString("0.###############", CultureInfo.InvariantCulture);

    private sealed record WorksheetData(string Name, IReadOnlyList<IReadOnlyList<CellValue>> Rows);

    private sealed record CellValue(string TextValue, double NumberValue, bool IsNumber)
    {
        public static CellValue From(object? value) =>
            value switch
            {
                null => new CellValue(string.Empty, 0.0, false),
                double number => new CellValue(string.Empty, number, true),
                float number => new CellValue(string.Empty, number, true),
                decimal number => new CellValue(string.Empty, (double)number, true),
                int number => new CellValue(string.Empty, number, true),
                long number => new CellValue(string.Empty, number, true),
                _ => new CellValue(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty, 0.0, false),
            };
    }
}
