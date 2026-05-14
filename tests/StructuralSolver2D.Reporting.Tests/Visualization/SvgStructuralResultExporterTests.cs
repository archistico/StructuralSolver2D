using StructuralSolver2D.Reporting.Visualization;

namespace StructuralSolver2D.Reporting.Tests.Visualization;

public sealed class SvgStructuralResultExporterTests
{
    [Fact]
    public void Export_ShouldRenderStandaloneSvgDocument()
    {
        StructuralVisualizationModel model = CreateVisualizationModel();

        string svg = new SvgStructuralResultExporter().Export(
            model,
            new SvgExportOptions
            {
                Width = 800.0,
                Height = 600.0,
                Title = "Portal frame preview",
                IncludeNodeLabels = true,
                IncludeLegend = true,
            });

        Assert.Contains("<svg", svg, StringComparison.Ordinal);
        Assert.Contains("Portal frame preview", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"member undeformed\"", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"member deformed\"", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"diagram normal-force\"", svg, StringComparison.Ordinal);
        Assert.Contains(">A<", svg, StringComparison.Ordinal);
        Assert.Contains("Legend", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_WithInvalidOptions_ShouldThrowClearException()
    {
        StructuralVisualizationModel model = CreateVisualizationModel();
        var options = new SvgExportOptions { Width = 0.0 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SvgStructuralResultExporter().Export(model, options));
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
                    new VisualizationPoint(2.0, -0.1),
                    new VisualizationPoint(4.0, -0.2),
                }),
            },
            new[]
            {
                new MemberDiagramPolyline("M1", VisualizationDiagramKind.NormalForce, new[]
                {
                    new VisualizationPoint(0.0, 0.0),
                    new VisualizationPoint(2.0, 0.4),
                    new VisualizationPoint(4.0, 0.0),
                }, 12.0),
            },
            new VisualizationBounds(-0.5, -0.5, 4.5, 1.0),
            100.0);
}
