using System.Globalization;
using System.Xml.Linq;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Reporting.Visualization;

namespace StructuralSolver2D.Reporting.Tests.Visualization;

public sealed class DeformationAnimationRegressionTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(17)]
    [InlineData(24)]
    public void UniformlyLoadedBeam_ShouldLoadAndUnloadWithoutReversingDisplacement(int frameCount)
    {
        var model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0)).AddNode(new StructuralNode("B", 5, 0))
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210000000))
            .AddSection(new StructuralSection("SEC", "Section", .003, .00002))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC"))
            .AddSupport(StructuralSupport.Hinge("SA", "A")).AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoadCase(new StructuralLoadCase("LC1", "Uniform downward load"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10));
        var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
        var scene = new StructuralVisualizationModelBuilder().Build(model, output,
            new VisualizationOptions { DeformationScale = 100, AnimationFrameCount = frameCount });
        var staticPoints = scene.DeformedShapes.Single().Points;
        Assert.True(staticPoints.Min(point => point.Y) < 0);
        foreach (var frame in scene.AnimationFrames)
        {
            Assert.InRange(frame.Factor, 0, 1);
            var points = frame.DeformedShapes.Single().Points;
            for (int i = 0; i < points.Count; i++)
            {
                Assert.InRange(points[i].Y, staticPoints[i].Y - 1e-12, 1e-12);
                Assert.Equal(staticPoints[i].Y * frame.Factor, points[i].Y, 10);
            }
            Assert.Equal(0, points[0].Y, 10);
            Assert.Equal(0, points[^1].Y, 10);
        }
        Assert.Equal(0, scene.AnimationFrames[0].Factor, 12);
        if (frameCount % 2 == 0) Assert.Equal(1, scene.AnimationFrames[frameCount / 2].Factor, 12);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(3, 4)]
    [InlineData(0, 5)]
    public void Export_ShouldPreserveNonuniformSampleOriginsAndFrameVectors(double endX, double endY)
    {
        var model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0)).AddNode(new StructuralNode("B", endX, endY))
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210000000))
            .AddSection(new StructuralSection("SEC", "Section", .003, .00002))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC"));
        double[] stations = { 0, .2, .65, 1 };
        var samples = stations.Select(t => new MemberDisplacementSample("M1", t, 5 * t, 0, 0, 0, .002 * t, -.01 * t * t)).ToArray();
        var diagram = new MemberDisplacementDiagram("M1", 5, samples);
        var result = new StructuralAnalysisResult("LC1", Array.Empty<NodalDisplacementResult>(), Array.Empty<SupportReactionResult>(), Array.Empty<MemberEndForceResult>());
        var scene = new StructuralVisualizationModelBuilder().Build(model, result, Array.Empty<MemberInternalForceDiagram>(), new[] { diagram },
            new VisualizationOptions { DeformationScale = 50, AnimationFrameCount = 4 });
        var document = XDocument.Parse(new SvgStructuralResultExporter().Export(scene));
        var axis = document.Descendants().Single(element => (string?)element.Attribute("class") == "member undeformed" && (string?)element.Attribute("data-member-id") == "M1");
        var curve = document.Descendants().Single(element => (string?)element.Attribute("data-deformed-polyline") == "true");
        var origins = Points((string)curve.Attribute("data-base-points")!);
        var vectors = Points((string)curve.Attribute("data-displacement-points")!);
        double x1 = Value(axis, "x1"), y1 = Value(axis, "y1"), dx = Value(axis, "x2") - x1, dy = Value(axis, "y2") - y1;
        double scale = Math.Sqrt(dx * dx + dy * dy) / 5;
        for (int i = 0; i < samples.Length; i++)
        {
            Assert.InRange(Math.Abs(origins[i].X - (x1 + dx * stations[i])), 0, .002);
            Assert.InRange(Math.Abs(origins[i].Y - (y1 + dy * stations[i])), 0, .002);
            Assert.InRange(Math.Abs(vectors[i].X - scale * 50 * samples[i].GlobalUx), 0, .002);
            Assert.InRange(Math.Abs(vectors[i].Y + scale * 50 * samples[i].GlobalUy), 0, .002);
            foreach (var frame in scene.AnimationFrames)
            {
                var point = frame.DeformedShapes.Single().Points[i];
                Assert.Equal(endX * stations[i] + 50 * frame.Factor * samples[i].GlobalUx, point.X, 10);
                Assert.Equal(endY * stations[i] + 50 * frame.Factor * samples[i].GlobalUy, point.Y, 10);
            }
        }
    }

    private static double Value(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
    private static (double X, double Y)[] Points(string value) => value.Split(' ').Select(pair =>
    {
        string[] xy = pair.Split(',');
        return (double.Parse(xy[0], CultureInfo.InvariantCulture), double.Parse(xy[1], CultureInfo.InvariantCulture));
    }).ToArray();
}
