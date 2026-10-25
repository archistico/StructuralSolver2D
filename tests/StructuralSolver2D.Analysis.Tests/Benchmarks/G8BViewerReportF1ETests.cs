using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Reporting.Markdown;
using StructuralSolver2D.Reporting.Visualization;

namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// F1.1-E: independent G8B reference -> public analysis -> SVG/Markdown/HTML.
/// This checks the actual exported numbers and graphic coordinates, rather than
/// only comparing a viewer model to the same production data it was built from.
/// The immutable static F1D JSON is a SEPARATE, still-open gate.
/// </summary>
public sealed class G8BViewerReportF1ETests
{
    private const double ForceTolerance = 0.005;
    private const double RelativeTolerance = 0.0001;
    private const double SvgPixelTolerance = 0.025;

    private static readonly double[] Ratios = { 0, 0.25, 0.5, 0.75, 1 };

    private static VisualizationOptions DiagramOptions() => new()
    {
        DeformationScale = 1,
        NormalForceDiagramScale = 0.002,
        ShearForceDiagramScale = 0.004,
        BendingMomentDiagramScale = 0.003,
        BoundsPadding = 0.5
    };

    private static SvgExportOptions ExportOptions() => new()
    {
        Width = 1400,
        Height = 1000,
        Padding = 32,
        IncludeLegend = false
    };

    private static StructuralAnalysisOutput Analyze(StructuralModel model, string combination) =>
        new StructuralSolver2DService().AnalyzeLoadCombination(model, combination,
            new StructuralAnalysisOptions { InternalForceSampleCount = 5, DisplacementSampleCount = 5 });

