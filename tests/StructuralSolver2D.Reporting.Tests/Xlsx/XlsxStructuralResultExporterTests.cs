using System.IO.Compression;
using System.Text;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Reporting.Xlsx;

namespace StructuralSolver2D.Reporting.Tests.Xlsx;

public sealed class XlsxStructuralResultExporterTests
{
    [Fact]
    public void Export_ShouldCreateValidOpenXmlWorkbookParts()
    {
        byte[] workbook = CreateExporter().Export(
            CreateResult(),
            CreateInternalForceDiagrams(),
            CreateDisplacementDiagrams(),
            CreateSummary());

        using var stream = new MemoryStream(workbook);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.NotNull(archive.GetEntry("[Content_Types].xml"));
        Assert.NotNull(archive.GetEntry("_rels/.rels"));
        Assert.NotNull(archive.GetEntry("xl/workbook.xml"));
        Assert.NotNull(archive.GetEntry("xl/_rels/workbook.xml.rels"));
        Assert.NotNull(archive.GetEntry("xl/styles.xml"));
        Assert.NotNull(archive.GetEntry("xl/worksheets/sheet1.xml"));
        Assert.NotNull(archive.GetEntry("xl/worksheets/sheet6.xml"));
    }

    [Fact]
    public void Export_ShouldWriteExpectedSheetNames()
    {
        byte[] workbook = CreateExporter().Export(
            CreateResult(),
            CreateInternalForceDiagrams(),
            CreateDisplacementDiagrams(),
            CreateSummary());

        string workbookXml = ReadEntry(workbook, "xl/workbook.xml");

        Assert.Contains("name=\"Summary\"", workbookXml, StringComparison.Ordinal);
        Assert.Contains("name=\"Nodal displacements\"", workbookXml, StringComparison.Ordinal);
        Assert.Contains("name=\"Support reactions\"", workbookXml, StringComparison.Ordinal);
        Assert.Contains("name=\"Member end forces\"", workbookXml, StringComparison.Ordinal);
        Assert.Contains("name=\"Internal force samples\"", workbookXml, StringComparison.Ordinal);
        Assert.Contains("name=\"Displacement samples\"", workbookXml, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_ShouldWriteResultValuesToWorksheets()
    {
        byte[] workbook = CreateExporter().Export(
            CreateResult(),
            CreateInternalForceDiagrams(),
            CreateDisplacementDiagrams(),
            CreateSummary());

        string displacementsXml = ReadEntry(workbook, "xl/worksheets/sheet2.xml");
        string memberForcesXml = ReadEntry(workbook, "xl/worksheets/sheet4.xml");
        string internalSamplesXml = ReadEntry(workbook, "xl/worksheets/sheet5.xml");

        Assert.Contains("NodeId", displacementsXml, StringComparison.Ordinal);
        Assert.Contains("N1", displacementsXml, StringComparison.Ordinal);
        Assert.Contains("<v>0.001</v>", displacementsXml, StringComparison.Ordinal);
        Assert.Contains("StartMoment_kNm", memberForcesXml, StringComparison.Ordinal);
        Assert.Contains("<v>3.75</v>", memberForcesXml, StringComparison.Ordinal);
        Assert.Contains("BendingMoment_kNm", internalSamplesXml, StringComparison.Ordinal);
        Assert.Contains("<v>-4</v>", internalSamplesXml, StringComparison.Ordinal);
    }

    private static XlsxStructuralResultExporter CreateExporter() => new();

    private static StructuralAnalysisResult CreateResult() =>
        new(
            "LC1",
            new[]
            {
                new NodalDisplacementResult("N1", 0.001, -0.002, 0.003),
                new NodalDisplacementResult("N2", 0.0, 0.0, 0.0),
            },
            new[]
            {
                new SupportReactionResult("S1", "N1", 1.0, 2.0, 3.0),
            },
            new[]
            {
                new MemberEndForceResult("M1", 1.25, -2.5, 3.75, -4.25, 5.5, -6.75),
            });

    private static IReadOnlyList<MemberInternalForceDiagram> CreateInternalForceDiagrams() =>
        new[]
        {
            new MemberInternalForceDiagram(
                "M1",
                4.0,
                new[]
                {
                    new MemberInternalForceSample("M1", 0.0, 0.0, 1.0, 2.0, 3.0),
                    new MemberInternalForceSample("M1", 1.0, 4.0, -2.0, -3.0, -4.0),
                }),
        };

    private static IReadOnlyList<MemberDisplacementDiagram> CreateDisplacementDiagrams() =>
        new[]
        {
            new MemberDisplacementDiagram(
                "M1",
                4.0,
                new[]
                {
                    new MemberDisplacementSample("M1", 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0),
                    new MemberDisplacementSample("M1", 1.0, 4.0, 0.001, -0.002, 0.003, 0.004, -0.005),
                }),
        };

    private static StructuralAnalysisSummary CreateSummary() =>
        new(
            "LC1",
            new AnalysisResultExtreme("N1", 0.001),
            new AnalysisResultExtreme("N1", 0.002),
            new AnalysisResultExtreme("N1", 0.003),
            new AnalysisResultExtreme("S1", 1.0),
            new AnalysisResultExtreme("S1", 2.0),
            new AnalysisResultExtreme("S1", 3.0),
            new[]
            {
                MemberInternalForceExtrema.FromDiagram(CreateInternalForceDiagrams()[0]),
            });

    private static string ReadEntry(byte[] workbook, string entryName)
    {
        using var stream = new MemoryStream(workbook);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        ZipArchiveEntry entry = archive.GetEntry(entryName) ?? throw new InvalidOperationException($"Entry '{entryName}' was not found.");
        using Stream entryStream = entry.Open();
        using var reader = new StreamReader(entryStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
