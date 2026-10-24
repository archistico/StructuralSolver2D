using System.Text.Json.Nodes;

namespace StructuralSolver2D.Cli.Tests;

public sealed class NonlinearCliM68Tests
{
    private const string StaticModel = """
        {
          "title": "M68 two-node nonlinear bar",
          "nonlinearTruss2D": {
            "mode": "static",
            "model": {
              "nodes": [{"id":"A","x":0,"y":0},{"id":"B","x":1,"y":0}],
              "members": [{"id":"AB","startNodeId":"A","endNodeId":"B","area":1,
                 "elasticModulus":1000,"yieldStress":5,"hardeningModulus":100,"massPerLength":1}],
              "supports": [{"nodeId":"A","normalX":1,"normalY":0},
                           {"nodeId":"A","normalX":0,"normalY":1},
                           {"nodeId":"B","normalX":0,"normalY":1}],
              "loads": [{"nodeId":"B","fx":2,"fy":0}]
            },
            "loadFactors": [1,4]
          }
        }
        """;
    private const string TimeModel = """
        {
          "title": "M68 dynamic nonlinear bar",
          "nonlinearTruss2D": {
            "mode": "time",
            "model": {
              "nodes": [{"id":"A","x":0,"y":0},{"id":"B","x":1,"y":0}],
              "members": [{"id":"AB","startNodeId":"A","endNodeId":"B","area":1,
                 "elasticModulus":1000,"yieldStress":5,"hardeningModulus":100,"massPerLength":1}],
              "supports": [{"nodeId":"A","normalX":1,"normalY":0},
                           {"nodeId":"A","normalX":0,"normalY":1},
                           {"nodeId":"B","normalX":0,"normalY":1}],
              "loads": [{"nodeId":"B","fx":2,"fy":0}]
            },
            "loadFactors": [0,1,0,0],
            "timeStep": 0.01
          }
        }
        """;

    [Theory]
    [InlineData("static")]
    [InlineData("time")]
    public async Task Cli_ExportsMarkdownAndBothCsvFamilies(string mode)
    {
        using var work = new CliTestWorkspace();
        string input = work.Save(JsonNode.Parse(mode == "static" ? StaticModel : TimeModel)!.AsObject());
        (await work.Run($"nonlinear-{mode}", input)).AssertSuccess();
        string markdown = work.PathFor("result.md");
        (await work.Run($"nonlinear-{mode}-report", input, markdown)).AssertSuccess();
        Assert.Contains("Analysis completed:** YES", File.ReadAllText(markdown));
        string directory = work.PathFor("result-csv");
        (await work.Run($"nonlinear-{mode}-csv", input, directory)).AssertSuccess();
        Assert.Contains("NodeId", File.ReadAllText(Path.Combine(directory, "nonlinear-nodes.csv")));
        Assert.Contains("PlasticStrain", File.ReadAllText(Path.Combine(directory, "nonlinear-members.csv")));
        Assert.Contains("YES", File.ReadAllText(Path.Combine(directory, "nonlinear-status.md")));
    }

    [Fact]
    public async Task Cli_RejectsCrossModeAndUnknownJsonFields()
    {
        using var work = new CliTestWorkspace();
        string input = work.Save(JsonNode.Parse(StaticModel)!.AsObject());
        var cross = await work.Run("nonlinear-time", input);
        Assert.NotEqual(0, cross.ExitCode);
        Assert.Contains("M68_MODE", cross.Stderr);
        var bad = JsonNode.Parse(StaticModel)!.AsObject();
        bad["nonlinearTruss2D"]!["unsupportedFeature"] = true;
        string path = work.Save(bad, "invalid.json");
        var unknown = await work.Run("nonlinear-static", path);
        Assert.NotEqual(0, unknown.ExitCode);
        Assert.Contains("Error", unknown.Stderr);
    }

    [Fact]
    public async Task Cli_StaticDoesNotAcceptTimeOnlyOptions()
    {
        using var work = new CliTestWorkspace();
        var bad = JsonNode.Parse(StaticModel)!.AsObject();
        bad["nonlinearTruss2D"]!["timeStep"] = 0.01;
        string path = work.Save(bad);
        var result = await work.Run("nonlinear-static", path);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("M68_STATIC_FIELDS", result.Stderr);
    }

    [Fact]
    public async Task Cli_NonconvergentHistory_ReturnsNonzeroAndExportsExplicitPartialStatus()
    {
        using var work = new CliTestWorkspace();
        var model = JsonNode.Parse(StaticModel)!.AsObject();
        model["nonlinearTruss2D"]!["maxIterations"] = 1;
        string input = work.Save(model);
        string target = work.PathFor("partial.md");
        var result = await work.Run("nonlinear-static-report", input, target);
        Assert.Equal(2, result.ExitCode);
        Assert.True(File.Exists(target));
        string report = File.ReadAllText(target);
        Assert.Contains("NO — partial accepted history only", report);
        Assert.Contains("M64_NONCONVERGENCE", report);
        Assert.Contains("| 0 | 1 |", report);
        Assert.DoesNotContain("| 1 | 4 |", report);
    }

    [Fact]
    public async Task Cli_ExistingStaticCommand_DoesNotMisinterpretNonlinearJson()
    {
        using var work = new CliTestWorkspace();
        string path = work.Save(JsonNode.Parse(StaticModel)!.AsObject());
        var result = await work.Run("analyze", path);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Error", result.Stderr);
    }
}
