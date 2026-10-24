using System.Text.Json.Nodes;

namespace StructuralSolver2D.Cli.Tests;

public sealed class SeismicCliM70Tests
{
    [Fact]
    public async Task CqcFixture_ExportsCqcMarkdownAndCsv_WithoutChangingLegacySrss()
    {
        using var workspace = new CliTestWorkspace();
        string sample = CliTestWorkspace.ExamplePath("dynamics/m70-seismic-cqc-cantilever.json");
        (await workspace.Run("seismic-spectrum", sample)).AssertSuccess();
        string report = workspace.PathFor("m70.md");
        string folder = workspace.PathFor("m70-csv");
        (await workspace.Run("seismic-spectrum-report", sample, report)).AssertSuccess();
        (await workspace.Run("seismic-spectrum-csv", sample, folder)).AssertSuccess();
        string markdown = File.ReadAllText(report);
        Assert.Contains("CQC", markdown);
        Assert.Contains("NON-NORMATIVE", markdown);
        Assert.Contains("CQCAbsUy_m", File.ReadAllText(Path.Combine(folder, "seismic-nodes.csv")));
        Assert.NotEqual(0, (await workspace.Run("analyze", sample)).ExitCode);
        string original = CliTestWorkspace.ExamplePath("dynamics/m60-seismic-cantilever.json");
        (await workspace.Run("seismic-spectrum", original)).AssertSuccess();
        string legacyFolder = workspace.PathFor("m60-csv");
        (await workspace.Run("seismic-spectrum-csv", original, legacyFolder)).AssertSuccess();
        Assert.Contains("SRSSAbsUy_m", File.ReadAllText(Path.Combine(legacyFolder, "seismic-nodes.csv")));
    }

    [Fact]
    public async Task CqcFixture_RejectsMissingOrInvalidDampingAndMethod()
    {
        using var workspace = new CliTestWorkspace();
        JsonObject input = CliTestWorkspace.Model("dynamics/m70-seismic-cqc-cantilever.json");
        JsonObject spectrum = (JsonObject)input["seismic"]!["spectrum"]!;
        spectrum.Remove("modalDampingRatio");
        var missing = await workspace.Run("seismic-spectrum", workspace.Save(input, "missing.json"));
        Assert.NotEqual(0, missing.ExitCode);
        Assert.Contains("M70_CQC_DAMPING", missing.Stderr);
        spectrum["modalDampingRatio"] = .05;
        spectrum["combination"] = "Invalid";
        var invalid = await workspace.Run("seismic-spectrum", workspace.Save(input, "invalid.json"));
        Assert.NotEqual(0, invalid.ExitCode);
        Assert.Contains("M70_COMBINATION", invalid.Stderr);
    }
}
