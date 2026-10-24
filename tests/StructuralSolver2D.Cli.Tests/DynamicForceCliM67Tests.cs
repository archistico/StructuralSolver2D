using StructuralSolver2D.Cli.Tests;

namespace StructuralSolver2D.Cli.Tests;

public sealed class DynamicForceCliM67Tests
{
    [Fact]
    public async Task ForceCommands_ProduceExplicitMarkdownAndInvariantCsv()
    {
        using var work = new CliTestWorkspace();
        string model = CliTestWorkspace.ExamplePath("dynamics/m59-time-cantilever.json");
        var stdout = await work.Run("time-forces", model);
        stdout.AssertSuccess();
        Assert.Contains("M67",stdout.Stdout);
        Assert.Contains("ELASTIC",stdout.Stdout);
        Assert.Contains("M*a + C*v + K*u - F",stdout.Stdout);

        string report = work.PathFor("force-report.md");
        (await work.Run("time-forces-report", model, report)).AssertSuccess();
        Assert.True(File.Exists(report));
        Assert.Contains("M67_REACTION",File.ReadAllText(report));
        string csv = work.PathFor("force-csv");
        (await work.Run("time-forces-csv", model, csv)).AssertSuccess();
        string reactions = File.ReadAllText(Path.Combine(csv,"reactions.csv"));
        string memberEnds = File.ReadAllText(Path.Combine(csv,"elastic-member-ends.csv"));
        Assert.Contains("ReactionFx_kN",reactions);
        Assert.Contains("ElasticM1_kNm",memberEnds);
        Assert.Contains("\"N0\"",reactions);
        Assert.Contains("\"E0\"",memberEnds);
        Assert.DoesNotContain("NaN",reactions);
    }
}
