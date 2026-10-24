using System.Text.Json.Nodes;

namespace StructuralSolver2D.Cli.Tests;

public sealed class NonlinearCliM69Tests
{
    private static readonly string Root = FindRoot();

    [Theory]
    [InlineData("static")]
    [InlineData("time")]
    public async Task StandaloneViewer_FromPublicJson_ExportsAcceptedPhysicalHistory(string mode)
    {
        using var work = new CliTestWorkspace();
        string fixture = Path.Combine(Root, "docs", "structural", "fixtures", $"m68-nonlinear-{mode}.json");
        string destination = work.PathFor($"m69-{mode}.html");
        var process = await work.Run($"nonlinear-{mode}-viewer", fixture, destination);
        process.AssertSuccess();
        string html = File.ReadAllText(destination);
        Assert.Contains("M69 · Viewer non lineare", html);
        Assert.Contains("\"completed\":true", html);
        Assert.Contains($"\"mode\":\"{mode}\"", html);
        Assert.Contains("id=\"load-chart\"", html);
        Assert.Contains("id=\"member\"", html);
        Assert.Contains("id=\"plastic-chart\"", html);
    }

    [Fact]
    public async Task WrongModeAndInputOutputCollision_AreRejected()
    {
        using var work = new CliTestWorkspace();
        string input = Path.Combine(Root, "docs", "structural", "fixtures", "m68-nonlinear-static.json");
        var wrongMode = await work.Run("nonlinear-time-viewer", input, work.PathFor("invalid.html"));
        Assert.NotEqual(0, wrongMode.ExitCode);
        Assert.Contains("M68_MODE", wrongMode.Stderr);
        var collision = await work.Run("nonlinear-static-viewer", input, input);
        Assert.NotEqual(0, collision.ExitCode);
        Assert.Contains("M68_OUTPUT", collision.Stderr);
    }

    [Fact]
    public async Task PartialHistory_IsExportedButCliReturnsFailure()
    {
        using var work = new CliTestWorkspace();
        var obj = JsonNode.Parse(File.ReadAllText(Path.Combine(Root, "docs", "structural", "fixtures", "m68-nonlinear-static.json")))!.AsObject();
        obj["nonlinearTruss2D"]!["maxIterations"] = 1;
        string input = work.Save(obj, "partial.json");
        string path = work.PathFor("partial.html");
        var execution = await work.Run("nonlinear-static-viewer", input, path);
        Assert.Equal(2, execution.ExitCode);
        string html = File.ReadAllText(path);
        Assert.Contains("RISULTATO PARZIALE", html);
        Assert.Contains("\"completed\":false", html);
    }

    private static string FindRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "StructuralSolver2D.sln")))return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("M69 tests require project solution root.");
    }
}
