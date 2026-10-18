using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Reporting.Visualization;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Analysis.Results;
using System.Text.Json;

string path = Path.GetFullPath(args.Single());
var model = new StructuralModel()
    .AddNode(new StructuralNode("A", 0, 0)).AddNode(new StructuralNode("B", 5, 0))
    .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000))
    .AddSection(new StructuralSection("SEC", "Section", .003, .00002))
    .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC"))
    .AddSupport(StructuralSupport.Fixed("SA", "A"))
    .AddLoadCase(new StructuralLoadCase("LC1", "Load"))
    .AddLoad(StructuralLoad.NodalForce("P", "LC1", "B", StructuralLoadDirection.GlobalY, -10));
var output = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
var visualization = new StructuralVisualizationModelBuilder().Build(model, output, new VisualizationOptions
{
    DeformationScale = 10,
    NormalForceDiagramScale = .001,
    ShearForceDiagramScale = .001,
    BendingMomentDiagramScale = .001
});
string tests = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "viewer-regressions.js"));
Directory.CreateDirectory(Path.GetDirectoryName(path)!);
WriteFixture(path, visualization, true, "Cantilever viewer regression", "Small cantilever model.", ExpectedSamples(model, output.DisplacementDiagrams, true));
foreach (string name in new[] { "two-storey-house-balconies", "truss-bridge-20m-deck-loads" })
{
    var file = StructuralModelJsonReader.Read(Path.Combine(AppContext.BaseDirectory, "Fixtures", name + ".json"));
    var service = new StructuralSolver2DService();
    var result = file.DefaultTargetIsCombination ? service.AnalyzeLoadCombination(file.Model, file.LoadCaseId) : service.AnalyzeLoadCase(file.Model, file.LoadCaseId);
    var scene = new StructuralVisualizationModelBuilder().Build(file.Model, result, new VisualizationOptions { DeformationScale = 100, NormalForceDiagramScale = .02, ShearForceDiagramScale = .02, BendingMomentDiagramScale = .02 });
    WriteFixture(Path.Combine(Path.GetDirectoryName(path)!, name + ".html"), scene, false, file.Title, file.Description, ExpectedSamples(file.Model, result.DisplacementDiagrams, false));
}

// Sparse stations exercise the public reporting API, which also accepts nonuniform diagrams.
model.Nodes[0] = model.Nodes[0] with { X = 1, Y = 2 };
model.Nodes[1] = model.Nodes[1] with { X = 4, Y = 6 };
var inclined = new StructuralSolver2DService().AnalyzeLoadCase(model, "LC1");
var sparse = inclined.DisplacementDiagrams.Select(diagram => new MemberDisplacementDiagram(diagram.MemberId, diagram.Length,
    diagram.Samples.Where((sample, index) => index is 0 or 1 or 4 || index == diagram.Samples.Count/2 || index == diagram.Samples.Count-1).ToArray())).ToArray();
var inclinedScene = new StructuralVisualizationModelBuilder().Build(model, inclined.Result, inclined.InternalForceDiagrams, sparse, new VisualizationOptions { DeformationScale = 100 });
WriteFixture(Path.Combine(Path.GetDirectoryName(path)!, "inclined-nonuniform.html"), inclinedScene, false, "Inclined cantilever, nonuniform stations", "Combined axial and bending displacement.", ExpectedSamples(model, sparse, true));

object ExpectedSamples(StructuralModel source, IReadOnlyList<MemberDisplacementDiagram> diagrams, bool analyticalCantilever) => diagrams.Select(diagram =>
{
    var member = source.Members.Single(item => item.Id == diagram.MemberId);
    var start = source.Nodes.Single(node => node.Id == member.StartNodeId);
    var end = source.Nodes.Single(node => node.Id == member.EndNodeId);
    double length = StructuralMember.GetLength(start, end), c = (end.X-start.X)/length, s = (end.Y-start.Y)/length;
    return new { id = member.Id, start = new { x = start.X, y = start.Y }, length, samples = diagram.Samples.Select(sample =>
    {
        double ux = sample.GlobalUx, uy = sample.GlobalUy;
        if (analyticalCantilever)
        {
            // Closed-form cantilever under the fixture's -10 kN global-Y end force.
            double x = sample.NormalizedPosition*length;
            double axial = -10*s*x/(210000000*.003);
            double bending = -10*c*x*x*(3*length-x)/(6*210000000*.00002);
            ux = c*axial-s*bending; uy = s*axial+c*bending;
        }
        return new { x = start.X+(end.X-start.X)*sample.NormalizedPosition, y = start.Y+(end.Y-start.Y)*sample.NormalizedPosition, ux, uy };
    }) };
}).ToArray();

void WriteFixture(string filePath, StructuralVisualizationModel scene, bool simple, string title, string description, object expected)
{
    string html = new InteractiveHtmlStructuralViewerExporter().Export(scene, new InteractiveViewerExportOptions
    {
        Title = title, Description = description,
        InitialDeformationVisualScalePercent = 102, InitialTextScalePercent = 117, InitialAnimationSpeed = 1.1
    });
    File.WriteAllText(filePath, html.Replace("</body>", $"<script type=\"application/json\" id=\"deformation-oracle\">{JsonSerializer.Serialize(expected)}</script><script>window.simpleTestFixture = {simple.ToString().ToLowerInvariant()};</script><script>{tests}</script></body>", StringComparison.Ordinal));
}
