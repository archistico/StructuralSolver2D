using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Reporting.Markdown;
using StructuralSolver2D.Reporting.Pdf;
using StructuralSolver2D.Reporting.Visualization;
using StructuralSolver2D.Reporting.Xlsx;

namespace StructuralSolver2D.Reporting.Tests;

public sealed class ReportingRegressionTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(11)]
    [InlineData(21)]
    public void StationLabels_ShouldInterpolateValuesAtTheirNamedPositions(int count)
    {
        var model = CreateModel();
        var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        var samples = Enumerable.Range(0, count).Select(index =>
        {
            double r = (double)index / (count - 1);
            return new MemberDisplacementSample("M1", r, 5 * r, .001 * r, -.002 * r, .03 * r, .001 * r, -.002 * r);
        }).ToArray();
        var diagram = new MemberDisplacementDiagram("M1", 5, samples);
        var visualization = new StructuralVisualizationModelBuilder().Build(model, output.Result,
            output.InternalForceDiagrams, new[] { diagram }, new VisualizationOptions { DeformationScale = 10 });

        Assert.Equal(new[] { "L/4", "L/2", "3L/4" }, visualization.MemberDisplacementLabels.Select(label => label.StationLabel));
        for (int i = 0; i < 3; i++)
        {
            double r = (i + 1) * .25;
            var label = visualization.MemberDisplacementLabels[i];
            Assert.Equal(r, label.NormalizedPosition, precision: 12);
            Assert.Equal(5 * r, label.Distance, precision: 12);
            Assert.Equal(.001 * r, label.GlobalUx, precision: 12);
            Assert.Equal(-.002 * r, label.GlobalUy, precision: 12);
            Assert.Equal(.03 * r, label.LocalRz, precision: 12);
            Assert.Equal(5 * r + .01 * r, label.Position.X, precision: 12);
            Assert.Equal(-.02 * r, label.Position.Y, precision: 12);
        }
    }

    [Theory]
    [InlineData("Source")]
    [InlineData("Title")]
    [InlineData("Member")]
    [InlineData("Normal")]
    public void Pdf_ShouldKeepEveryWrappedTextLineInsidePageMargins(string field)
    {
        var model = CreateModel();
        if (field == "Member") model.Members[0] = model.Members[0] with { Id = new string('M', 2000) };
        var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        var options = new PdfTechnicalReportOptions
        {
            Title = field == "Title" ? new string('T', 5000) : "Regression report",
            SourceLabel = field == "Source" ? new string('X', 15000) : "source.json"
        };
        string pdf = Encoding.ASCII.GetString(new PdfTechnicalReportExporter().Export(model, output.Result,
            output.InternalForceDiagrams, output.DisplacementDiagrams, output.Summary, options));
        var lines = Regex.Matches(pdf, @"BT /F[12] (?<font>[\d.]+) Tf[^\r\n]*\r?\n48 (?<y>-?[\d.]+) Td");
        Assert.NotEmpty(lines);
        foreach (Match line in lines)
        {
            if (line.Groups["font"].Value == "9") continue; // Page header/footer, outside the body area.
            double y = double.Parse(line.Groups["y"].Value, CultureInfo.InvariantCulture);
            Assert.InRange(y, 48, 788);
        }
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(45.0)]
    [InlineData(90.0)]
    public void Markdown_ShouldDescribeSupportLocalAxesAndOrientation(double angle)
    {
        var model = CreateModel().AddSupport(StructuralSupport.SimpleSupport("ROTATED", "B", orientationDegrees: angle));
        var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        string report = new MarkdownStructuralReportGenerator().Generate(model, output.Result, output.InternalForceDiagrams, output.Summary);
        string section = report.Split("### Supports", StringSplitOptions.None)[1].Split("### Load cases", StringSplitOptions.None)[0];

        Assert.Contains("Ux (local)", section);
        Assert.Contains("Uy (local)", section);
        Assert.Contains("Orientation [deg]", section);
        Assert.Contains($"| `ROTATED` | `B` | SimpleSupport | no | yes | no | {angle.ToString("0.000000", CultureInfo.InvariantCulture)} |", section);
    }

    [Theory]
    [InlineData(1e-17)]
    [InlineData(-1e-17)]
    [InlineData(1e-100)]
    [InlineData(-1e100)]
    [InlineData(.12345678901234567)]
    public void Xlsx_ShouldPreserveNumericCellValueAcrossScales(double value)
    {
        var result = new StructuralAnalysisResult("LC1", new[] { new NodalDisplacementResult("A", value, value, value) },
            Array.Empty<SupportReactionResult>(), Array.Empty<MemberEndForceResult>());
        var summary = new Frame2DResultSummarizer().Summarize(result, Array.Empty<MemberInternalForceDiagram>());
        byte[] bytes = new XlsxStructuralResultExporter().Export(result, Array.Empty<MemberInternalForceDiagram>(),
            Array.Empty<MemberDisplacementDiagram>(), summary);
        using var archive = new ZipArchive(new MemoryStream(bytes));
        using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet2.xml")!.Open());
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var document = XDocument.Parse(reader.ReadToEnd());
        string stored = document.Descendants(ns + "c").Single(cell => (string?)cell.Attribute("r") == "B2").Element(ns + "v")!.Value;

        Assert.Equal(value, double.Parse(stored, CultureInfo.InvariantCulture));
    }

    private static StructuralModel CreateModel() => new StructuralModel()
        .AddNode(new StructuralNode("A", 0, 0)).AddNode(new StructuralNode("B", 5, 0))
        .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000))
        .AddSection(new StructuralSection("SEC", "Section", .003, .00002))
        .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC"))
        .AddSupport(StructuralSupport.Fixed("SA", "A"))
        .AddLoadCase(new StructuralLoadCase("LC1", "Load"))
        .AddLoad(StructuralLoad.NodalForce("P", "LC1", "B", StructuralLoadDirection.GlobalY, -10));
}
