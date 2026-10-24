using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Reporting.Transient;

namespace StructuralSolver2D.Reporting.Tests.Transient;

public sealed class TimeConvergenceReportingM591Tests
{
    [Fact]
    public void MarkdownAndCsv_HaveUnitsAndDistinguishNumericalHeuristics()
    {
        var result = new StructuralTimeConvergenceOutput(.2,.002,100,
            new[]{new StructuralTimeComponentComparison("Uy","m",true,.08,.02,.1,2)},
            new StructuralTimePeakComparison("B","Uy","m",.001,.1,.00105,.11,.00106,.112,
                .0566,.00943),
            new[]{new StructuralTimeFrequencyResolution(1,20,.04,.02,.01)},
            new[]{"M591_SCOPE: Diagnostic only"}, true, false, true, true);
        var exporter=new TimeConvergenceReportExporter();
        string md=exporter.ExportMarkdown(result,"Example");
        string csv=exporter.ExportCsv(result);
        Assert.Contains("dt/4", md);
        Assert.Contains("Medium", md, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Fine-grid envelope", md);
        Assert.Contains("sampled", md, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Frequency [Hz]", md);
        Assert.Contains("M591_SCOPE", md);
        Assert.Contains("CoarseMediumRelative", csv);
        Assert.Contains("Uy,m,true",csv);
    }
}
