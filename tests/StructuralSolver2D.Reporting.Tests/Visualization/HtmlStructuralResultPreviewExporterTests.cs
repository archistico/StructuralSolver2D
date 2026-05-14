using StructuralSolver2D.Reporting.Visualization;

namespace StructuralSolver2D.Reporting.Tests.Visualization;

public sealed class HtmlStructuralResultPreviewExporterTests
{
    [Fact]
    public void Export_ShouldRenderStandaloneHtmlWithEmbeddedSvg()
    {
        StructuralVisualizationModel model = CreateVisualizationModel();

        string html = new HtmlStructuralResultPreviewExporter().Export(
            model,
            new HtmlPreviewExportOptions
            {
                Title = "Bridge truss preview",
                Description = "Static educational preview.",
                SvgOptions = new SvgExportOptions
                {
                    Width = 900.0,
                    Height = 600.0,
                    IncludeLegend = false,
                },
            });

        Assert.Contains("<!DOCTYPE html>", html, StringComparison.Ordinal);
        Assert.Contains("Bridge truss preview", html, StringComparison.Ordinal);
        Assert.Contains("Static educational preview.", html, StringComparison.Ordinal);
        Assert.Contains("<svg", html, StringComparison.Ordinal);
        Assert.Contains("<dt>Nodes</dt><dd>2</dd>", html, StringComparison.Ordinal);
    }

    private static StructuralVisualizationModel CreateVisualizationModel() =>
        new(
            new[]
            {
                new VisualizationNode("N1", "N1", new VisualizationPoint(0.0, 0.0), new VisualizationPoint(0.0, 0.0), 0.0, 0.0, 0.0),
                new VisualizationNode("N2", "N2", new VisualizationPoint(3.0, 1.0), new VisualizationPoint(3.0, 0.9), 0.0, -0.001, 0.0),
            },
            new[]
            {
                new VisualizationMember("T1", "N1", "N2", StructuralSolver2D.Core.Model.Enums.MemberType.Truss2D, new VisualizationPoint(0.0, 0.0), new VisualizationPoint(3.0, 1.0)),
            },
            new[]
            {
                new DeformedMemberShape("T1", new[]
                {
                    new VisualizationPoint(0.0, 0.0),
                    new VisualizationPoint(3.0, 0.9),
                }),
            },
            Array.Empty<MemberDiagramPolyline>(),
            new VisualizationBounds(-0.2, -0.2, 3.2, 1.2),
            50.0);
}
