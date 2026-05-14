using StructuralSolver2D.Reporting.Visualization;

namespace StructuralSolver2D.Reporting.Tests.Visualization;

public sealed class InteractiveHtmlStructuralViewerExporterTests
{
    [Fact]
    public void Export_ShouldRenderInteractiveStandaloneViewer()
    {
        StructuralVisualizationModel model = CreateVisualizationModel();

        string html = new InteractiveHtmlStructuralViewerExporter().Export(
            model,
            new InteractiveViewerExportOptions
            {
                Title = "Interactive preview",
                Description = "Pan and zoom test.",
                SvgOptions = new SvgExportOptions
                {
                    Width = 900.0,
                    Height = 600.0,
                },
            });

        Assert.Contains("<!DOCTYPE html>", html, StringComparison.Ordinal);
        Assert.Contains("Interactive preview", html, StringComparison.Ordinal);
        Assert.Contains("id=\"viewerCanvas\"", html, StringComparison.Ordinal);
        Assert.Contains("data-action=\"zoom-in\"", html, StringComparison.Ordinal);
        Assert.Contains("data-action=\"reset\"", html, StringComparison.Ordinal);
        Assert.Contains("data-layer=\"#undeformed-model\"", html, StringComparison.Ordinal);
        Assert.Contains("data-layer=\"#deformed-shape\"", html, StringComparison.Ordinal);
        Assert.Contains("Nodal displacement labels", html, StringComparison.Ordinal);
        Assert.Contains("Member station labels", html, StringComparison.Ordinal);
        Assert.Contains("data-layer=\".displacement-label,.displacement-label-anchor\"", html, StringComparison.Ordinal);
        Assert.Contains("data-layer=\".member-displacement-label,.member-displacement-label-anchor\"", html, StringComparison.Ordinal);
        Assert.Contains("Max disp.", html, StringComparison.Ordinal);
        Assert.Contains("addEventListener('wheel'", html, StringComparison.Ordinal);
        Assert.Contains("pointerdown", html, StringComparison.Ordinal);
        Assert.Contains("<svg", html, StringComparison.Ordinal);
    }

    private static StructuralVisualizationModel CreateVisualizationModel() =>
        new(
            new[]
            {
                new VisualizationNode("A", "A", new VisualizationPoint(0.0, 0.0), new VisualizationPoint(0.0, 0.0), 0.0, 0.0, 0.0),
                new VisualizationNode("B", "B", new VisualizationPoint(4.0, 0.0), new VisualizationPoint(4.0, -0.2), 0.0, -0.002, 0.0),
            },
            new[]
            {
                new VisualizationMember("M1", "A", "B", StructuralSolver2D.Core.Model.Enums.MemberType.Frame2D, new VisualizationPoint(0.0, 0.0), new VisualizationPoint(4.0, 0.0)),
            },
            new[]
            {
                new DeformedMemberShape("M1", new[]
                {
                    new VisualizationPoint(0.0, 0.0),
                    new VisualizationPoint(4.0, -0.2),
                }),
            },
            Array.Empty<MemberDiagramPolyline>(),
            new VisualizationBounds(-0.5, -0.5, 4.5, 1.0),
            100.0,
            new[]
            {
                new VisualizationSupport("S1", "A", SupportGlyphKind.Hinge, new VisualizationPoint(0.0, 0.0), null),
            },
            null,
            null,
            null,
            new VisualizationDisplacementAnnotation("B", new VisualizationPoint(4.0, 0.0), new VisualizationPoint(4.0, -0.2), 0.002, 0.0, -0.002, 0.0),
            null,
            null,
            new[]
            {
                new VisualizationNodeDisplacementLabel("B", "B", new VisualizationPoint(4.0, -0.2), 0.0, -0.002, 0.002, 0.0),
            },
            new[]
            {
                new VisualizationMemberDisplacementLabel("M1", "L/2", 0.50, 2.0, new VisualizationPoint(2.0, -0.1), 0.0, -0.001, 0.001, 0.0),
            });
}
