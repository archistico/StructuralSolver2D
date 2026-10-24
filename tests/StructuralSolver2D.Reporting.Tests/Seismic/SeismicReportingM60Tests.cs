using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Reporting.Seismic;

namespace StructuralSolver2D.Reporting.Tests.Seismic;

public sealed class SeismicReportingM60Tests
{
    [Fact]
    public void Exporters_DeclareNonNormativeScopeAndPhysicalUnits()
    {
        var mode = new StructuralSeismicModeParticipation(1, 2, .5, 1, 1, 20, 20, 2, .01266);
        var spectrum = new StructuralSeismicSpectrumOutput(SeismicGroundDirection.GlobalX, 5, 1, 20,
            new[] { mode }, new[] { new StructuralSeismicNodeSpectrumEnvelope("N1", .01, .02, .003) },
            new[] { "Example warning" });
        var exporter = new SeismicReportExporter();
        string md = exporter.ExportSpectrumMarkdown(spectrum, "Test");
        Assert.Contains("NON-NORMATIVE", md);
        Assert.Contains("SRSS", md);
        Assert.Contains("Sa [m/s²]", md);
        Assert.Contains("Example warning", md);
        Assert.Contains("EffectiveMass_kNs2m", exporter.ExportSpectrumModesCsv(spectrum));
        Assert.Contains("SRSSAbsUx_m", exporter.ExportSpectrumNodesCsv(spectrum));
    }
}
