using System.Text.Json;
using System.Text.RegularExpressions;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Reporting.Nonlinear;

namespace StructuralSolver2D.Reporting.Tests.Nonlinear;

public sealed class InteractiveNonlinearViewerM69Tests
{
    private static NonlinearTruss2DModel Model(string? nodeId = null) => new(
        new[] { new NonlinearTruss2DNode("A", 0, 0), new NonlinearTruss2DNode(nodeId ?? "B", 1, 0) },
        new[] { new NonlinearTruss2DMember("AB", "A", nodeId ?? "B", 1, 1000, 5, 100, 1) },
        new[] { new NonlinearTruss2DSupport("A", 1, 0), new NonlinearTruss2DSupport("A", 0, 1),
            new NonlinearTruss2DSupport(nodeId ?? "B", 0, 1) },
        new[] { new NonlinearTruss2DLoad(nodeId ?? "B", 2, 0) });

    [Fact]
    public void StaticViewer_ContainsPhysicalHistoriesAndControls()
    {
        var model = Model();
        var result = new StructuralNonlinearTruss2DService().AnalyzeStatic(
            new StructuralNonlinearStaticRequest(model, new[] { 1.0, 4.0 }));
        Assert.True(result.Completed, result.Failure);
        string html = new InteractiveHtmlNonlinearTruss2DViewerExporter().ExportStatic(model, result, "Static test");
        Assert.Contains("id=\"dataset\"", html);
        Assert.Contains("id=\"structure\"", html);
        Assert.Contains("id=\"load-chart\"", html);
        Assert.Contains("id=\"force-chart\"", html);
        Assert.Contains("id=\"plastic-chart\"", html);
        Assert.Contains("id=\"energy-chart\"", html);
        Assert.Contains("\"mode\":\"static\"", html);
        Assert.Contains("\"completed\":true", html);
        Assert.Contains("\"accumulatedPlasticStrain\"", html);
        Assert.Contains("\"plasticDissipation\"", html);
        Assert.Contains("\"axialForces\"", html);
        Assert.Contains("\"loadFactor\":4", html);
        Assert.DoesNotContain("src=\"http", html);
        Assert.DoesNotContain("<script src=", html);
    }

    [Fact]
    public void TimeViewer_UsesPhysicalTimeAndAcceptedEnergies()
    {
        var model = Model();
        var result = new StructuralNonlinearTruss2DService().AnalyzeTime(
            new StructuralNonlinearTimeRequest(model, new[] { 0d, 1d, 0d, 0d }, 0.01));
        Assert.True(result.Completed, result.Failure);
        string html = new InteractiveHtmlNonlinearTruss2DViewerExporter().ExportTime(model, result);
        Assert.Contains("\"mode\":\"time\"", html);
        Assert.Contains("\"kinetic\"", html);
        Assert.Contains("\"physicalEnergyResidual\"", html);
        Assert.Contains("\"position\":", html);
        Assert.Contains("tempo fisico", html);
    }

    [Fact]
    public void Failure_ExportsOnlyAcceptedHistoryAndShowsVisibleWarning()
    {
        var model = Model();
        var output = new StructuralNonlinearTruss2DService().AnalyzeStatic(
            new StructuralNonlinearStaticRequest(model, new[] { 1d, 4d }, MaxIterations: 1));
        Assert.False(output.Completed);
        Assert.Single(output.Steps);
        string html = new InteractiveHtmlNonlinearTruss2DViewerExporter().ExportStatic(model, output);
        Assert.Contains("RISULTATO PARZIALE", html);
        Assert.Contains("\"completed\":false", html);
        Assert.Contains("M64_NONCONVERGENCE", html);
        Assert.DoesNotContain("\"loadFactor\":4", html);
        Assert.Contains("Solo passi accettati", html);
    }

    [Fact]
    public void MaliciousLabels_AreEscapedInsideStandaloneHtml()
    {
        const string untrusted = "</script><script>alert(123)</script>";
        var model = Model(untrusted);
        var result = new StructuralNonlinearTruss2DService().AnalyzeStatic(
            new StructuralNonlinearStaticRequest(model, new[] { 1d }));
        string html = new InteractiveHtmlNonlinearTruss2DViewerExporter().ExportStatic(model, result, untrusted);
        // The title and node id are JSON data inside a <script type="application/json"> block,
        // NOT HTML text. System.Text.Json must encode HTML-significant characters as \uXXXX.
        // Requiring an HTML entity such as &lt;/script&gt; here would be incorrect.
        Assert.DoesNotContain(untrusted, html);
        Assert.Contains("\\u003C/script\\u003E", html);
        Assert.Contains("\\u003Cscript\\u003E", html);

        const string datasetOpen = "<script type=\"application/json\" id=\"dataset\">";
        int start = html.IndexOf(datasetOpen, StringComparison.Ordinal);
        Assert.True(start >= 0, "The standalone HTML must contain its JSON data block.");
        start += datasetOpen.Length;
        int end = html.IndexOf("</script>", start, StringComparison.Ordinal);
        Assert.True(end > start, "The JSON data block must terminate normally.");
        string json = html.Substring(start, end - start);
        Assert.DoesNotContain("<", json); // No raw closing tag can escape the JSON script element.

        using var document = JsonDocument.Parse(json);
        Assert.Equal(untrusted, document.RootElement.GetProperty("title").GetString());
        Assert.Equal(untrusted, document.RootElement.GetProperty("nodes")[1].GetProperty("id").GetString());
        Assert.Equal(2, Regex.Matches(html, @"<script(?:\s|>)", RegexOptions.IgnoreCase).Count);
        Assert.Equal(2, Regex.Matches(html, @"</script\s*>", RegexOptions.IgnoreCase).Count);
        Assert.Contains("$('title').textContent=data.title", html); // Rendered as text, never as markup.
    }

    [Fact]
    public void MismatchedModelOrderOrBrokenStepShape_IsRejected()
    {
        var model = Model();
        var result = new StructuralNonlinearTruss2DService().AnalyzeStatic(
            new StructuralNonlinearStaticRequest(model, new[] { 1d }));
        var exporter = new InteractiveHtmlNonlinearTruss2DViewerExporter();
        var different = model with { Nodes = model.Nodes.Reverse().ToArray() };
        Assert.Contains("M69_MODEL_ORDER", Assert.Throws<ArgumentException>(() =>
            exporter.ExportStatic(different, result)).Message);
        var broken = result with { Steps = new[] { result.Steps[0] with { AxialForces = Array.Empty<double>() } } };
        Assert.Contains("M69_HISTORY_SHAPE", Assert.Throws<ArgumentException>(() =>
            exporter.ExportStatic(model, broken)).Message);
    }

    [Fact]
    public void EmptyFailedHistory_StillRendersAnExplicitNoDataState()
    {
        var model = Model();
        var result = new StructuralNonlinearStaticOutput(false, "UNSTABLE", new[] { "A", "B" },
            new[] { "AB" }, Array.Empty<StructuralNonlinearStaticStep>(),
            new[] { new StructuralNonlinearMaterialState(0, 0) });
        string html = new InteractiveHtmlNonlinearTruss2DViewerExporter().ExportStatic(model, result);
        Assert.Contains("\"steps\":[]", html);
        Assert.Contains("UNSTABLE", html);
        Assert.Contains("Nessun passo accettato", html);
    }
}
