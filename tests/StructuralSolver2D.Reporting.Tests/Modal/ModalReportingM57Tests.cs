using System.Globalization;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Reporting.Modal;
using StructuralSolver2D.Reporting.Visualization;

namespace StructuralSolver2D.Reporting.Tests.Modal;

public sealed class ModalReportingM57Tests
{
    private static (StructuralModel, StructuralModalAnalysisOutput) Fixture()
    {
        var model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddMember(new StructuralMember("beam", "A", "B", "MAT", "SEC"));
        var mode = new StructuralModalModeResult(1, 2 * Math.PI * 12.5,
            new[] { new StructuralModalNodeShape("A", 0, 0, 0),
                    new StructuralModalNodeShape("B", 0.2, 1, 0.4) })
        { RelativeResidual = 1e-12, RayleighQuotient = Math.Pow(2 * Math.PI * 12.5, 2) };
        return (model, new StructuralModalAnalysisOutput(new[] {mode})
        { AvailableModeCount = 3, Warnings = new[] { "Illustrative modes only." } });
    }

    [Fact]
    public void ModalTables_AreInvariantAndContainProperUnits()
    {
        var (_, output) = Fixture();
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("it-IT");
            var export = new ModalReportExporter();
            string csv = export.ExportSummaryCsv(output);
            Assert.Contains("12.5", csv);
            Assert.DoesNotContain("12,5", csv);
            Assert.Contains("FrequencyHz", csv);
            Assert.Contains("NormalizedRz", export.ExportShapesCsv(output));
            string md = export.ExportMarkdown(output, "First test");
            Assert.Contains("Mass: phi^T M phi = 1", md);
            Assert.Contains("**Modal normalization:** " + output.Normalization, md);
            Assert.Contains("not physical response displacements", md);
            Assert.Contains("12.5", md);
            Assert.Contains("Illustrative modes only", md);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Fact]
    public void ModalMarkdown_UsesProvidedNormalizationContract()
    {
        var (_, baseOutput) = Fixture();
        var output = baseOutput with { Normalization = "Mass: phi^T M phi = 1; sign arbitrary; regression marker" };
        string md = new ModalReportExporter().ExportMarkdown(output);
        Assert.Contains("**Modal normalization:** " + output.Normalization, md);
        Assert.Contains("Component amplitudes and signs are not physical response displacements", md);
    }

    [Fact]
    public void ModalHtml_IsStandaloneDeterministicAndNotStaticAnimation()
    {
        var (model, output) = Fixture();
        var exporter = new InteractiveHtmlModalViewerExporter();
        string html = exporter.Export(model, output, "Modal display");
        Assert.Equal(html, exporter.Export(model, output, "Modal display"));
        Assert.Contains("modal-mode", html);
        Assert.DoesNotContain("<script src=", html);
        Assert.Contains("modal-data", html);
        Assert.Contains("Modal display", html);
        Assert.Contains("Hermite", html, StringComparison.OrdinalIgnoreCase); // Viewer JavaScript documents interpolation.
        Assert.Contains("amplitude and playback speed", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Play deformation", html);
    }

    [Fact]
    public void ModalViewer_EncodesUntrustedLabelsAndRejectsMissingShapes()
    {
        var (model, output) = Fixture();
        string html = new InteractiveHtmlModalViewerExporter().Export(model, output, "<script>alert(1)</script>");
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;", html);
        var invalid = new StructuralModalAnalysisOutput(new[] {
            new StructuralModalModeResult(1, 1, new[] { new StructuralModalNodeShape("A", 1, 0, 0) })
        });
        Assert.Throws<ArgumentException>(() => new InteractiveHtmlModalViewerExporter().Export(model, invalid));
    }
}
