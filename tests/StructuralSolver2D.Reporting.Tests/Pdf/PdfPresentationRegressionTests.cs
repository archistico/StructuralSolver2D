using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Reporting.Pdf;

namespace StructuralSolver2D.Reporting.Tests.Pdf;

public sealed class PdfPresentationRegressionTests
{
    [Fact]
    public void Export_ShouldHaveValidObjectOffsetsStreamLengthsAndPageTotals()
    {
        string pdf = Export(Model());
        var objects = Regex.Matches(pdf, @"(?m)^(?<id>\d+) 0 obj\r?$");
        string[] xref = pdf.Split("xref\n", StringSplitOptions.None)[1].Split('\n');
        foreach (Match item in objects)
        {
            int id = int.Parse(item.Groups["id"].Value, CultureInfo.InvariantCulture);
            Assert.Equal(item.Index, int.Parse(xref[id + 1][..10], CultureInfo.InvariantCulture));
        }
        foreach (Match stream in Regex.Matches(pdf, @"/Length (?<length>\d+) >>\nstream\n(?<content>.*?)endstream", RegexOptions.Singleline))
            Assert.Equal(int.Parse(stream.Groups["length"].Value, CultureInfo.InvariantCulture), stream.Groups["content"].Length);
        int pages = Regex.Matches(pdf, @"/Type /Page ").Count;
        Assert.True(pages >= 2);
        for (int page = 1; page <= pages; page++) Assert.Contains($"(Page {page} of {pages}) Tj", pdf);
        Assert.Contains("/BaseFont /Helvetica-Bold", pdf);
    }

