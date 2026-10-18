using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Reporting.Pdf;

/// <summary>Exports a self-contained, paginated technical report without external PDF libraries.</summary>
public sealed class PdfTechnicalReportExporter
{
    /// <summary>Exports the supplied model and analysis results to PDF bytes.</summary>
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

        var report = new PdfReportLayout(options.Title);
        report.Hero(options.Title, options.Description, result.LoadCaseId, options.SourceLabel);
        report.Heading("Model overview");
        report.Statistics(new[]
        {
            ("Nodes", model.Nodes.Count), ("Members", model.Members.Count), ("Supports", model.Supports.Count),
            ("Load cases", model.LoadCases.Count), ("Combinations", model.LoadCombinations.Count), ("Loads", model.Loads.Count)
        });
        report.ModelFigure(model, displacementDiagrams);

        report.Heading("Executive summary");
        report.Highlights(new[]
        {
            ("Nodal max |Uy| [m]", Format(Math.Abs(summary.MaxAbsUy.Value))),
            ("Max |V| [kN]", Format(Math.Abs(summary.MaxAbsShearForce.Value))),
            ("Max |M| [kNm]", Format(Math.Abs(summary.MaxAbsBendingMoment.Value)))
        });
        report.Table("Executive summary", new[] { "Metric", "Entity / member", "Pos [0-1]", "x [m]", "Value", "Unit" },
            new[] { 76d, 123, 68, 76, 107, 49 }, SummaryRows(summary), rightAlignedFrom: 2);

        report.Heading("Assumptions and limitations");
        report.Paragraph("First-order linear elastic 2D analysis. Units: m, rad, kN and kNm. Summary values are absolute magnitudes; detail tables retain their signs.", muted: true);
        report.Paragraph("This is a technical computation report. It does not replace engineering judgement or project-specific normative checks.", muted: true);

        report.Heading("Nodal displacements");
        report.Table("Nodal displacements", new[] { "Node", "Ux [m]", "Uy [m]", "Rz [rad]" },
            new[] { 151d, 116, 116, 116 }, result.Displacements.Take(options.MaxRowsPerSection)
                .Select(item => new[] { item.NodeId, Format(item.Ux), Format(item.Uy), Format(item.Rz) }), 1);
        Truncation(report, result.Displacements.Count, options.MaxRowsPerSection);

        report.Heading("Support reactions");
        report.Table("Support reactions", new[] { "Support", "Node", "Fx [kN]", "Fy [kN]", "Mz [kNm]" },
            new[] { 85d, 84, 110, 110, 110 }, result.Reactions.Take(options.MaxRowsPerSection)
                .Select(item => new[] { item.SupportId, item.NodeId, Format(item.Fx), Format(item.Fy), Format(item.Mz) }), 2);
        Truncation(report, result.Reactions.Count, options.MaxRowsPerSection);

        report.Heading("Member end forces");
        report.Table("Member end forces", new[] { "Member", "End", "N [kN]", "V [kN]", "M [kNm]" },
            new[] { 113d, 56, 110, 110, 110 }, result.MemberEndForces.Take(options.MaxRowsPerSection).SelectMany(item => new[]
            {
                new[] { item.MemberId, "Start", Format(item.StartAxial), Format(item.StartShear), Format(item.StartMoment) },
                new[] { item.MemberId, "End", Format(item.EndAxial), Format(item.EndShear), Format(item.EndMoment) }
            }), 2);
        Truncation(report, result.MemberEndForces.Count, options.MaxRowsPerSection);

        if (options.IncludeInternalForceSamples)
        {
            report.Heading("Internal-force samples");
            report.Table("Internal-force samples", new[] { "Member", "Pos [0-1]", "x [m]", "N [kN]", "V [kN]", "M [kNm]" },
                new[] { 91d, 64, 77, 89, 89, 89 }, internalForceDiagrams.SelectMany(diagram => diagram.Samples).Take(options.MaxRowsPerSection)
                    .Select(item => new[] { item.MemberId, Format(item.Position), Format(item.Distance), Format(item.NormalForce), Format(item.ShearForce), Format(item.BendingMoment) }), 1);
            Truncation(report, internalForceDiagrams.Sum(diagram => diagram.Samples.Count), options.MaxRowsPerSection);
        }

        if (options.IncludeDisplacementSamples)
        {
            report.Heading("Displacement samples");
            report.Table("Displacement samples", new[] { "Member", "Pos [0-1]", "x [m]", "GUx [m]", "GUy [m]" },
                new[] { 119d, 70, 90, 110, 110 }, displacementDiagrams.SelectMany(diagram => diagram.Samples).Take(options.MaxRowsPerSection)
                    .Select(item => new[] { item.MemberId, Format(item.NormalizedPosition), Format(item.Distance), Format(item.GlobalUx), Format(item.GlobalUy) }), 1);
            Truncation(report, displacementDiagrams.Sum(diagram => diagram.Samples.Count), options.MaxRowsPerSection);
        }

        return BuildPdf(report.Finish());
    }

    private static IEnumerable<string[]> SummaryRows(StructuralAnalysisSummary summary)
    {
        yield return Nodal("Max |Ux|", summary.MaxAbsUx, "m");
        yield return Nodal("Max |Uy|", summary.MaxAbsUy, "m");
        yield return Nodal("Max |Rz|", summary.MaxAbsRz, "rad");
        yield return Nodal("Max |Fx|", summary.MaxAbsReactionFx, "kN");
        yield return Nodal("Max |Fy|", summary.MaxAbsReactionFy, "kN");
        yield return Nodal("Max |Mz|", summary.MaxAbsReactionMz, "kNm");
        yield return Member("Max |N|", summary.MaxAbsNormalForce, "kN");
        yield return Member("Max |V|", summary.MaxAbsShearForce, "kN");
        yield return Member("Max |M|", summary.MaxAbsBendingMoment, "kNm");

        static string[] Nodal(string metric, AnalysisResultExtreme value, string unit) =>
            new[] { metric, value.EntityId, "", "", Format(Math.Abs(value.Value)), unit };
        static string[] Member(string metric, InternalForceExtreme value, string unit) =>
            new[] { metric, value.MemberId, Format(value.Position), Format(value.Distance), Format(Math.Abs(value.Value)), unit };
    }

    private static void Truncation(PdfReportLayout report, int total, int limit)
    {
        if (total > limit)
            report.Paragraph($"Only the first {limit} of {total} rows are shown in this PDF. Use CSV/XLSX exports for full tabular data.", muted: true);
    }

    private static byte[] BuildPdf(IReadOnlyList<string> pages)
    {
        int firstPage = 6;
        int firstContent = firstPage + pages.Count;
        string kids = string.Join(" ", Enumerable.Range(0, pages.Count).Select(index => $"{firstPage + index} 0 R"));
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{kids}] /Count {pages.Count} >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"
        };
        for (int index = 0; index < pages.Count; index++)
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R /F3 5 0 R >> >> /Contents {firstContent + index} 0 R >>");
        foreach (string page in pages)
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(page)} >>\nstream\n{page}endstream");

        using var stream = new MemoryStream();
        Write("%PDF-1.4\n%StructuralSolver2D\n");
        var offsets = new List<long> { 0 };
        for (int index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }
        long xref = stream.Position;
        Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (long offset in offsets.Skip(1)) Write($"{offset:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return stream.ToArray();

        void Write(string value) => stream.Write(Encoding.ASCII.GetBytes(value));
    }

    private static string Format(double value) => value.ToString("0.000000", CultureInfo.InvariantCulture);
}
