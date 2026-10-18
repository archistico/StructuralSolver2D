using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Serviceability;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Reporting.Markdown;
using StructuralSolver2D.Reporting.Pdf;
using StructuralSolver2D.Reporting.Visualization;
using StructuralSolver2D.Reporting.Xlsx;

namespace StructuralSolver2D.Reporting.Tests;

public sealed class SecondAuditRegressionTests
{
    public static IEnumerable<object[]> LayerCombinations() => Enumerable.Range(0, 16).Select(mask => new object[] { mask });

    [Theory]
    [MemberData(nameof(LayerCombinations))]
    public void Svg_ContextLayersShouldBeIndependent(int mask)
    {
        var model = Model(); var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        var visualization = new StructuralVisualizationModelBuilder().Build(model, output);
        var options = new SvgExportOptions { IncludeUndeformedModel = (mask & 1) != 0, IncludeSupportSymbols = (mask & 2) != 0, IncludeReactions = (mask & 4) != 0, IncludeMemberDimensions = (mask & 8) != 0 };
        var document = XDocument.Parse(new SvgStructuralResultExporter().Export(visualization, options));
        foreach (var (id, expected) in new[] { ("undeformed-model", options.IncludeUndeformedModel), ("supports", options.IncludeSupportSymbols), ("reactions", options.IncludeReactions), ("dimensions", options.IncludeMemberDimensions) })
        {
            var group = document.Root!.Elements().SingleOrDefault(element => (string?)element.Attribute("id") == id);
            Assert.Equal(expected, group is not null);
            if (expected) Assert.NotEmpty(group!.Elements());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HtmlExport_ShouldPreserveSharedSvgOptions(bool interactive)
    {
        var model = Model(); var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        var visualization = new StructuralVisualizationModelBuilder().Build(model, output);
        var options = new SvgExportOptions { Title = "Original", IncludeNodeLabels = false, IncludeLegend = true, IncludeNodeDisplacementLabels = false, IncludeMemberDisplacementLabels = false };
        string state = JsonSerializer.Serialize(options);
        string before = new SvgStructuralResultExporter().Export(visualization, options);
        if (interactive) new InteractiveHtmlStructuralViewerExporter().Export(visualization, new InteractiveViewerExportOptions { Title = "Different", SvgOptions = options });
        else new HtmlStructuralResultPreviewExporter().Export(visualization, new HtmlPreviewExportOptions { Title = "Different", SvgOptions = options });
        Assert.Equal(state, JsonSerializer.Serialize(options));
        Assert.Equal(before, new SvgStructuralResultExporter().Export(visualization, options));
    }

    [Theory]
    [InlineData("A|base", "Appoggio\nsinistro")]
    [InlineData("A`base", "Appoggio\r\nsinistro")]
    [InlineData("A\nbase", "<left> | support")]
    [InlineData("A\\|base", "right\\|side\rwith `ticks` and [brackets]")]
    public void Markdown_ShouldKeepUserContentInsideItsCells(string id, string label)
    {
        var model = Model(id); model.Nodes[0] = model.Nodes[0] with { Label = label };
        var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        string report = new MarkdownStructuralReportGenerator().Generate(model, output.Result, output.InternalForceDiagrams, output.Summary);
        int? columns = null;
        foreach (string line in report.Split('\n'))
        {
            if (!line.StartsWith('|')) { columns = null; continue; }
            int count = Regex.Matches(line, @"(?<!\\)\|").Count;
            columns ??= count;
            Assert.Equal(columns.Value, count);
        }
        Assert.Contains("<code>", report);
        Assert.DoesNotContain("Appoggio\nsinistro", report);
        Assert.Contains("<br />", report);
        Assert.DoesNotContain("<left>", report);
        Assert.DoesNotContain("with `ticks`", report);
        Assert.DoesNotContain("right\\\\|side", report);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Markdown_StatisticsAndChecksShouldControlOnlyTheirSections(bool statistics, bool checks)
    {
        var model = Model(); var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1", new StructuralAnalysisOptions { DeflectionLimit = new DeflectionLimit(250) });
        string report = new MarkdownStructuralReportGenerator().Generate(model, output.Result, output.InternalForceDiagrams, output.DisplacementDiagrams, output.DeflectionChecks, output.Summary,
            new MarkdownReportOptions { IncludeModelStatistics = statistics, IncludeDeflectionChecks = checks });
        Assert.Contains("## Executive summary", report);
        Assert.Contains("### Governing absolute values", report);
        Assert.Equal(statistics, report.Contains("### Model size"));
        Assert.Equal(checks, report.Contains("| Checks passing |"));
        Assert.Equal(checks, report.Contains("| Member | Direction | Limit |"));
    }

    [Fact]
    public void Markdown_SummaryShouldKeepCriticalResultsWithoutSampledAppendices()
    {
        var model = Model();
        for (int i = 2; i <= 10; i++)
        {
            model.AddNode(new StructuralNode("B" + i, 4 * i, 0));
            model.AddMember(new StructuralMember("M" + i, i == 2 ? "B" : "B" + (i - 1), "B" + i, "MAT", "SEC"));
        }
        model.Loads[0] = model.Loads[0] with { TargetId = "B10" };
        var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        var generator = new MarkdownStructuralReportGenerator();
        string full = generator.Generate(model, output.Result, output.InternalForceDiagrams, output.DisplacementDiagrams, output.Summary, new MarkdownReportOptions { IncludeDisplacementSamples = true });
        string summary = generator.Generate(model, output.Result, output.InternalForceDiagrams, output.DisplacementDiagrams, output.Summary, new MarkdownReportOptions { DetailLevel = MarkdownReportDetail.Summary });
        Assert.True(summary.Length < full.Length / 2);
        Assert.Contains("## Units", summary); Assert.Contains("Load case: `LC1`", summary); Assert.Contains("### Governing absolute values", summary);
        Assert.Contains("node `B10`", summary); Assert.Contains("### Support reactions", summary);
        Assert.DoesNotContain("### Nodes", summary); Assert.DoesNotContain("## Internal force diagrams", summary);
        Assert.DoesNotContain("### Member `M2`", summary); Assert.DoesNotContain("## Deformed shape samples", summary);
        Assert.Contains("## Model", full); Assert.Contains("## Deformed shape samples", full);
        static string[] CriticalRows(string text) => text.Split('\n').Where(line => line.StartsWith("| Max ", StringComparison.Ordinal)).Distinct().OrderBy(line => line, StringComparer.Ordinal).ToArray();
        Assert.Equal(CriticalRows(full), CriticalRows(summary));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Summaries_ShouldUseMagnitudesInAllFormatsAndKeepSignedDetail(int sign)
    {
        var model = Model(); model.Loads[0] = model.Loads[0] with { Value = sign * 12 };
        var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        string md = new MarkdownStructuralReportGenerator().Generate(model, output.Result, output.InternalForceDiagrams, output.Summary);
        Assert.Contains("| Max \\|M\\| [kNm] | 48.000000 |", md);
        string pdf = Encoding.ASCII.GetString(new PdfTechnicalReportExporter().Export(model, output.Result, output.InternalForceDiagrams, output.DisplacementDiagrams, output.Summary));
        Assert.DoesNotMatch(@"\(Max \|[^\r\n]+\s-\d", pdf);
        using var archive = new ZipArchive(new MemoryStream(new XlsxStructuralResultExporter().Export(output.Result, output.InternalForceDiagrams, output.DisplacementDiagrams, output.Summary)));
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        using var stream = archive.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        var document = XDocument.Load(stream);
        var values = document.Descendants(ns + "c").Where(cell => ((string?)cell.Attribute("r"))?.StartsWith('F') == true).Elements(ns + "v").Select(value => double.Parse(value.Value, CultureInfo.InvariantCulture));
        Assert.All(values, value => Assert.True(value >= 0));
        string signed = output.Result.Displacements.Single(displacement => displacement.NodeId == "B").Uy.ToString("0.000000", CultureInfo.InvariantCulture);
        Assert.Contains($"| `B` | 0.000000 | {signed} |", md);
    }

    [Theory]
    [InlineData("Relazione – capacità e qualità")]
    [InlineData("Été, più rigidità, € e ²")]
    public void Pdf_ShouldPreserveWesternCharacters(string title)
    {
        var model = Model(); var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        string pdf = Encoding.ASCII.GetString(new PdfTechnicalReportExporter().Export(model, output.Result, output.InternalForceDiagrams, output.DisplacementDiagrams, output.Summary, new PdfTechnicalReportOptions { Title = title, SourceLabel = "modello_è.json" }));
        Assert.Contains("/Encoding /WinAnsiEncoding", pdf);
        var encoding = CodePagesEncodingProvider.Instance.GetEncoding(1252)!;
        string decoded = Regex.Replace(pdf, @"\\([0-7]{3})", match => encoding.GetString(new[] { Convert.ToByte(match.Groups[1].Value, 8) }));
        Assert.Contains(title, decoded); Assert.Contains("modello_è.json", decoded);
    }

    [Fact]
    public void Pdf_ShouldRejectUnsupportedCharactersInsteadOfReplacingIdentifiers()
    {
        var model = Model(); var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        Assert.Throws<NotSupportedException>(() => new PdfTechnicalReportExporter().Export(model, output.Result, output.InternalForceDiagrams, output.DisplacementDiagrams, output.Summary, new PdfTechnicalReportOptions { Title = "東京" }));
    }

    [Theory]
    [InlineData("BEAM_GROUND_FLOOR_01")]
    [InlineData("A_VERY_LONG_MEMBER_IDENTIFIER_WITH_SEVERAL_PARTS_AND_NO_NUMERIC_DATA_LOSS")]
    public void Pdf_ShouldKeepTableTextWithinHorizontalPageMargins(string id)
    {
        var model = Model(memberId: id); var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        string pdf = Encoding.ASCII.GetString(new PdfTechnicalReportExporter().Export(model, output.Result, output.InternalForceDiagrams, output.DisplacementDiagrams, output.Summary, new PdfTechnicalReportOptions { Title = new string('W', 120) }));
        var lines = Regex.Matches(pdf, @"BT /F2 (?<size>[\d.]+) Tf[^\r\n]*\r?\n(?<x>[\d.]+) (?<y>[\d.]+) Td \((?<text>[^\r\n]*)\) Tj");
        Assert.NotEmpty(lines);
        foreach (Match line in lines)
        {
            double size = double.Parse(line.Groups["size"].Value, CultureInfo.InvariantCulture);
            double x = double.Parse(line.Groups["x"].Value, CultureInfo.InvariantCulture);
            Assert.True(x + line.Groups["text"].Value.Length * size * .6 <= 547, line.Value);
        }
        Assert.Contains("Start", pdf); Assert.Contains("End", pdf);
        Assert.Contains("48.000000", pdf);
        var titles = Regex.Matches(pdf, @"BT /F1 18 Tf[^\r\n]*\r?\n48 [\d.]+ Td \((?<text>W+)\) Tj");
        Assert.NotEmpty(titles);
        Assert.All(titles.Cast<Match>(), line => Assert.True(48 + line.Groups["text"].Length * 18 * .944 <= 547));
    }

    private static StructuralModel Model(string nodeId = "A", string memberId = "M1") => new StructuralModel()
        .AddNode(new StructuralNode(nodeId, 0, 0)).AddNode(new StructuralNode("B", 4, 0))
        .AddMaterial(new StructuralMaterial("MAT", "Steel", 210000000)).AddSection(new StructuralSection("SEC", "Section", .003, .00002))
        .AddMember(new StructuralMember(memberId, nodeId, "B", "MAT", "SEC")).AddSupport(StructuralSupport.Fixed("SA", nodeId))
        .AddLoadCase(new StructuralLoadCase("LC1", "Load")).AddLoad(StructuralLoad.NodalForce("P", "LC1", "B", StructuralLoadDirection.GlobalY, -12));
}