    [Fact]
    public void LongTables_ShouldRepeatTheirHeadersAndRetainAllRows()
    {
        var model = Model();
        var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        var rows = Enumerable.Range(0, 95).Select(index => new NodalDisplacementResult($"ROW_{index:D3}", index + .125, -index - .25, .0005)).ToArray();
        var result = new StructuralAnalysisResult("LC1", rows, output.Result.Reactions, output.Result.MemberEndForces);
        string pdf = Encoding.ASCII.GetString(new PdfTechnicalReportExporter().Export(model, result, output.InternalForceDiagrams,
            output.DisplacementDiagrams, output.Summary, new PdfTechnicalReportOptions { MaxRowsPerSection = 100, IncludeDisplacementSamples = false, IncludeInternalForceSamples = false }));
        string[] pages = Streams(pdf);
        Assert.True(pages.Count(page => page.Contains("ROW_", StringComparison.Ordinal)) >= 3);
        foreach (string page in pages.Where(page => page.Contains("ROW_", StringComparison.Ordinal)))
        {
            Assert.Contains("(Node) Tj", page);
            Assert.Contains("(Ux [m]) Tj", page);
            Assert.Contains("(Uy [m]) Tj", page);
            Assert.Contains("(Rz [rad]) Tj", page);
        }
        for (int index = 0; index < rows.Length; index++) Assert.Single(Regex.Matches(pdf, $@"\(ROW_{index:D3}\) Tj").Cast<Match>());
        Assert.Contains("Nodal displacements / continued", pdf);
        Assert.Contains("(-94.250000) Tj", pdf);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(10)]
    [InlineData(0)]
    public void Figure_ShouldRespectGlobalDisplacementDirectionAndKeepTheGeometryUndeformed(double load)
    {
        var model = Model(load);
        if (load == 0) model.Loads.Clear();
        string pdf = Export(model);
        var figure = Regex.Match(pdf, @"q [^\r\n]+ re W n(?<drawing>.*?)\r?\nQ", RegexOptions.Singleline).Groups["drawing"].Value;
        var original = Regex.Match(figure, @"0.62 0.68 0.72 RG 1.5 w (?<x1>[\d.]+) (?<y1>[\d.]+) m (?<x2>[\d.]+) (?<y2>[\d.]+) l S");
        Assert.True(original.Success);
        double baseline = Parse(original.Groups["y1"].Value);
        Assert.Equal(baseline, Parse(original.Groups["y2"].Value));
        var deformed = Regex.Matches(figure, @"0.00 0.43 0.46 RG 1.7 w [\d.]+ (?<y1>[\d.]+) m [\d.]+ (?<y2>[\d.]+) l S");
        Assert.NotEmpty(deformed);
        var ordinates = deformed.SelectMany(line => new[] { Parse(line.Groups["y1"].Value), Parse(line.Groups["y2"].Value) }).ToArray();
        if (load < 0) { Assert.True(ordinates.Min() < baseline); Assert.All(ordinates, yy => Assert.True(yy <= baseline + .001)); }
        else if (load > 0) { Assert.True(ordinates.Max() > baseline); Assert.All(ordinates, yy => Assert.True(yy >= baseline - .001)); }
        else Assert.All(ordinates, yy => Assert.Equal(baseline, yy));
        Assert.Contains("auto-amplified deformation", pdf);
        Assert.Contains("Nodal max |Uy| [m]", pdf); // Nodal displacement can be zero despite span deflection.
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SampleOptionsAndTruncation_ShouldKeepTheirMeaning(bool forces, bool displacements)
    {
        string pdf = Export(Model(), new PdfTechnicalReportOptions { MaxRowsPerSection = 1, IncludeInternalForceSamples = forces, IncludeDisplacementSamples = displacements });
        Assert.Equal(forces, pdf.Contains("Internal-force samples", StringComparison.Ordinal));
        Assert.Equal(displacements, pdf.Contains("Displacement samples", StringComparison.Ordinal));
        Assert.Contains("Only the first 1 of 2 rows", pdf);
        Assert.Contains("CSV/XLSX", pdf);
        Assert.Contains("GEOMETRY & DEFORMATION", pdf);
    }

    [Fact]
    public void EmptyReport_ShouldShowExplicitEmptyTablesWithoutInvalidDrawingCoordinates()
    {
        var result = new StructuralAnalysisResult("EMPTY", Array.Empty<NodalDisplacementResult>(), Array.Empty<SupportReactionResult>(), Array.Empty<MemberEndForceResult>());
        var zero = new AnalysisResultExtreme("", 0);
        var summary = new StructuralAnalysisSummary("EMPTY", zero, zero, zero, zero, zero, zero, Array.Empty<MemberInternalForceExtrema>());
        string pdf = Encoding.ASCII.GetString(new PdfTechnicalReportExporter().Export(new StructuralModel(), result,
            Array.Empty<MemberInternalForceDiagram>(), Array.Empty<MemberDisplacementDiagram>(), summary));
        Assert.Equal(5, Regex.Matches(pdf, "No results available").Count);
        Assert.DoesNotContain("NaN", pdf);
        Assert.DoesNotContain("Infinity", pdf);
    }

    private static string[] Streams(string pdf) => Regex.Matches(pdf, @"stream\n(?<content>.*?)endstream", RegexOptions.Singleline)
        .Select(match => match.Groups["content"].Value).ToArray();
    private static double Parse(string value) => double.Parse(value, CultureInfo.InvariantCulture);
    private static string Export(StructuralModel model, PdfTechnicalReportOptions? options = null)
    {
        var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        return Encoding.ASCII.GetString(new PdfTechnicalReportExporter().Export(model, output.Result, output.InternalForceDiagrams,
            output.DisplacementDiagrams, output.Summary, options));
    }

    private static StructuralModel Model(double load = -10) => new StructuralModel()
        .AddNode(new StructuralNode("A", 0, 0)).AddNode(new StructuralNode("B", 5, 0))
        .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000))
        .AddSection(new StructuralSection("SEC", "Section", .003, .00002))
        .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC"))
        .AddSupport(StructuralSupport.Hinge("SA", "A")).AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
        .AddLoadCase(new StructuralLoadCase("LC1", "Uniform load"))
        .AddLoad(StructuralLoad.UniformDistributedLoad("Q", "LC1", "M1", StructuralLoadDirection.GlobalY, load));
}
