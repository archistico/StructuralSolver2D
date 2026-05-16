using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Reporting.Visualization;

namespace StructuralSolver2D.Reporting.Tests.Visualization;

public sealed class ViewerSnapshotStabilityTests
{
    [Fact]
    public void SvgAndInteractiveViewerExports_ShouldBeDeterministicAcrossRepeatedRuns()
    {
        StructuralVisualizationModel model = CreateVisualizationModel();
        var svgExporter = new SvgStructuralResultExporter();
        var htmlExporter = new InteractiveHtmlStructuralViewerExporter();
        var svgOptions = new SvgExportOptions
        {
            Width = 640.0,
            Height = 480.0,
            Title = "Snapshot preview",
            IncludeLegend = true,
            IncludeNodeLabels = true,
            IncludeNodeDisplacementLabels = true,
            IncludeMemberDisplacementLabels = true,
        };
        var htmlOptions = new InteractiveViewerExportOptions
        {
            Title = "Snapshot viewer",
            Description = "Deterministic snapshot contract.",
            SvgOptions = svgOptions,
        };

        string firstSvg = NormalizeMarkup(svgExporter.Export(model, svgOptions));
        string secondSvg = NormalizeMarkup(svgExporter.Export(model, svgOptions));
        string firstHtml = NormalizeMarkup(htmlExporter.Export(model, htmlOptions));
        string secondHtml = NormalizeMarkup(htmlExporter.Export(model, htmlOptions));

        Assert.Equal(firstSvg, secondSvg);
        Assert.Equal(firstHtml, secondHtml);
        Assert.DoesNotContain("\r\n", firstSvg, StringComparison.Ordinal);
        Assert.DoesNotContain("\r\n", firstHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Generated:", firstSvg, StringComparison.Ordinal);
        Assert.DoesNotContain("Generated:", firstHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void SvgExport_ForLoadCombination_ShouldKeepStableFactoredLoadSnapshotContract()
    {
        StructuralVisualizationModel model = CreateFactoredCombinationVisualizationModel();

        string svg = NormalizeMarkup(new SvgStructuralResultExporter().Export(
            model,
            new SvgExportOptions
            {
                Width = 640.0,
                Height = 480.0,
                Title = "Combination snapshot",
                IncludeLegend = false,
                IncludeNodeLabels = false,
                IncludeLoads = true,
            }));

        Assert.Contains("id=\"loads\"", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"load-arrow\" data-load-id=\"PG\" data-load-case-id=\"G\" data-load-factor=\"1.35\"", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"load-label\" data-load-id=\"PG\" data-load-case-id=\"G\" data-load-factor=\"1.35\"", svg, StringComparison.Ordinal);
        Assert.Contains("G point: -13.5 kN (1.35x)", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"load-arrow\" data-load-id=\"PQ\" data-load-case-id=\"Q\" data-load-factor=\"1.5\"", svg, StringComparison.Ordinal);
        Assert.Contains("Q point: 6 kN (1.5x)", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"distributed-load-shape\" data-load-id=\"QG\" data-load-case-id=\"G\" data-load-factor=\"1.35\"", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"distributed-load-arrow\" data-load-id=\"QG\" data-load-case-id=\"G\" data-load-factor=\"1.35\"", svg, StringComparison.Ordinal);
        Assert.Contains("class=\"load-label\" data-load-id=\"QG\" data-load-case-id=\"G\" data-load-factor=\"1.35\"", svg, StringComparison.Ordinal);
        Assert.Contains("G distributed: -2.7 kN/m (1.35x)", svg, StringComparison.Ordinal);
        Assert.Contains("Q linear: -1.5 → -4.5 kN/m (1.5x)", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("Wind point", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("data-load-case-id=\"W\"", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void InteractiveViewer_ShouldKeepStableControlSnapshotContract()
    {
        StructuralVisualizationModel model = CreateVisualizationModel();

        string html = NormalizeMarkup(new InteractiveHtmlStructuralViewerExporter().Export(
            model,
            new InteractiveViewerExportOptions
            {
                Title = "Viewer controls snapshot",
                SvgOptions = new SvgExportOptions
                {
                    Width = 640.0,
                    Height = 480.0,
                    IncludeLegend = false,
                    IncludeLoads = true,
                },
            }));

        Assert.Contains("data-scale=\"deformed\"", html, StringComparison.Ordinal);
        Assert.Contains("max=\"500\"", html, StringComparison.Ordinal);
        Assert.Contains("data-text-scale=\"labels\"", html, StringComparison.Ordinal);
        Assert.Contains("data-layer=\"#loads\" checked", html, StringComparison.Ordinal);
        Assert.Contains("data-layer=\".load-label\" checked", html, StringComparison.Ordinal);
        Assert.Contains("data-layer=\".diagram.normal-force,.diagram-fill.normal-force,.annotation-point.normal-force,.annotation-label.normal-force\"", html, StringComparison.Ordinal);
        Assert.Contains("data-layer=\".diagram.shear-force,.diagram-fill.shear-force,.annotation-point.shear-force,.annotation-label.shear-force\"", html, StringComparison.Ordinal);
        Assert.Contains("data-layer=\".diagram.bending-moment,.diagram-fill.bending-moment,.annotation-point.bending-moment,.annotation-label.bending-moment\"", html, StringComparison.Ordinal);
        Assert.Contains("applyVisualScales", html, StringComparison.Ordinal);
        Assert.Contains("applyTextScale", html, StringComparison.Ordinal);
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
                new VisualizationMember("M1", "A", "B", MemberType.Frame2D, new VisualizationPoint(0.0, 0.0), new VisualizationPoint(4.0, 0.0)),
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
                    new VisualizationPoint(2.0, 0.3),
                    new VisualizationPoint(4.0, 0.0),
                }, 12.0),
            },
            new VisualizationBounds(-0.5, -0.5, 4.5, 1.0),
            100.0,
            new[]
            {
                new VisualizationSupport("S1", "A", SupportGlyphKind.Hinge, new VisualizationPoint(0.0, 0.0), "A"),
            },
            Array.Empty<VisualizationReactionArrow>(),
            Array.Empty<VisualizationReactionMoment>(),
            Array.Empty<MemberDimensionAnnotation>(),
            new VisualizationDisplacementAnnotation("B", new VisualizationPoint(4.0, 0.0), new VisualizationPoint(4.0, -0.2), 0.002, 0.0, -0.002, 0.0),
            Array.Empty<DiagramValueAnnotation>(),
            null,
            new[]
            {
                new VisualizationNodeDisplacementLabel("B", "B", new VisualizationPoint(4.0, -0.2), 0.0, -0.002, 0.002, 0.0),
            },
            new[]
            {
                new VisualizationMemberDisplacementLabel("M1", "L/2", 0.50, 2.0, new VisualizationPoint(2.0, -0.1), 0.0, -0.001, 0.001, 0.0),
            },
            new[]
            {
                new VisualizationLoadArrow("P1", "LC1", VisualizationLoadGlyphKind.ForceArrow, new VisualizationPoint(4.0, 0.0), new VisualizationPoint(4.0, -0.5), -10.0, "kN", "Point P"),
            },
            Array.Empty<VisualizationLoadMoment>(),
            Array.Empty<VisualizationDistributedLoad>());

    private static StructuralVisualizationModel CreateFactoredCombinationVisualizationModel()
    {
        StructuralModel structuralModel = new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 4.0, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic", 210_000_000.0))
            .AddSection(new StructuralSection("SEC", "Generic", 0.01, 0.0001))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("G", "Permanent"))
            .AddLoadCase(new StructuralLoadCase("Q", "Variable"))
            .AddLoadCase(new StructuralLoadCase("W", "Wind"))
            .AddLoadCombination(new StructuralLoadCombination(
                "ULS1",
                "ULS 1",
                new[]
                {
                    new StructuralLoadCombinationTerm("G", 1.35),
                    new StructuralLoadCombinationTerm("Q", 1.50),
                }))
            .AddLoad(StructuralLoad.NodalForce("PG", "G", "B", StructuralLoadDirection.GlobalY, -10.0, "G point"))
            .AddLoad(StructuralLoad.NodalForce("PQ", "Q", "B", StructuralLoadDirection.GlobalX, 4.0, "Q point"))
            .AddLoad(StructuralLoad.NodalForce("PW", "W", "B", StructuralLoadDirection.GlobalY, 99.0, "Wind point"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("QG", "G", "M1", StructuralLoadDirection.GlobalY, -2.0, "G distributed"))
            .AddLoad(StructuralLoad.LinearDistributedLoad("QQ", "Q", "M1", StructuralLoadDirection.GlobalY, -1.0, -3.0, "Q linear"));
        var result = new StructuralAnalysisResult(
            "ULS1",
            Array.Empty<NodalDisplacementResult>(),
            Array.Empty<SupportReactionResult>(),
            Array.Empty<MemberEndForceResult>());

        return new StructuralVisualizationModelBuilder().Build(
            structuralModel,
            result,
            Array.Empty<MemberInternalForceDiagram>(),
            Array.Empty<MemberDisplacementDiagram>(),
            new VisualizationOptions
            {
                LoadForceScale = 0.01,
                DistributedLoadScale = 0.01,
                BoundsPadding = 0.0,
            });
    }

    private static string NormalizeMarkup(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Trim();
}
