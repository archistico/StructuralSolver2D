using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Reporting.Visualization;

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
string html = new InteractiveHtmlStructuralViewerExporter().Export(visualization);
string tests = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "viewer-regressions.js"));
Directory.CreateDirectory(Path.GetDirectoryName(path)!);
File.WriteAllText(path, html.Replace("</body>", $"<script>{tests}</script></body>", StringComparison.Ordinal));
