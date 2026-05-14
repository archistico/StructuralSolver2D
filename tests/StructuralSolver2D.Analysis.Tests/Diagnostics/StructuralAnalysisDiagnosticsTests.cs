using StructuralSolver2D.Analysis;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Diagnostics;

/// <summary>
/// Verifies that analysis failures provide actionable diagnostics instead of generic errors.
/// </summary>
public sealed class StructuralAnalysisDiagnosticsTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;

    [Fact]
    public void Frame2DAnalyzer_WithTrussMember_ShouldReportUnsupportedMemberIdAndType()
    {
        StructuralModel model = CreateBaseModel(MemberType.Truss2D)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"));

        Frame2DAnalyzer analyzer = new();

        StructuralAnalysisException exception = Assert.Throws<StructuralAnalysisException>(() => analyzer.Analyze(model, "LC1"));

        Assert.Contains("Frame2D", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("M1", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Truss2D", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Mixed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Truss2DAnalyzer_WithFrameMember_ShouldReportUnsupportedMemberIdAndType()
    {
        StructuralModel model = CreateBaseModel(MemberType.Frame2D)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"));

        Truss2DAnalyzer analyzer = new();

        StructuralAnalysisException exception = Assert.Throws<StructuralAnalysisException>(() => analyzer.Analyze(model, "LC1"));

        Assert.Contains("Truss2D", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("M1", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Frame2D", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Mixed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Truss2DAnalyzer_WithUnsupportedLoad_ShouldReportUnsupportedLoadIdAndType()
    {
        StructuralModel model = CreateBaseModel(MemberType.Truss2D)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10.0));

        Truss2DAnalyzer analyzer = new();

        StructuralAnalysisException exception = Assert.Throws<StructuralAnalysisException>(() => analyzer.Analyze(model, "LC1"));

        Assert.Contains("Q1", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UniformDistributedLoad", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("nodal force", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Frame2DAnalyzer_WithSingularModel_ShouldReportSingularPivotAndMechanismHint()
    {
        StructuralModel model = CreateBaseModel(MemberType.Frame2D)
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalY, -10.0));

        Frame2DAnalyzer analyzer = new();

        StructuralAnalysisException exception = Assert.Throws<StructuralAnalysisException>(() => analyzer.Analyze(model, "LC1"));

        Assert.Contains("singular", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pivot", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mechanism", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static StructuralModel CreateBaseModel(MemberType memberType) =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 5.0, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", memberType))
            .AddLoadCase(new StructuralLoadCase("LC1", "Main load case"));
}
