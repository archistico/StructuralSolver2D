using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Text.Json.Nodes;

namespace StructuralSolver2D.Cli.Tests;

public sealed class CliCommandRegressionTests
{
    private static readonly string[] ExportCommands = { "report", "export-csv", "export-xlsx", "export-pdf", "export-svg", "export-html", "export-viewer" };
    public static IEnumerable<object[]> Commands() => new[] { "analyze" }.Concat(ExportCommands).Select(command => new object[] { command });
    public static IEnumerable<object[]> FileCollisions() => ExportCommands.Where(command => command != "export-csv")
        .SelectMany(command => (OperatingSystem.IsWindows() ? new[] { "same", "relative", "case" } : new[] { "same", "relative" })
            .Select(alias => new object[] { command, alias }));

    [Theory]
    [MemberData(nameof(FileCollisions))]
    public async Task Export_ShouldPreserveSourceWhenDestinationIsEquivalent(string command, string alias)
    {
        using var workspace = new CliTestWorkspace();
        string source = workspace.Save(CliTestWorkspace.Model());
        byte[] original = File.ReadAllBytes(source);
        string destination = alias switch
        {
            "relative" => Path.Combine(".", "input.json"),
            "case" => workspace.PathFor("INPUT.JSON"),
            _ => source
        };
        var result = await workspace.Run(command, source, destination);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("input", result.Stderr, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("output", result.Stderr, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(original, File.ReadAllBytes(source));
        Assert.Single(Directory.GetFiles(workspace.DirectoryPath));
    }

    [Theory]
    [InlineData("nodal-displacements.csv")]
    [InlineData("support-reactions.csv")]
    [InlineData("member-end-forces.csv")]
    [InlineData("internal-force-samples.csv")]
    [InlineData("displacement-samples.csv")]
    [InlineData("summary.csv")]
    public async Task Csv_ShouldCheckEveryDestinationBeforeWritingAnyFile(string name)
    {
        using var workspace = new CliTestWorkspace();
        string source = workspace.Save(CliTestWorkspace.Model(), name);
        byte[] original = File.ReadAllBytes(source);
        var result = await workspace.Run("export-csv", source, ".");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("output", result.Stderr, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(original, File.ReadAllBytes(source));
        Assert.Single(Directory.GetFiles(workspace.DirectoryPath));
    }

    public static IEnumerable<object[]> Aliases() => new[]
    {
        "help", "--help", "-h", "example", "examples", "analyze", "analyse", "report", "markdown",
        "export-csv", "csv", "export-xlsx", "xlsx", "export-pdf", "pdf", "export-svg", "svg", "export-html", "html", "export-viewer", "viewer"
    }.Select(alias => new object[] { alias });

    [Theory]
    [MemberData(nameof(Aliases))]
    public async Task Command_ShouldRejectExtraArgumentsBeforeReadingOrWriting(string command)
    {
        using var workspace = new CliTestWorkspace();
        string source = workspace.Save(CliTestWorkspace.Model());
        string destination = workspace.PathFor("existing-output");
        File.WriteAllText(destination, "previous output");
        string[] args = command switch
        {
            "help" or "--help" or "-h" => new[] { command, "UNEXPECTED" },
            "example" or "examples" => new[] { command, "cantilever-point-load", "UNEXPECTED" },
            "analyze" or "analyse" => new[] { command, source, "LC1", "UNEXPECTED" },
            _ => new[] { command, source, destination, "LC1", "UNEXPECTED" }
        };
        byte[] original = File.ReadAllBytes(source);
        var result = await workspace.Run(args);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Unexpected", result.Stderr);
        Assert.Contains("UNEXPECTED", result.Stderr);
        Assert.Equal("previous output", File.ReadAllText(destination));
        Assert.Equal(original, File.ReadAllBytes(source));
        Assert.Equal(2, Directory.GetFiles(workspace.DirectoryPath).Length);
    }

    public static IEnumerable<object[]> MissingArguments() => Aliases().Where(row => !new[] { "help", "--help", "-h" }.Contains((string)row[0]));

    [Theory]
    [MemberData(nameof(MissingArguments))]
    public async Task Command_ShouldRejectMissingArguments(string command)
    {
        using var workspace = new CliTestWorkspace();
        var result = await workspace.Run(command);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Missing", result.Stderr);
        Assert.Empty(Directory.GetFiles(workspace.DirectoryPath));
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task Command_ShouldRejectIncompleteAndNullJsonWithoutCreatingOutput(string command)
    {
        using var workspace = new CliTestWorkspace();
        var model = CliTestWorkspace.Model();
        var load = model["loads"]![0]!.AsObject();
        load["type"] = "LinearDistributedLoad";
        load["targetType"] = "Member";
        load["targetId"] = "M1";
        load["endValue"] = -12;
        load.Remove("value");
        string source = workspace.Save(model);
        string destination = workspace.PathFor("output");
        var result = await RunCommand(workspace, command, source, destination);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("loads", result.Stderr);
        Assert.Contains("value", result.Stderr);
        Assert.False(Path.Exists(destination));

        model["loads"]!.AsArray().Insert(0, null);
        workspace.Save(model);
        result = await RunCommand(workspace, command, source, destination);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("loads[0]", result.Stderr);
        Assert.False(Path.Exists(destination));
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task Command_ShouldHonorCombinationDefaultAndExplicitOverride(string command)
    {
        using var workspace = new CliTestWorkspace();
        var model = CliTestWorkspace.Model();
        model.Remove("loadCaseId");
        model["loadCombinationId"] = "LC1";
        string source = workspace.Save(model);
        string destination = workspace.PathFor("output");
        var result = await RunCommand(workspace, command, source, destination);
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("combination", result.Stderr, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LC1", result.Stderr);
        Assert.False(Path.Exists(destination));

        model["loadCombinationId"] = "MISSING";
        workspace.Save(model);
        result = await RunCommand(workspace, command, source, destination);
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("combination", result.Stderr, StringComparison.OrdinalIgnoreCase);
        Assert.False(Path.Exists(destination));

        result = await RunCommand(workspace, command, source, destination, "LC1");
        result.AssertSuccess();
        AssertReaction(command, destination, result, 12, expectedMz: 48);
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task Commands_ShouldAgreeForSelectedCasesAndCombinations(string command)
    {
        using var workspace = new CliTestWorkspace();
        var model = CliTestWorkspace.Model("trusses/simple-truss.json");
        model["loadCases"]!.AsArray().Add(JsonNode.Parse("""{"id":"OTHER","name":"Unused moment"}"""));
        model["loads"]!.AsArray().Add(JsonNode.Parse("""{"id":"MZ","loadCaseId":"OTHER","type":"NodalMoment","targetType":"Node","targetId":"A","direction":"MomentZ","value":10}"""));
        model["loadCombinations"] = JsonNode.Parse("""[{"id":"COMBO","name":"Twice LC1","terms":[{"loadCaseId":"LC1","factor":2}]},{"id":"BAD_COMBO","name":"Includes moment","terms":[{"loadCaseId":"OTHER","factor":1}]}]""");
        string source = workspace.Save(model);
        string destination = workspace.PathFor("output");
        foreach (var (target, expected) in new[] { ("LC1", 15.0), ("COMBO", 30.0) })
        {
            var result = await RunCommand(workspace, command, source, destination, target);
            result.AssertSuccess();
            AssertReaction(command, destination, result, expected);
        }
        foreach (string target in new[] { "OTHER", "BAD_COMBO" })
        {
            var result = await RunCommand(workspace, command, source, workspace.PathFor("invalid-output"), target);
            Assert.Equal(2, result.ExitCode);
            Assert.NotEmpty(result.Stderr);
            Assert.False(Path.Exists(workspace.PathFor("invalid-output")));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Combination_ShouldKeepDefaultPrecedenceAndLegacyCaseSelector(bool legacyCaseSelector)
    {
        using var workspace = new CliTestWorkspace();
        var model = CliTestWorkspace.Model("combinations/load-combination.json");
        model["loadCaseId"] = legacyCaseSelector ? "ULS1" : "G1";
        if (legacyCaseSelector) model.Remove("loadCombinationId");
        var result = await workspace.Run("analyze", workspace.Save(model));
        result.AssertSuccess();
        Assert.Contains("Analysis id: ULS1", result.Stdout);
        AssertReaction("analyze", "", result, 48.75);
    }

    [Theory]
    [MemberData(nameof(Aliases))]
    public async Task CommandsAndAliases_ShouldAcceptValidSyntaxAndReplacePreviousReports(string command)
    {
        using var workspace = new CliTestWorkspace();
        if (command is "help" or "--help" or "-h")
        {
            var result = await workspace.Run(command);
            result.AssertSuccess();
            Assert.Contains("Usage:", result.Stdout);
            return;
        }
        if (command is "example" or "examples")
        {
            var result = await workspace.Run(command, "cantilever-point-load");
            result.AssertSuccess();
            AssertReaction("analyze", "", result, 12, expectedMz: 48);
            return;
        }
        string canonical = command switch
        {
            "analyse" => "analyze", "markdown" => "report", "csv" => "export-csv", "xlsx" => "export-xlsx",
            "pdf" => "export-pdf", "svg" => "export-svg", "html" => "export-html", "viewer" => "export-viewer", _ => command
        };
        string source = workspace.Save(CliTestWorkspace.Model());
        byte[] original = File.ReadAllBytes(source);
        string destination = workspace.PathFor("output");
        foreach (string? target in new string?[] { null, "lc1" })
        {
            var args = new List<string> { command, source };
            if (canonical != "analyze") args.Add(destination);
            if (target is not null) args.Add(target);
            var result = await workspace.Run(args.ToArray());
            result.AssertSuccess();
            AssertReaction(canonical, destination, result, 12, expectedMz: 48);
        }
        Assert.Equal(original, File.ReadAllBytes(source));
    }

    [Fact]
    public async Task NoArguments_ShouldDisplayHelp()
    {
        using var workspace = new CliTestWorkspace();
        var result = await workspace.Run();
        result.AssertSuccess();
        Assert.Contains("Usage:", result.Stdout);
    }

    public static IEnumerable<object[]> Examples() => Directory.GetFiles(CliTestWorkspace.ExamplePath(""), "*.json", SearchOption.AllDirectories)
        .Where(path => Path.GetFileName(Path.GetDirectoryName(path)) != "sections")
        .Select(path => new object[] { Path.GetRelativePath(CliTestWorkspace.ExamplePath(""), path) });

    [Theory]
    [MemberData(nameof(Examples))]
    public async Task DistributedExamples_ShouldAnalyzeAndExportViewer(string relative)
    {
        using var workspace = new CliTestWorkspace();
        string source = CliTestWorkspace.ExamplePath(relative);
        var model = CliTestWorkspace.Model(relative);
        var connected = model["members"]!.AsArray().SelectMany(member => new[] { (string)member!["startNodeId"]!, (string)member["endNodeId"]! }).ToHashSet();
        Assert.All(model["nodes"]!.AsArray(), node => Assert.Contains((string)node!["id"]!, connected));
        (await workspace.Run("analyze", source)).AssertSuccess();
        string destination = workspace.PathFor("viewer.html");
        (await workspace.Run("export-viewer", source, destination)).AssertSuccess();
        Assert.Contains("<svg", File.ReadAllText(destination));
    }

    [Theory]
    [InlineData("SLS_HOUSE")]
    [InlineData("ULS_HOUSE")]
    public async Task HouseExample_ShouldAnalyzeItsCombinations(string target)
    {
        using var workspace = new CliTestWorkspace();
        var result = await workspace.Run("analyze", CliTestWorkspace.ExamplePath("mixed/two-storey-house-balconies.json"), target);
        result.AssertSuccess();
        Assert.Contains($"Analysis id: {target}", result.Stdout);
    }

    private static Task<CliProcessResult> RunCommand(CliTestWorkspace workspace, string command, string source, string destination, string? target = null)
    {
        var args = new List<string> { command, source };
        if (command != "analyze") args.Add(destination);
        if (target is not null) args.Add(target);
        return workspace.Run(args.ToArray());
    }

    private static void AssertReaction(string command, string destination, CliProcessResult result, double expectedFy, double expectedMz = 0)
    {
        string value = expectedFy.ToString("0.000000", CultureInfo.InvariantCulture);
        string moment = expectedMz.ToString("0.000000", CultureInfo.InvariantCulture);
        if (command == "export-xlsx")
        {
            using var archive = ZipFile.OpenRead(destination);
            using var stream = archive.GetEntry("xl/worksheets/sheet3.xml")!.Open();
            var document = XDocument.Load(stream);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var cell = document.Descendants(ns + "c").Single(item => (string?)item.Attribute("r") == "D2");
            Assert.Equal(expectedFy, double.Parse(cell.Element(ns + "v")!.Value, CultureInfo.InvariantCulture), precision: 8);
            var momentCell = document.Descendants(ns + "c").Single(item => (string?)item.Attribute("r") == "E2");
            Assert.Equal(expectedMz, double.Parse(momentCell.Element(ns + "v")!.Value, CultureInfo.InvariantCulture), precision: 8);
            return;
        }
        string text = command switch
        {
            "analyze" => result.Stdout,
            "export-csv" => File.ReadAllText(Path.Combine(destination, "support-reactions.csv")),
            "export-pdf" => string.Join(" ", Regex.Matches(Encoding.ASCII.GetString(File.ReadAllBytes(destination)),
                @"\((?<text>(?:\\.|[^\\\r\n)])*)\) Tj").Select(match => match.Groups["text"].Value)),
            _ => File.ReadAllText(destination)
        };
        if (command == "report") Assert.Contains($"| `SA` | `A` | 0.000000 | {value} | {moment} |", text);
        else if (command == "export-csv") Assert.Contains($",SA,A,0.000000,{value},{moment}", text);
        else if (command is "analyze" or "export-pdf") Assert.Matches($@"SA\s+A\s+-?0\.000000\s+{Regex.Escape(value)}\s+{Regex.Escape(moment)}", text);
        else Assert.Contains($"Ry = {expectedFy.ToString("0.###", CultureInfo.InvariantCulture)} kN", text);
    }
}
