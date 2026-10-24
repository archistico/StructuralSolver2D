using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Reporting.Seismic;

namespace StructuralSolver2D.Reporting.Tests.Seismic;

public sealed class SeismicReportingM70Tests
{
    [Fact]
    public void CqcReports_UseCqcHeaderDampingAndNonNormativeDisclaimer()
    {
        var mode = new StructuralSeismicModeParticipation(1, 2, .5, 1, 1, 20, 20, 2, .01266);
        var output = new StructuralSeismicSpectrumOutput(SeismicGroundDirection.GlobalX, 5, 1, 20,
            new[] { mode }, new[] { new StructuralSeismicNodeSpectrumEnvelope("N1", .01, .02, .003) },
            new[] { "Warning" }, SeismicModalCombination.Cqc, .05);
        var exporter = new SeismicReportExporter();
        string md = exporter.ExportSpectrumMarkdown(output, "M70");
        Assert.Contains("CQC", md);
        Assert.Contains("0.05", md); // output uses invariant numeric formatting
        Assert.Contains("NON-NORMATIVE", md);
        Assert.Contains("Warning", md);
        Assert.Contains("CQCAbsUx_m", exporter.ExportSpectrumNodesCsv(output));
        Assert.DoesNotContain("SRSSAbsUx_m", exporter.ExportSpectrumNodesCsv(output));
    }

    [Fact]
    public void DefaultSrssExport_RemainsBackwardCompatible()
    {
        var output = new StructuralSeismicSpectrumOutput(SeismicGroundDirection.GlobalX, 5, 1, 20,
            new[] { new StructuralSeismicModeParticipation(1, 2, .5, 1, 1, 20, 20, 2, .01) },
            new[] { new StructuralSeismicNodeSpectrumEnvelope("N1", 1, 2, 3) }, Array.Empty<string>());
        Assert.Contains("SRSSAbsUy_m", new SeismicReportExporter().ExportSpectrumNodesCsv(output));
        Assert.Contains("SRSS", new SeismicReportExporter().ExportSpectrumMarkdown(output, "Legacy"));
    }
}
