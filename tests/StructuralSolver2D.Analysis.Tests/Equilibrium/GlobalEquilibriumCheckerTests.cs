using StructuralSolver2D.Analysis;
using StructuralSolver2D.Analysis.Equilibrium;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Equilibrium;

/// <summary>
/// Validates global static equilibrium checks for load cases, combinations and different analyzers.
/// </summary>
public sealed class GlobalEquilibriumCheckerTests
{
    [Fact]
    public void Check_SimpleSupportedBeamWithUniformLoad_ShouldBeInGlobalEquilibrium()
    {
        StructuralModel model = CreateSimpleSupportedBeamWithUniformLoad();
        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");

        GlobalEquilibriumResult equilibrium = new GlobalEquilibriumChecker().Check(model, result);

        Assert.Equal(-50.0, equilibrium.AppliedFy, 9);
        Assert.Equal(50.0, equilibrium.ReactionFy, 9);
        Assert.True(equilibrium.IsInEquilibrium(1e-8), FormatResidual(equilibrium));
    }

    [Fact]
    public void Check_CantileverTipPointLoad_ShouldIncludeMomentEquilibrium()
    {
        StructuralModel model = CreateCantileverWithTipPointLoad();
        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");

        GlobalEquilibriumResult equilibrium = new GlobalEquilibriumChecker().Check(model, result);

        Assert.Equal(-12.0, equilibrium.AppliedFy, 9);
        Assert.Equal(-48.0, equilibrium.AppliedMz, 9);
        Assert.Equal(12.0, equilibrium.ReactionFy, 9);
        Assert.Equal(48.0, equilibrium.ReactionMz, 9);
        Assert.True(equilibrium.IsInEquilibrium(1e-8), FormatResidual(equilibrium));
    }

    [Fact]
    public void Check_ManualLoadCombination_ShouldApplyLoadCaseFactors()
    {
        StructuralModel model = CreateSimpleSupportedBeamWithLoadCombination();
        StructuralAnalysisResult result = new Frame2DAnalyzer().AnalyzeCombination(model, "ULS1");

        GlobalEquilibriumResult equilibrium = new GlobalEquilibriumChecker().Check(model, result);

        Assert.Equal(-80.0, equilibrium.AppliedFy, 9);
        Assert.Equal(80.0, equilibrium.ReactionFy, 9);
        Assert.True(equilibrium.IsInEquilibrium(1e-8), FormatResidual(equilibrium));
    }

    [Fact]
    public void Check_InclinedMemberWithLocalYLoad_ShouldResolveGlobalResultant()
    {
        StructuralModel model = CreateInclinedCantileverWithLocalYLoad();
        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC1");

        GlobalEquilibriumResult equilibrium = new GlobalEquilibriumChecker().Check(model, result);

        Assert.Equal(40.0, equilibrium.AppliedFx, 9);
        Assert.Equal(-30.0, equilibrium.AppliedFy, 9);
        Assert.Equal(-125.0, equilibrium.AppliedMz, 9);
        Assert.True(equilibrium.IsInEquilibrium(1e-8), FormatResidual(equilibrium));
    }

    [Fact]
    public void Check_TrussModel_ShouldBeInGlobalEquilibrium()
    {
        StructuralModel model = CreateSymmetricTriangularTruss();
        StructuralAnalysisResult result = new Truss2DAnalyzer().Analyze(model, "LC1");

        GlobalEquilibriumResult equilibrium = new GlobalEquilibriumChecker().Check(model, result);

        Assert.Equal(-30.0, equilibrium.AppliedFy, 9);
        Assert.Equal(30.0, equilibrium.ReactionFy, 9);
        Assert.True(equilibrium.IsInEquilibrium(1e-8), FormatResidual(equilibrium));
    }

    [Fact]
    public void Check_MixedFrameAndTrussModel_ShouldBeInGlobalEquilibrium()
    {
        StructuralModel model = CreateMixedFrameTrussModel();
        StructuralAnalysisResult result = new PlaneStructureAnalyzer().Analyze(model, "LC1");

        GlobalEquilibriumResult equilibrium = new GlobalEquilibriumChecker().Check(model, result);

        Assert.True(equilibrium.IsInEquilibrium(1e-8), FormatResidual(equilibrium));
    }

    [Fact]
    public void Check_UnknownResultId_ShouldThrowClearException()
    {
        StructuralModel model = CreateSimpleSupportedBeamWithUniformLoad();
        StructuralAnalysisResult result = new(
            "UNKNOWN",
            Array.Empty<NodalDisplacementResult>(),
            Array.Empty<SupportReactionResult>(),
            Array.Empty<MemberEndForceResult>());

        StructuralAnalysisException exception = Assert.Throws<StructuralAnalysisException>(() =>
            new GlobalEquilibriumChecker().Check(model, result));

        Assert.Contains("neither a load case nor a load combination", exception.Message);
    }

