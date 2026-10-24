namespace StructuralSolver2D.Cli.Tests;

public sealed class SeismicCliM60Tests
{
    [Fact]
    public async Task SeismicCommands_ExportFullResultsWithoutChangingStaticContracts()
    {
        using var workspace = new CliTestWorkspace();
        string example = CliTestWorkspace.ExamplePath("dynamics/m60-seismic-cantilever.json");
        var time = await workspace.Run("seismic-time", example);
        time.AssertSuccess();
        Assert.Contains("NON-NORMATIVE", time.Stdout);
        var spec = await workspace.Run("seismic-spectrum", example);
        spec.AssertSuccess();
        Assert.Contains("SRSS", spec.Stdout);
        string md = workspace.PathFor("seismic.md");
        string csv = workspace.PathFor("seismic.csv");
        string specMd = workspace.PathFor("spectrum.md");
        string specCsv = workspace.PathFor("spectrum-csv");
        (await workspace.Run("seismic-time-report", example, md)).AssertSuccess();
        (await workspace.Run("seismic-time-csv", example, csv)).AssertSuccess();
        (await workspace.Run("seismic-spectrum-report", example, specMd)).AssertSuccess();
        (await workspace.Run("seismic-spectrum-csv", example, specCsv)).AssertSuccess();
        Assert.Contains("NON-NORMATIVE", File.ReadAllText(md));
        Assert.Contains("AbsoluteAy_ms2", File.ReadAllText(csv));
        Assert.Contains("SRSS", File.ReadAllText(specMd));
        Assert.Contains("EffectiveMass_kNs2m", File.ReadAllText(Path.Combine(specCsv,"seismic-modes.csv")));
        Assert.Contains("SRSSAbsUy_m", File.ReadAllText(Path.Combine(specCsv,"seismic-nodes.csv")));
        Assert.NotEqual(0, (await workspace.Run("analyze", example)).ExitCode);
    }
}
