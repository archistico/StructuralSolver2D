namespace StructuralSolver2D.Cli.Tests;

public sealed class TimeConvergenceCliM591Tests
{
    [Fact]
    public async Task M591_ProducesMarkdownAndCsv_WithoutAlteringStaticValidation()
    {
        using var workspace = new CliTestWorkspace();
        string model=CliTestWorkspace.ExamplePath("dynamics/m59-time-cantilever.json");
        var stdout=await workspace.Run("time-convergence",model);
        stdout.AssertSuccess();
        Assert.Contains("Matching",stdout.Stdout,StringComparison.OrdinalIgnoreCase);
        string report=workspace.PathFor("convergence.md");
        string csv=workspace.PathFor("convergence.csv");
        (await workspace.Run("time-convergence-report",model,report)).AssertSuccess();
        (await workspace.Run("time-convergence-csv",model,csv)).AssertSuccess();
        Assert.Contains("Natural-frequency resolution",File.ReadAllText(report));
        Assert.Contains("Peak amplitude",File.ReadAllText(report));
        Assert.Contains("MediumFineRelative",File.ReadAllText(csv));
        Assert.NotEqual(0,(await workspace.Run("analyze",model)).ExitCode);
    }
}
