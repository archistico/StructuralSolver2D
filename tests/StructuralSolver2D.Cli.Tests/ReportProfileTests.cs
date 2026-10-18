namespace StructuralSolver2D.Cli.Tests;

public sealed class ReportProfileTests
{
    [Fact]
    public async Task Report_ShouldDefaultToSummaryAndAllowExplicitFullReport()
    {
        using var workspace = new CliTestWorkspace();
        string source = workspace.Save(CliTestWorkspace.Model());
        string summaryPath = workspace.PathFor("summary.md"), fullPath = workspace.PathFor("full.md");
        (await workspace.Run("report", source, summaryPath)).AssertSuccess();
        (await workspace.Run("report-full", source, fullPath)).AssertSuccess();
        string summary = File.ReadAllText(summaryPath), full = File.ReadAllText(fullPath);
        Assert.Contains("## Executive summary", summary);
        Assert.Contains("### Support reactions", summary);
        Assert.DoesNotContain("### Nodes", summary);
        Assert.DoesNotContain("## Deformed shape samples", summary);
        Assert.Contains("### Nodes", full);
        Assert.Contains("## Deformed shape samples", full);
        Assert.True(summary.Length < full.Length);
    }
}
