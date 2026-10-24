using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Reporting.Modal;

namespace StructuralSolver2D.Reporting.Tests.Modal;

public sealed class HarmonicReportingM58Tests
{
    private static StructuralHarmonicAnalysisOutput Example()
    {
        var sample = new StructuralHarmonicFrequencyResult(4,
            new[] { new StructuralHarmonicNodeResult("N1",
                new StructuralHarmonicComponent(.003, -.004),
                new StructuralHarmonicComponent(0, 0),
                new StructuralHarmonicComponent(0, 0)) });
        return new StructuralHarmonicAnalysisOutput(new[] { sample }, new[] { 4d }, new[] { .05d },
            new[] { "Educational use only." });
    }

    [Fact]
    public void Csv_UsesInvariantDecimals_PhaseAndExplicitUnits()
    {
        var csv = new HarmonicReportExporter().ExportCsv(Example());
        Assert.Contains("PhaseDegrees,Real,Imaginary,Unit", csv);
        Assert.Contains("0.005", csv);
        Assert.Contains("\"N1\",Ux", csv);
        Assert.Contains(",m", csv);
        Assert.Contains(",rad", csv);
    }

    [Fact]
    public void Markdown_ExplainsPhysicalPhaseAndLimitations()
    {
        var md = new HarmonicReportExporter().ExportMarkdown(Example());
        Assert.Contains("Re(U) sin", md);
        Assert.Contains("Educational use only", md);
    }

    [Fact]
    public void Viewer_ExposesNodeSelectionAndBothCurves()
    {
        var html = new InteractiveHtmlHarmonicViewerExporter().Export(Example());
        Assert.Contains("id=\"amplitude\"", html);
        Assert.Contains("id=\"phase\"", html);
        Assert.Contains("id=\"node\"", html);
        Assert.Contains("N1", html);
        Assert.DoesNotContain("__M58_DATA__", html);
    }
}