    [Theory]
    [MemberData(nameof(G8BIndependentMemberF1CTests.G8BCases),
        MemberType = typeof(G8BIndependentMemberF1CTests))]
    public void AllSixteenScenarios_SvgPolylinePositionsMatchIndependentSignedNvm(
        string filename, string combination)
    {
        StructuralModel model = G8BIndependentMemberF1CTests.LoadHashLockedModel(filename);
        var reference = G8BVariationalReferenceF1C.Analyze(model, combination);
        var options = DiagramOptions();
        var scene = new StructuralVisualizationModelBuilder().Build(model, Analyze(model, combination), options);
        XDocument svg = XDocument.Parse(new SvgStructuralResultExporter().Export(scene, ExportOptions()));

        var memberAxes = svg.Descendants().Where(element =>
                element.Name.LocalName == "line" && (string?)element.Attribute("class") == "member undeformed" &&
                element.Attribute("data-member-id") is not null)
            .ToDictionary(element => (string)element.Attribute("data-member-id")!, StringComparer.Ordinal);
        var polylines = svg.Descendants().Where(element =>
                element.Name.LocalName == "polyline" &&
                ((string?)element.Attribute("class"))?.StartsWith("diagram ", StringComparison.Ordinal) == true)
            .ToDictionary(element => (
                Id: (string)element.Attribute("data-member-id")!,
                Kind: (string)element.Attribute("class")!), element => element);

        Assert.Equal(model.Members.Count, memberAxes.Count);
        Assert.Equal(model.Members.Count * 3, polylines.Count);

        foreach (StructuralMember member in model.Members)
        {
            var force = reference.Members[member.Id];
            StructuralNode a = model.Nodes.Single(node => node.Id == member.StartNodeId);
            StructuralNode b = model.Nodes.Single(node => node.Id == member.EndNodeId);
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double length = force.Length;
            double nx = -dy / length;
            double ny = dx / length;
            XElement axis = memberAxes[member.Id];
            double ax1 = AttributeNumber(axis, "x1");
            double ay1 = AttributeNumber(axis, "y1");
            double ax2 = AttributeNumber(axis, "x2");
            double ay2 = AttributeNumber(axis, "y2");
            double pixelsPerMetre = Math.Sqrt(Math.Pow(ax2 - ax1, 2) + Math.Pow(ay2 - ay1, 2)) / length;

            foreach ((string css, double diagramScale, Func<(double N, double V, double M), double> read) in
                DiagramKinds(options))
            {
                var points = ParsePoints((string)polylines[(member.Id, css)].Attribute("points")!);
                Assert.Equal(Ratios.Length, points.Count);

                for (int station = 0; station < Ratios.Length; station++)
                {
                    double t = Ratios[station];
                    double independentValue = read(force.At(t * length));
                    // Y is inverted only by the SVG coordinate mapper, not by the FEM.
                    double expectedX = ax1 + (ax2 - ax1) * t + nx * independentValue * diagramScale * pixelsPerMetre;
                    double expectedY = ay1 + (ay2 - ay1) * t - ny * independentValue * diagramScale * pixelsPerMetre;
                    double pixelTolerance = SvgPixelTolerance +
                        (ForceTolerance + RelativeTolerance * Math.Abs(independentValue)) * diagramScale * pixelsPerMetre;
                    Close(expectedX, points[station].X, pixelTolerance,
                        $"{filename}/{combination}/{member.Id}/{css}/{t:R} SVG x");
                    Close(expectedY, points[station].Y, pixelTolerance,
                        $"{filename}/{combination}/{member.Id}/{css}/{t:R} SVG y");
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(G8BIndependentMemberF1CTests.G8BCases),
        MemberType = typeof(G8BIndependentMemberF1CTests))]
    public void AllSixteenScenarios_MarkdownEndForcesAndFiveStationTablesMatchIndependentReference(
        string filename, string combination)
    {
        StructuralModel model = G8BIndependentMemberF1CTests.LoadHashLockedModel(filename);
        var reference = G8BVariationalReferenceF1C.Analyze(model, combination);
        var output = Analyze(model, combination);
        string report = new MarkdownStructuralReportGenerator().Generate(model, output.Result,
            output.InternalForceDiagrams, output.Summary, new MarkdownReportOptions
            {
                IncludeEducationalExplanations = false,
                IncludeCharacteristicPoints = false,
                MaxSamplesPerMember = 5,
                GeneratedAtUtc = new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc)
            });
        string[] lines = report.Split('\n').Select(line => line.Trim()).ToArray();
        int endForceHeading = Array.IndexOf(lines, "### Local member end forces");
        int diagramHeading = Array.IndexOf(lines, "## Internal force diagrams");
        Assert.True(endForceHeading >= 0 && diagramHeading > endForceHeading);

        var endRows = lines.Skip(endForceHeading + 1).Take(diagramHeading - endForceHeading - 1)
            .Where(line => line.StartsWith("| `", StringComparison.Ordinal))
            .Select(ParseTableRow)
            .ToDictionary(row => row[0].Trim('`'), StringComparer.Ordinal);
        Assert.Equal(model.Members.Count, endRows.Count);

        foreach (StructuralMember member in model.Members)
        {
            var expected = reference.Members[member.Id];
            string[] end = endRows[member.Id];
            Assert.Equal(7, end.Length);
            double[] endReference =
            {
                expected.StartAxial, expected.StartShear, expected.StartMoment,
                expected.EndAxial, expected.EndShear, expected.EndMoment
            };
            for (int i = 0; i < endReference.Length; i++)
                Close(endReference[i], Number(end[i + 1]), ForceTolerance,
                    $"{filename}/{combination}/{member.Id} report end force {i}");

            int memberHeading = Array.IndexOf(lines, $"### Member `{member.Id}`", diagramHeading + 1);
            Assert.True(memberHeading > diagramHeading, $"Missing report station table: {member.Id}");
            int tableHeader = Array.FindIndex(lines, memberHeading + 1,
                line => line == "| Position | x [m] | N [kN] | V [kN] | M [kNm] |");
            Assert.True(tableHeader > memberHeading, $"Missing N/V/M heading: {member.Id}");
            string[][] rows = lines.Skip(tableHeader + 2).Take(Ratios.Length)
                .Select(ParseTableRow).ToArray();
            Assert.Equal(Ratios.Length, rows.Length);

            for (int station = 0; station < Ratios.Length; station++)
            {
                double t = Ratios[station];
                string[] row = rows[station];
                Assert.Equal(5, row.Length);
                (double n, double v, double m) = expected.At(t * expected.Length);
                Close(t, Number(row[0]), 0.000001, $"{member.Id} report ratio");
                Close(t * expected.Length, Number(row[1]), 0.000001, $"{member.Id} report distance [m]");
                Close(n, Number(row[2]), ForceTolerance, $"{member.Id} N({t:R}) [kN]");
                Close(v, Number(row[3]), ForceTolerance, $"{member.Id} V({t:R}) [kN]");
                Close(m, Number(row[4]), ForceTolerance, $"{member.Id} M({t:R}) [kNm]");
            }
        }
    }

    [Theory]
    [InlineData("ULS_WIND_POS", "W3_RIDGE", 1)]
    [InlineData("ULS_WIND_NEG", "WN3_RIDGE", -1)]
    public void SvgWindArrow_RetainsTheCorrectGlobalXDirection(
        string combination, string loadId, int sign)
    {
        StructuralModel model = G8BIndependentMemberF1CTests.LoadHashLockedModel(
            "g8b-b_riferimento-controventato.json");
        var scene = new StructuralVisualizationModelBuilder().Build(model, Analyze(model, combination), DiagramOptions());
        XDocument svg = XDocument.Parse(new SvgStructuralResultExporter().Export(scene, ExportOptions()));
        XElement arrow = Assert.Single(svg.Descendants(), element =>
            element.Name.LocalName == "line" && (string?)element.Attribute("class") == "load-arrow" &&
            (string?)element.Attribute("data-load-id") == loadId);
        Assert.True(sign * (AttributeNumber(arrow, "x2") - AttributeNumber(arrow, "x1")) > 0,
            $"{loadId}: the arrow must follow the signed global-X wind force.");
    }

    [Fact]
    public void SvgGravityDistributedArrows_PointDownwardForNegativeGlobalY()
    {
        StructuralModel model = G8BIndependentMemberF1CTests.LoadHashLockedModel(
            "g8b-b_riferimento-controventato.json");
        var scene = new StructuralVisualizationModelBuilder().Build(model, Analyze(model, "ULS_GRAVITY"), DiagramOptions());
        XDocument svg = XDocument.Parse(new SvgStructuralResultExporter().Export(scene, ExportOptions()));
        var arrows = svg.Descendants().Where(element =>
            element.Name.LocalName == "line" &&
            (string?)element.Attribute("class") == "distributed-load-arrow").ToArray();
        Assert.NotEmpty(arrows);
        Assert.All(arrows, arrow => Assert.True(
            AttributeNumber(arrow, "y2") > AttributeNumber(arrow, "y1"),
            "GlobalY-negative gravity must point down in the screen's SVG coordinate system."));
    }

    [Fact]
    public void HtmlViewer_EmbedsIndependentNvmMaximaAndLoadVisibilitySwitch()
    {
        StructuralModel model = G8BIndependentMemberF1CTests.LoadHashLockedModel(
            "g8b-b_riferimento-controventato.json");
        const string combination = "ULS_GRAVITY";
        var reference = G8BVariationalReferenceF1C.Analyze(model, combination);
        var scene = new StructuralVisualizationModelBuilder().Build(model, Analyze(model, combination), DiagramOptions());
        string html = new InteractiveHtmlStructuralViewerExporter().Export(scene,
            new InteractiveViewerExportOptions { ShowLoadsByDefault = true });

        const string beginTag = "<script type=\"application/json\" id=\"viewer-data\">";
        int start = html.IndexOf(beginTag, StringComparison.Ordinal);
        Assert.True(start >= 0, "Viewer must embed inspectable data.");
        start += beginTag.Length;
        int finish = html.IndexOf("</script>", start, StringComparison.Ordinal);
        Assert.True(finish > start);
        using JsonDocument data = JsonDocument.Parse(html[start..finish]);
        JsonElement members = data.RootElement.GetProperty("members");
        Assert.Equal(model.Members.Count, members.EnumerateObject().Count());

        foreach (StructuralMember member in model.Members)
        {
            var force = reference.Members[member.Id];
            JsonElement values = members.GetProperty(member.Id).GetProperty("forces");
            Close(Ratios.Max(t => Math.Abs(force.At(t * force.Length).N)),
                values.GetProperty("NormalForce").GetDouble(), ForceTolerance, $"{member.Id} HTML Nmax");
            Close(Ratios.Max(t => Math.Abs(force.At(t * force.Length).V)),
                values.GetProperty("ShearForce").GetDouble(), ForceTolerance, $"{member.Id} HTML Vmax");
            Close(Ratios.Max(t => Math.Abs(force.At(t * force.Length).M)),
                values.GetProperty("BendingMoment").GetDouble(), ForceTolerance, $"{member.Id} HTML Mmax");
        }

        Assert.Contains("data-layer=\"#loads\" checked", html, StringComparison.Ordinal);
        Assert.Contains("data-view", html, StringComparison.Ordinal);
        Assert.Contains("class=\"diagram bending-moment\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void TamperedMomentSign_IsDetectedAgainstIndependentReference()
    {
        StructuralModel model = G8BIndependentMemberF1CTests.LoadHashLockedModel(
            "g8b-b_riferimento-controventato.json");
        const string combination = "ULS_GRAVITY";
        var reference = G8BVariationalReferenceF1C.Analyze(model, combination);
        var output = Analyze(model, combination);
        var strongest = output.InternalForceDiagrams.MaxBy(diagram => diagram.MaxAbsBendingMoment)!;
        int index = Array.IndexOf(strongest.Samples.ToArray(),
            strongest.Samples.MaxBy(sample => Math.Abs(sample.BendingMoment))!);
        Assert.True(Math.Abs(reference.Members[strongest.MemberId].At(
            Ratios[index] * strongest.Length).M) > 1, "The counterproof needs a nonzero moment.");

        var altered = output.InternalForceDiagrams.Select(diagram =>
            diagram.MemberId != strongest.MemberId ? diagram :
            new MemberInternalForceDiagram(diagram.MemberId, diagram.Length,
                diagram.Samples.Select(sample => sample with
                {
                    BendingMoment = -sample.BendingMoment
                }).ToArray())).ToArray();
        var options = DiagramOptions();
        var scene = new StructuralVisualizationModelBuilder().Build(model, output.Result,
            altered, output.DisplacementDiagrams, options);
        MemberDiagramPolyline moment = scene.Diagrams.Single(diagram =>
            diagram.MemberId == strongest.MemberId &&
            diagram.Kind == VisualizationDiagramKind.BendingMoment);
        StructuralMember member = model.Members.Single(m => m.Id == strongest.MemberId);
        StructuralNode a = model.Nodes.Single(n => n.Id == member.StartNodeId);
        StructuralNode b = model.Nodes.Single(n => n.Id == member.EndNodeId);
        double length = reference.Members[strongest.MemberId].Length;
        double normalX = -(b.Y - a.Y) / length;
        double normalY = (b.X - a.X) / length;
        double t = Ratios[index];
        double baseX = a.X + (b.X - a.X) * t;
        double baseY = a.Y + (b.Y - a.Y) * t;
        double decodedMoment = ((moment.Points[index].X - baseX) * normalX +
                                (moment.Points[index].Y - baseY) * normalY) /
                               options.BendingMomentDiagramScale;
        double expectedMoment = reference.Members[strongest.MemberId].At(t * length).M;

        Assert.True(Math.Abs(decodedMoment - expectedMoment) > 2,
            "An inverted bending-moment diagram must disagree with the independent signed oracle.");
    }

    private static IEnumerable<(string Css, double Scale,
        Func<(double N, double V, double M), double> Read)> DiagramKinds(VisualizationOptions options)
    {
        yield return ("diagram normal-force", options.NormalForceDiagramScale, value => value.N);
        yield return ("diagram shear-force", options.ShearForceDiagramScale, value => value.V);
        yield return ("diagram bending-moment", options.BendingMomentDiagramScale, value => value.M);
    }

    private static string[] ParseTableRow(string line) =>
        line.Trim().Trim('|').Split('|', StringSplitOptions.TrimEntries);

    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);

    private static double AttributeNumber(XElement element, string name) =>
        Number((string)element.Attribute(name)!);

    private static List<(double X, double Y)> ParsePoints(string points) =>
        points.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Split(','))
            .Select(parts => (X: Number(parts[0]), Y: Number(parts[1]))).ToList();

    private static void Close(double expected, double actual, double absoluteTolerance, string context)
    {
        double allowed = absoluteTolerance + RelativeTolerance * Math.Abs(expected);
        Assert.True(double.IsFinite(actual) && Math.Abs(expected - actual) <= allowed,
            string.Format(CultureInfo.InvariantCulture,
                "{0}: expected {1:R}; exported {2:R}; tolerance {3:R}",
                context, expected, actual, allowed));
    }
}
