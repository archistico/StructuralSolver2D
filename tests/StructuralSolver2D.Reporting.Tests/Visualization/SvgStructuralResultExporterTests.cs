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
                IncludeNodeDisplacementLabels = true,
                IncludeMemberDisplacementLabels = true,
            });

        Assert.Contains("<svg", svg, StringComparison.Ordinal);
        Assert.Contains("Portal frame preview", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"member undeformed\"", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"member deformed\"", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"diagram normal-force\"", svg, StringComparison.Ordinal);
        Assert.Contains(">A<", svg, StringComparison.Ordinal);
        Assert.Contains("Simple support", svg, StringComparison.Ordinal);
        Assert.Contains("data-orientation-degrees=\"30\"", svg, StringComparison.Ordinal);
        Assert.Contains("rotate(-30", svg, StringComparison.Ordinal);
        Assert.Contains("Rx = 10", svg, StringComparison.Ordinal);
        Assert.Contains("Ry = 25", svg, StringComparison.Ordinal);
        Assert.Contains("Mz = 12", svg, StringComparison.Ordinal);
        Assert.Contains("L = 4", svg, StringComparison.Ordinal);
        Assert.Contains("umax = 2 mm", svg, StringComparison.Ordinal);
        Assert.Contains("Uy = -2 mm", svg, StringComparison.Ordinal);
        Assert.Contains("Rz = 0.01 rad", svg, StringComparison.Ordinal);
        Assert.Contains("B: u = 2 mm", svg, StringComparison.Ordinal);
        Assert.Contains("M1 L/4: u = 0.5 mm", svg, StringComparison.Ordinal);
        Assert.Contains("M1 L/2: u = 1 mm", svg, StringComparison.Ordinal);
        Assert.Contains("M1 3L/4: u = 1.5 mm", svg, StringComparison.Ordinal);
        Assert.Contains("Nmax = 12", svg, StringComparison.Ordinal);
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
            100.0,
            new[]
            {
                new VisualizationSupport("S1", "A", SupportGlyphKind.SimpleSupport, new VisualizationPoint(0.0, 0.0), null, 30.0),
            },
            new[]
            {
                new VisualizationReactionArrow("S1", "A", ReactionComponentKind.ForceX, new VisualizationPoint(0.0, 0.0), new VisualizationPoint(0.4, 0.0), 10.0),
                new VisualizationReactionArrow("S1", "A", ReactionComponentKind.ForceY, new VisualizationPoint(0.0, 0.0), new VisualizationPoint(0.0, 0.8), 25.0),
            },
            new[]
            {
                new VisualizationReactionMoment("S1", "A", new VisualizationPoint(0.0, 0.0), 0.3, false, 12.0),
            },
            new[]
            {
                new MemberDimensionAnnotation("M1", new VisualizationPoint(0.0, 0.0), new VisualizationPoint(4.0, 0.0), 4.0),
            },
            new VisualizationDisplacementAnnotation("B", new VisualizationPoint(4.0, 0.0), new VisualizationPoint(4.0, -0.2), 0.002, 0.0, -0.002, 0.01),
            new[]
            {
                new DiagramValueAnnotation("M1", VisualizationDiagramKind.NormalForce, new VisualizationPoint(2.0, 0.4), 12.0, 12.0),
            },
            null,
            new[]
            {
                new VisualizationNodeDisplacementLabel("B", "B", new VisualizationPoint(4.0, -0.2), 0.0, -0.002, 0.002, 0.01),
            },
            new[]
            {
                new VisualizationMemberDisplacementLabel("M1", "L/4", 0.25, 1.0, new VisualizationPoint(1.0, -0.05), 0.0, -0.0005, 0.0005, 0.0025),
                new VisualizationMemberDisplacementLabel("M1", "L/2", 0.50, 2.0, new VisualizationPoint(2.0, -0.10), 0.0, -0.0010, 0.0010, 0.0050),
                new VisualizationMemberDisplacementLabel("M1", "3L/4", 0.75, 3.0, new VisualizationPoint(3.0, -0.15), 0.0, -0.0015, 0.0015, 0.0075),
            });
}
