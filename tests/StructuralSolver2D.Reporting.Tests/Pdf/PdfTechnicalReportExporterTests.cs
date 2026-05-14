using System.Text;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Reporting.Pdf;

namespace StructuralSolver2D.Reporting.Tests.Pdf;

public sealed class PdfTechnicalReportExporterTests
{
    [Fact]
    public void Export_ShouldCreatePdfDocumentWithExpectedSections()
    {
        byte[] pdf = CreateExporter().Export(
            CreateModel(),
            CreateResult(),
            CreateInternalForceDiagrams(),
            CreateDisplacementDiagrams(),
            CreateSummary(),
            new PdfTechnicalReportOptions
            {
                Title = "Example technical report",
                Description = "Educational PDF report.",
                SourceLabel = "example.json",
            });

        string text = Encoding.ASCII.GetString(pdf);

        Assert.StartsWith("%PDF-1.4", text, StringComparison.Ordinal);
        Assert.Contains("/Type /Catalog", text, StringComparison.Ordinal);
        Assert.Contains("Example technical report", text, StringComparison.Ordinal);
        Assert.Contains("Model overview", text, StringComparison.Ordinal);
        Assert.Contains("Executive summary", text, StringComparison.Ordinal);
        Assert.Contains("Nodal displacements", text, StringComparison.Ordinal);
        Assert.Contains("Support reactions", text, StringComparison.Ordinal);
        Assert.Contains("Member end forces", text, StringComparison.Ordinal);
        Assert.Contains("startxref", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_WithInvalidOptions_ShouldThrowClearException()
    {
        var options = new PdfTechnicalReportOptions { MaxRowsPerSection = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateExporter().Export(
                CreateModel(),
                CreateResult(),
                CreateInternalForceDiagrams(),
                CreateDisplacementDiagrams(),
                CreateSummary(),
                options));
    }

    private static PdfTechnicalReportExporter CreateExporter() => new();

    private static StructuralModel CreateModel() =>
        new StructuralModel()
            .AddNode(new StructuralNode("N1", 0.0, 0.0))
            .AddNode(new StructuralNode("N2", 4.0, 0.0))
            .AddSupport(StructuralSupport.Hinge("S1", "N1"))
            .AddMaterial(new StructuralMaterial("MAT", "Generic", 210_000_000.0))
            .AddSection(new StructuralSection("SEC", "Generic", 0.01, 0.0001))
            .AddMember(new StructuralMember("M1", "N1", "N2", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("LC1", "Test load case"));

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
}
