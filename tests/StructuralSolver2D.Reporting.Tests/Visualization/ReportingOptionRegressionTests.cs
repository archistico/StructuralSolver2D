using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Reporting.Visualization;

namespace StructuralSolver2D.Reporting.Tests.Visualization;

public sealed class ReportingOptionRegressionTests
{
    public static IEnumerable<object[]> InvalidVisualizationNumbers() => typeof(VisualizationOptions).GetProperties()
        .Where(property => property.PropertyType == typeof(double))
        .SelectMany(property => new[] { -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity }
            .Select(value => new object[] { property.Name, value }));

    [Theory]
    [MemberData(nameof(InvalidVisualizationNumbers))]
    public void Builder_ShouldRejectInvalidNumericOption(string name, double value)
    {
        var options = new VisualizationOptions();
        typeof(VisualizationOptions).GetProperty(name)!.SetValue(options, value);
        var result = new StructuralAnalysisResult("LC1", Array.Empty<NodalDisplacementResult>(),
            Array.Empty<SupportReactionResult>(), Array.Empty<MemberEndForceResult>());
        Assert.Throws<ArgumentOutOfRangeException>(() => new StructuralVisualizationModelBuilder().Build(
            new StructuralModel(), result, Array.Empty<MemberInternalForceDiagram>(), Array.Empty<MemberDisplacementDiagram>(), options));
    }

    public static IEnumerable<object[]> NonFiniteSvgNumbers() => new[] { "Width", "Height", "Padding" }
        .SelectMany(name => new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity }
            .Select(value => new object[] { name, value }));

    [Theory]
    [MemberData(nameof(NonFiniteSvgNumbers))]
    public void Svg_ShouldRejectNonFiniteOption(string name, double value)
    {
        var options = new SvgExportOptions();
        typeof(SvgExportOptions).GetProperty(name)!.SetValue(options, value);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    public static IEnumerable<object[]> NonFiniteViewerNumbers() => new[]
        { "InitialDeformationVisualScalePercent", "InitialTextScalePercent", "InitialAnimationSpeed" }
        .SelectMany(name => new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity }
            .Select(value => new object[] { name, value }));

    [Theory]
    [MemberData(nameof(NonFiniteViewerNumbers))]
    public void Viewer_ShouldRejectNonFiniteInitialControlValue(string name, double value)
    {
        var options = new InteractiveViewerExportOptions();
        typeof(InteractiveViewerExportOptions).GetProperty(name)!.SetValue(options, value);
        var model = new StructuralVisualizationModel(Array.Empty<VisualizationNode>(), Array.Empty<VisualizationMember>(),
            Array.Empty<DeformedMemberShape>(), Array.Empty<MemberDiagramPolyline>(), VisualizationBounds.Empty, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new InteractiveHtmlStructuralViewerExporter().Export(model, options));
    }

    [Fact]
    public void Builder_ShouldAllowZeroScalesAndProduceFiniteGeometry()
    {
        var options = new VisualizationOptions();
        foreach (var property in typeof(VisualizationOptions).GetProperties().Where(property => property.PropertyType == typeof(double)))
            property.SetValue(options, 0.0);
        var result = new StructuralAnalysisResult("LC1", Array.Empty<NodalDisplacementResult>(),
            Array.Empty<SupportReactionResult>(), Array.Empty<MemberEndForceResult>());
        var model = new StructuralVisualizationModelBuilder().Build(new StructuralModel(), result,
            Array.Empty<MemberInternalForceDiagram>(), Array.Empty<MemberDisplacementDiagram>(), options);
        Assert.Equal(0, model.DeformationScale);
        string svg = new SvgStructuralResultExporter().Export(model);
        Assert.DoesNotContain("NaN", svg);
        Assert.DoesNotContain("Infinity", svg);
    }
}