    private static StructuralModel CreateSimpleSupportedBeamWithUniformLoad()
    {
        StructuralModel model = CreateBaseFrameModel();
        model.AddNode(new StructuralNode("A", 0.0, 0.0));
        model.AddNode(new StructuralNode("B", 5.0, 0.0));
        model.AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC"));
        model.AddSupport(StructuralSupport.Hinge("SA", "A"));
        model.AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
        model.AddLoadCase(new StructuralLoadCase("LC1", "Uniform load"));
        model.AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10.0));
        return model;
    }

    private static StructuralModel CreateCantileverWithTipPointLoad()
    {
        StructuralModel model = CreateBaseFrameModel();
        model.AddNode(new StructuralNode("A", 0.0, 0.0));
        model.AddNode(new StructuralNode("B", 4.0, 0.0));
        model.AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC"));
        model.AddSupport(StructuralSupport.Fixed("SA", "A"));
        model.AddLoadCase(new StructuralLoadCase("LC1", "Tip point load"));
        model.AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalY, -12.0));
        return model;
    }

    private static StructuralModel CreateSimpleSupportedBeamWithLoadCombination()
    {
        StructuralModel model = CreateBaseFrameModel();
        model.AddNode(new StructuralNode("A", 0.0, 0.0));
        model.AddNode(new StructuralNode("B", 5.0, 0.0));
        model.AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC"));
        model.AddSupport(StructuralSupport.Hinge("SA", "A"));
        model.AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
        model.AddLoadCase(new StructuralLoadCase("G1", "Permanent"));
        model.AddLoadCase(new StructuralLoadCase("Q1", "Variable"));
        model.AddLoad(StructuralLoad.UniformDistributedLoad("G1_LOAD", "G1", "M1", StructuralLoadDirection.GlobalY, -5.0));
        model.AddLoad(StructuralLoad.UniformDistributedLoad("Q1_LOAD", "Q1", "M1", StructuralLoadDirection.GlobalY, -10.0));
        model.AddLoadCombination(new StructuralLoadCombination(
            "ULS1",
            "ULS 1",
            new[]
            {
                new StructuralLoadCombinationTerm("G1", 1.0),
                new StructuralLoadCombinationTerm("Q1", 1.1),
            }));
        return model;
    }

    private static StructuralModel CreateInclinedCantileverWithLocalYLoad()
    {
        StructuralModel model = CreateBaseFrameModel();
        model.AddNode(new StructuralNode("A", 0.0, 0.0));
        model.AddNode(new StructuralNode("B", 3.0, 4.0));
        model.AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC"));
        model.AddSupport(StructuralSupport.Fixed("SA", "A"));
        model.AddLoadCase(new StructuralLoadCase("LC1", "Local Y load"));
        model.AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.LocalY, -10.0));
        return model;
    }

    private static StructuralModel CreateSymmetricTriangularTruss()
    {
        StructuralModel model = CreateBaseFrameModel();
        model.AddNode(new StructuralNode("A", 0.0, 0.0));
        model.AddNode(new StructuralNode("B", 4.0, 0.0));
        model.AddNode(new StructuralNode("C", 2.0, 3.0));
        model.AddMember(new StructuralMember("AB", "A", "B", "MAT", "SEC", MemberType.Truss2D));
        model.AddMember(new StructuralMember("AC", "A", "C", "MAT", "SEC", MemberType.Truss2D));
        model.AddMember(new StructuralMember("BC", "B", "C", "MAT", "SEC", MemberType.Truss2D));
        model.AddSupport(StructuralSupport.Hinge("SA", "A"));
        model.AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
        model.AddLoadCase(new StructuralLoadCase("LC1", "Top load"));
        model.AddLoad(StructuralLoad.NodalForce("P1", "LC1", "C", StructuralLoadDirection.GlobalY, -30.0));
        return model;
    }

    private static StructuralModel CreateMixedFrameTrussModel()
    {
        StructuralModel model = CreateBaseFrameModel();
        model.AddNode(new StructuralNode("A", 0.0, 0.0));
        model.AddNode(new StructuralNode("B", 4.0, 0.0));
        model.AddNode(new StructuralNode("C", 0.0, 3.0));
        model.AddNode(new StructuralNode("D", 4.0, 3.0));
        model.AddMember(new StructuralMember("AC", "A", "C", "MAT", "SEC", MemberType.Frame2D));
        model.AddMember(new StructuralMember("CD", "C", "D", "MAT", "SEC", MemberType.Frame2D));
        model.AddMember(new StructuralMember("BD", "B", "D", "MAT", "SEC", MemberType.Frame2D));
        model.AddMember(new StructuralMember("AD", "A", "D", "MAT", "SEC", MemberType.Truss2D));
        model.AddSupport(StructuralSupport.Fixed("SA", "A"));
        model.AddSupport(StructuralSupport.Fixed("SB", "B"));
        model.AddLoadCase(new StructuralLoadCase("LC1", "Top load"));
        model.AddLoad(StructuralLoad.NodalForce("P1", "LC1", "D", StructuralLoadDirection.GlobalY, -20.0));
        model.AddLoad(StructuralLoad.NodalForce("H1", "LC1", "D", StructuralLoadDirection.GlobalX, 5.0));
        return model;
    }

    private static StructuralModel CreateBaseFrameModel()
    {
        StructuralModel model = new();
        model.AddMaterial(new StructuralMaterial("MAT", "Generic steel", 210_000_000.0));
        model.AddSection(new StructuralSection("SEC", "Generic section", 0.01, 0.0001));
        return model;
    }

    private static string FormatResidual(GlobalEquilibriumResult equilibrium) =>
        $"Residuals: Fx={equilibrium.ResidualFx}, Fy={equilibrium.ResidualFy}, Mz={equilibrium.ResidualMz}";
}
