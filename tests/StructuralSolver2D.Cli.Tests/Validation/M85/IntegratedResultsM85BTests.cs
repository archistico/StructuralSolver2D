using System.Globalization;
using System.IO.Compression;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Reporting.Csv;

namespace StructuralSolver2D.Cli.Tests.Validation.M85;

/// <summary>
/// M85-B: end-to-end JSON -> public facade -> CLI -> CSV/Markdown/XLSX/PDF.
/// Expected reactions and deflections come from elementary beam statics,
/// not from production stiffness matrices or the CSV/report exporters.
/// </summary>
public sealed class IntegratedResultsM85BTests
{
    private const double E = 210_000_000d; // kN/m²
    private const double I = 0.00002d;     // m^4

    [Fact]
    public async Task Cantilever_ClosedFormResults_AgreeAcrossCliCsvMarkdownXlsxAndPdf()
    {
        using var workspace = new CliTestWorkspace();
        string source = workspace.Save(CliTestWorkspace.Model());
        var input = StructuralModelJsonReader.Read(source);
        var output = new StructuralSolver2DService().AnalyzeLoadCase(input.Model, "LC1");

        // Classical Euler-Bernoulli cantilever: L=4 m, signed tip load P=-12 kN.
        const double length = 4d, force = -12d;
        double expectedUy = force * Math.Pow(length, 3) / (3d * E * I);
        double expectedRz = force * length * length / (2d * E * I);
        Close(expectedUy, output.Result.GetDisplacement("B").Uy, 1e-10);
        Close(expectedRz, output.Result.GetDisplacement("B").Rz, 1e-10);
        Close(12d, output.Result.GetReaction("SA").Fy, 1e-8);
        Close(48d, output.Result.GetReaction("SA").Mz, 1e-8);

        var cli = await workspace.Run("analyze", source);
        cli.AssertSuccess();
        Assert.Contains("Analysis id: LC1", cli.Stdout);
        Assert.Contains(expectedUy.ToString("0.000000", CultureInfo.InvariantCulture), cli.Stdout);

        await AssertCsvAgainstPublicApi(workspace, source, "LC1", output);

        string markdownPath = workspace.PathFor("cantilever.md");
        (await workspace.Run("report", source, markdownPath)).AssertSuccess();
        string markdown = File.ReadAllText(markdownPath);
        Assert.Contains("**Analysis id:** `LC1`", markdown);
        AssertMarkdownRow(markdown, "| `B` |", expectedUy, 3, expectedRz, 4);
        AssertMarkdownRow(markdown, "| `SA` | `A` |", 12d, 4, 48d, 5);

        string xlsxPath = workspace.PathFor("cantilever.xlsx");
        (await workspace.Run("export-xlsx", source, xlsxPath)).AssertSuccess();
        Close(expectedUy, XlsxNumber(xlsxPath, 2, "C3"), 1e-10);
        Close(12d, XlsxNumber(xlsxPath, 3, "D2"), 1e-8);
        Close(48d, XlsxNumber(xlsxPath, 3, "E2"), 1e-8);
        Assert.Equal("LC1", XlsxInlineText(xlsxPath, 1, "B2"));

        string pdfPath = workspace.PathFor("cantilever.pdf");
        (await workspace.Run("export-pdf", source, pdfPath)).AssertSuccess();
        byte[] pdf = File.ReadAllBytes(pdfPath);
        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }

    [Theory]
    [InlineData("ULS1", 48.75d, 60.9375d)]
    [InlineData("SLS1", 35d, 43.75d)]
    [InlineData("G1", 25d, 31.25d)]
    public async Task ManualCombination_OrExplicitCase_PropagatesCorrectlyToAllCsvAndMarkdown(
        string target, double expectedReaction, double expectedMidspanMoment)
    {
        using var workspace = new CliTestWorkspace();
        // Default is ULS1. G1 is deliberately selected by explicit CLI override.
        string source = workspace.Save(CliTestWorkspace.Model("combinations/load-combination.json"));
        var input = StructuralModelJsonReader.Read(source);
        bool combination = target is "ULS1" or "SLS1";
        var service = new StructuralSolver2DService();
        var output = combination
            ? service.AnalyzeLoadCombination(input.Model, target)
            : service.AnalyzeLoadCase(input.Model, target);

        // L=5, q(G1)=10, q(Q1)=4 kN/m. For a simply supported beam,
        // R_A=R_B=qL/2 and M_mid=qL²/8, with no FEM assumptions.
        Close(expectedReaction, output.Result.GetReaction("SA").Fy, 1e-8);
        Close(expectedReaction, output.Result.GetReaction("SB").Fy, 1e-8);
        Close(expectedMidspanMoment, Math.Abs(output.Summary.MaxAbsBendingMoment.Value), 1e-7);

        var cli = await workspace.Run("analyze", source, target);
        cli.AssertSuccess();
        Assert.Contains($"Analysis id: {target}", cli.Stdout);
        await AssertCsvAgainstPublicApi(workspace, source, target, output);

        string markdownPath = workspace.PathFor("result-" + target + ".md");
        (await workspace.Run("report-full", source, markdownPath, target)).AssertSuccess();
        string markdown = File.ReadAllText(markdownPath);
        Assert.Contains($"**Analysis id:** `{target}`", markdown);
        AssertMarkdownRow(markdown, "| `SA` | `A` |", expectedReaction, 4);
        AssertMarkdownRow(markdown, "| `SB` | `B` |", expectedReaction, 4);

        string xlsxPath = workspace.PathFor("result-" + target + ".xlsx");
        (await workspace.Run("export-xlsx", source, xlsxPath, target)).AssertSuccess();
        Assert.Equal(target, XlsxInlineText(xlsxPath, 1, "B2"));
        Close(expectedReaction, XlsxNumber(xlsxPath, 3, "D2"), 1e-8);
        Close(expectedReaction, XlsxNumber(xlsxPath, 3, "D3"), 1e-8);
    }

