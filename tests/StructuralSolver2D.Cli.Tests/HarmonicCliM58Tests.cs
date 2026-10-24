namespace StructuralSolver2D.Cli.Tests;

public sealed class HarmonicCliM58Tests
{
    private const string Fixture = "dynamics/m58-harmonic-cantilever.json";

    [Fact]
    public async Task HarmonicCommands_RunOnModalOnlyInputAndKeepStaticRejection()
    {
        using var workspace = new CliTestWorkspace();
        string source = CliTestWorkspace.ExamplePath(Fixture);
        var output = await workspace.Run("harmonic", source);
        output.AssertSuccess();
        Assert.Contains("Frequency [Hz]", output.Stdout);
        Assert.Contains("sin(Omega*t)", output.Stdout);

        string md = workspace.PathFor("harmonic.md");
        string csv = workspace.PathFor("harmonic.csv");
        string html = workspace.PathFor("harmonic.html");
        (await workspace.Run("harmonic-report", source, md)).AssertSuccess();
        (await workspace.Run("harmonic-csv", source, csv)).AssertSuccess();
        (await workspace.Run("harmonic-viewer", source, html)).AssertSuccess();
        Assert.Contains("Phase [deg]", File.ReadAllText(md));
        Assert.Contains("PhaseDegrees", File.ReadAllText(csv));
        Assert.Contains("id=\"phase\"", File.ReadAllText(html));
        var legacy = await workspace.Run("analyze", source);
        Assert.NotEqual(0, legacy.ExitCode);
        Assert.Contains("at least one load case", legacy.Stderr);
    }

    [Fact]
    public async Task Harmonic_CannotRunWithoutExplicitDampingOrLoads()
    {
        using var workspace = new CliTestWorkspace();
        var model = CliTestWorkspace.Model(Fixture);
        model["harmonic"]!.AsObject().Remove("damping");
        string file = workspace.Save(model);
        var result = await workspace.Run("harmonic", file);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("M58_DAMPING_REQUIRED", result.Stderr);
    }
}
