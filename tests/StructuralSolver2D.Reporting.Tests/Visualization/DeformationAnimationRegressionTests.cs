using System.Globalization;
using System.Xml.Linq;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Reporting.Visualization;

namespace StructuralSolver2D.Reporting.Tests.Visualization;

public sealed class DeformationAnimationRegressionTests
{
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
