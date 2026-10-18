using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Reporting.Visualization;
using StructuralSolver2D.Cli.Input;

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
WriteFixture(path, visualization, true, "Cantilever viewer regression", "Small cantilever model.");
foreach (string name in new[] { "two-storey-house-balconies", "truss-bridge-20m-deck-loads" })
{
    var file = StructuralModelJsonReader.Read(Path.Combine(AppContext.BaseDirectory, "Fixtures", name + ".json"));
    var service = new StructuralSolver2DService();
    var result = file.DefaultTargetIsCombination ? service.AnalyzeLoadCombination(file.Model, file.LoadCaseId) : service.AnalyzeLoadCase(file.Model, file.LoadCaseId);
    var scene = new StructuralVisualizationModelBuilder().Build(file.Model, result, new VisualizationOptions { DeformationScale = 100, NormalForceDiagramScale = .02, ShearForceDiagramScale = .02, BendingMomentDiagramScale = .02 });
    WriteFixture(Path.Combine(Path.GetDirectoryName(path)!, name + ".html"), scene, false, file.Title, file.Description);
}

void WriteFixture(string filePath, StructuralVisualizationModel scene, bool simple, string title, string description)
{
    string html = new InteractiveHtmlStructuralViewerExporter().Export(scene, new InteractiveViewerExportOptions
    {
        Title = title, Description = description,
        InitialDeformationVisualScalePercent = 102, InitialTextScalePercent = 117, InitialAnimationSpeed = 1.1
    });
    File.WriteAllText(filePath, html.Replace("</body>", $"<script>window.simpleTestFixture = {simple.ToString().ToLowerInvariant()};</script><script>{tests}</script></body>", StringComparison.Ordinal));
}
