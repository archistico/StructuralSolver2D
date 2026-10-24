namespace StructuralSolver2D.Cli.Tests;

public sealed class TimeHistoryCliM59Tests
{
    [Fact]
    public async Task M59_CommandsExportFiles_AndStaticContractStillRejectsLoadlessModel()
    {
        using var workspace=new CliTestWorkspace();
        string model=CliTestWorkspace.ExamplePath("dynamics/m59-time-cantilever.json");
        var summary=await workspace.Run("time",model);
        summary.AssertSuccess();
        Assert.Contains("Newmark",summary.Stdout);
        string md=workspace.PathFor("time.md"), html=workspace.PathFor("time.html"), csv=workspace.PathFor("csv");
        (await workspace.Run("time-report",model,md)).AssertSuccess();
        (await workspace.Run("time-csv",model,csv)).AssertSuccess();
        (await workspace.Run("time-viewer",model,html)).AssertSuccess();
        Assert.Contains("M59",File.ReadAllText(md));
        Assert.Contains("TimeSeconds,Step,NodeId",File.ReadAllText(Path.Combine(csv,"time-nodes.csv")));
        Assert.Contains("BalanceResidual_kNm",File.ReadAllText(Path.Combine(csv,"time-energy.csv")));
        Assert.Contains("id=\"step\"",File.ReadAllText(html));
        Assert.NotEqual(0,(await workspace.Run("analyze",model)).ExitCode);
    }
}