    [Fact]
    public async Task SignedCombination_WithNegativeCoefficient_IsPreservedByApiCsvAndCli()
    {
        using var workspace = new CliTestWorkspace();
        var model = CliTestWorkspace.Model("combinations/load-combination.json");
        var combinations = model["loadCombinations"]!.AsArray();
        combinations.Add(JsonNode.Parse("""{"id":"NEG","name":"Signed check","terms":[{"loadCaseId":"G1","factor":1.2},{"loadCaseId":"Q1","factor":-0.5}]}"""));
        string source = workspace.Save(model);
        var input = StructuralModelJsonReader.Read(source);
        var result = new StructuralSolver2DService().AnalyzeLoadCombination(input.Model, "NEG");

        // Signed design combination: (-10)*1.2 + (-4)*(-0.5) = -10 kN/m.
        Close(25d, result.Result.GetReaction("SA").Fy, 1e-8);
        Close(25d, result.Result.GetReaction("SB").Fy, 1e-8);
        Close(31.25d, Math.Abs(result.Summary.MaxAbsBendingMoment.Value), 1e-7);
        await AssertCsvAgainstPublicApi(workspace, source, "NEG", result);
        var cli = await workspace.Run("analyze", source, "NEG");
        cli.AssertSuccess();
        Assert.Contains("Analysis id: NEG", cli.Stdout);
    }

    private static async Task AssertCsvAgainstPublicApi(
        CliTestWorkspace workspace, string source, string target, StructuralAnalysisOutput run)
    {
        string directory = workspace.PathFor("csv-" + target);
        (await workspace.Run("export-csv", source, directory, target)).AssertSuccess();
        var exporter = new CsvStructuralResultExporter();
        var expected = new Dictionary<string, string>
        {
            ["nodal-displacements.csv"] = exporter.ExportNodalDisplacements(run.Result),
            ["support-reactions.csv"] = exporter.ExportSupportReactions(run.Result),
            ["member-end-forces.csv"] = exporter.ExportMemberEndForces(run.Result),
            ["internal-force-samples.csv"] = exporter.ExportInternalForceSamples(target, run.InternalForceDiagrams),
            ["displacement-samples.csv"] = exporter.ExportDisplacementSamples(target, run.DisplacementDiagrams),
            ["summary.csv"] = exporter.ExportSummary(run.Summary),
        };
        Assert.Equal(expected.Count, Directory.GetFiles(directory, "*.csv").Length);
        foreach (var (fileName, expectedText) in expected)
        {
            string actual = File.ReadAllText(Path.Combine(directory, fileName));
            Assert.Equal(expectedText, actual);
        }
        string[] reactionLines = File.ReadAllLines(Path.Combine(directory, "support-reactions.csv"));
        Assert.All(reactionLines.Skip(1), line => Assert.StartsWith(target + ",", line));
    }

    private static double XlsxNumber(string path, int sheet, string address)
    {
        XElement cell = XlsxCell(path, sheet, address);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return double.Parse(cell.Element(ns + "v")?.Value
            ?? throw new InvalidDataException("Missing numeric cell " + address), CultureInfo.InvariantCulture);
    }

    private static string XlsxInlineText(string path, int sheet, string address)
    {
        XElement cell = XlsxCell(path, sheet, address);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return cell.Descendants(ns + "t").Single().Value;
    }

    private static XElement XlsxCell(string path, int sheet, string address)
    {
        using ZipArchive archive = ZipFile.OpenRead(path);
        ZipArchiveEntry entry = archive.GetEntry($"xl/worksheets/sheet{sheet}.xml")
            ?? throw new InvalidDataException("XLSX sheet missing: " + sheet);
        using Stream stream = entry.Open();
        var doc = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return doc.Descendants(ns + "c").Single(c => (string?)c.Attribute("r") == address);
    }

    private static void AssertMarkdownRow(string markdown, string startsWith,
        double expected, int column, double? secondExpected = null, int secondColumn = 0)
    {
        string line = markdown.Split('\n').Single(line =>
            line.StartsWith(startsWith, StringComparison.Ordinal) &&
            line.Split('|').Length == column + 3);
        string[] cells = line.Split('|', StringSplitOptions.TrimEntries);
        Close(expected, double.Parse(cells[column], CultureInfo.InvariantCulture), 5.1e-7);
        if (secondExpected.HasValue)
            Close(secondExpected.Value, double.Parse(cells[secondColumn], CultureInfo.InvariantCulture), 5.1e-7);
    }

    private static void Close(double expected, double actual, double tolerance) =>
        Assert.True(double.IsFinite(actual) && Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected:R}, got {actual:R}; absolute tolerance {tolerance:R}.");
}
