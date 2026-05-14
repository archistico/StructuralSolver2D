using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

/// <summary>
/// Validation tests for local/global conventions, inclined members and member orientation.
/// These tests intentionally use a 3-4-5 inclined member so the expected force projections
/// can be checked by hand.
/// </summary>
public sealed class Frame2DLocalGlobalConventionTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;
    private const double Length = 5.0;
    private const double Cosine = 0.6;
    private const double Sine = 0.8;

    [Fact]
    public void Analyze_InclinedCantileverWithTipGlobalVerticalLoad_ShouldMatchGlobalEquilibrium()
    {
        const double force = 20.0;

        StructuralModel model = CreateInclinedCantilever(startAtBase: true)
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalY, -force));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");

        Assert.Equal(0.0, result.GetReaction("SA").Fx, precision: 9);
        Assert.Equal(force, result.GetReaction("SA").Fy, precision: 9);
        Assert.Equal(3.0 * force, result.GetReaction("SA").Mz, precision: 9);
    }

    [Fact]
    public void Analyze_InclinedCantileverWithLocalYUniformLoad_ShouldRotateLoadWithElementAxis()
    {
        const double load = 10.0;

        StructuralModel model = CreateInclinedCantilever(startAtBase: true)
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.LocalY, -load));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");

        // Local y for an A(0,0) -> B(3,4) member is (-sin, cos) = (-0.8, 0.6).
        // A negative LocalY uniform load therefore has total global force (+40, -30) kN.
        // The fixed support must react with (-40, +30) kN and +125 kNm.
        Assert.Equal(-40.0, result.GetReaction("SA").Fx, precision: 6);
        Assert.Equal(30.0, result.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(125.0, result.GetReaction("SA").Mz, precision: 6);
    }

    [Fact]
    public void Analyze_InclinedCantileverWithGlobalYUniformLoad_ShouldUseGlobalDirectionIndependentFromElementAxis()
    {
        const double load = 10.0;

        StructuralModel model = CreateInclinedCantilever(startAtBase: true)
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -load));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");

        // Total vertical load is -10 kN/m * 5 m = -50 kN.
        // Its resultant acts at the member midpoint: (1.5, 2.0), so Mz = 1.5 * 50 = 75 kNm.
        Assert.Equal(0.0, result.GetReaction("SA").Fx, precision: 9);
        Assert.Equal(50.0, result.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(75.0, result.GetReaction("SA").Mz, precision: 6);
    }

    [Fact]
    public void Analyze_GlobalYUniformLoad_ShouldBeIndependentFromMemberOrientation()
    {
        const double load = 10.0;

        StructuralModel forward = CreateInclinedCantilever(startAtBase: true)
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -load));
        StructuralModel reversed = CreateInclinedCantilever(startAtBase: false)
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -load));

        Frame2DAnalyzer analyzer = new();
        var forwardResult = analyzer.Analyze(forward, "LC1");
        var reversedResult = analyzer.Analyze(reversed, "LC1");

        Assert.Equal(forwardResult.GetReaction("SA").Fx, reversedResult.GetReaction("SA").Fx, precision: 9);
        Assert.Equal(forwardResult.GetReaction("SA").Fy, reversedResult.GetReaction("SA").Fy, precision: 9);
        Assert.Equal(forwardResult.GetReaction("SA").Mz, reversedResult.GetReaction("SA").Mz, precision: 9);
        Assert.Equal(forwardResult.GetDisplacement("B").Ux, reversedResult.GetDisplacement("B").Ux, precision: 12);
        Assert.Equal(forwardResult.GetDisplacement("B").Uy, reversedResult.GetDisplacement("B").Uy, precision: 12);
    }

    [Fact]
    public void Analyze_LocalYUniformLoad_ShouldFollowMemberOrientation()
    {
        const double load = 10.0;

        StructuralModel forward = CreateInclinedCantilever(startAtBase: true)
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.LocalY, -load));
        StructuralModel reversed = CreateInclinedCantilever(startAtBase: false)
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.LocalY, -load));

        Frame2DAnalyzer analyzer = new();
        var forwardResult = analyzer.Analyze(forward, "LC1");
        var reversedResult = analyzer.Analyze(reversed, "LC1");

        // Reversing the member reverses its local x axis and therefore also its local y axis.
        Assert.Equal(-forwardResult.GetReaction("SA").Fx, reversedResult.GetReaction("SA").Fx, precision: 6);
        Assert.Equal(-forwardResult.GetReaction("SA").Fy, reversedResult.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(-forwardResult.GetReaction("SA").Mz, reversedResult.GetReaction("SA").Mz, precision: 6);
    }

    [Fact]
    public void Analyze_InclinedCantileverWithAxialTipLoad_ShouldMatchClosedFormAxialDisplacement()
    {
        const double axialLoad = 100.0;

        StructuralModel model = CreateInclinedCantilever(startAtBase: true)
            .AddLoad(StructuralLoad.NodalForce("PX", "LC1", "B", StructuralLoadDirection.GlobalX, axialLoad * Cosine))
            .AddLoad(StructuralLoad.NodalForce("PY", "LC1", "B", StructuralLoadDirection.GlobalY, axialLoad * Sine));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");

        double expectedAxialElongation = axialLoad * Length / (ElasticModulus * Area);

        Assert.Equal(-axialLoad * Cosine, result.GetReaction("SA").Fx, precision: 6);
        Assert.Equal(-axialLoad * Sine, result.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(0.0, result.GetReaction("SA").Mz, precision: 6);
        Assert.Equal(expectedAxialElongation * Cosine, result.GetDisplacement("B").Ux, precision: 12);
        Assert.Equal(expectedAxialElongation * Sine, result.GetDisplacement("B").Uy, precision: 12);
    }

    [Fact]
    public void Analyze_MixedFrameWithInclinedTrussBrace_ShouldSatisfyGlobalEquilibrium()
    {
        StructuralModel model = CreateMixedBracedPortalModel()
            .AddLoad(StructuralLoad.NodalForce("H1", "LC1", "D", StructuralLoadDirection.GlobalX, 20.0))
            .AddLoad(StructuralLoad.NodalForce("V1", "LC1", "D", StructuralLoadDirection.GlobalY, -10.0));

        var result = new PlaneStructureAnalyzer().Analyze(model, "LC1");

        double residualFx = result.Reactions.Sum(reaction => reaction.Fx) + 20.0;
        double residualFy = result.Reactions.Sum(reaction => reaction.Fy) - 10.0;
        double braceNormalForce = result.MemberEndForces.Single(force => force.MemberId == "T1").EndAxial;

        Assert.Equal(0.0, residualFx, precision: 6);
        Assert.Equal(0.0, residualFy, precision: 6);
        Assert.NotEqual(0.0, Math.Round(braceNormalForce, 6));
    }

    private static StructuralModel CreateBaseModel() =>
        new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddLoadCase(new StructuralLoadCase("LC1", "Main load case"));

    private static StructuralModel CreateInclinedCantilever(bool startAtBase)
    {
        StructuralModel model = CreateBaseModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 3.0, 4.0))
            .AddSupport(StructuralSupport.Fixed("SA", "A"));

        return startAtBase
            ? model.AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            : model.AddMember(new StructuralMember("M1", "B", "A", "MAT", "SEC", MemberType.Frame2D));
    }

    private static StructuralModel CreateMixedBracedPortalModel() =>
        CreateBaseModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 4.0, 0.0))
            .AddNode(new StructuralNode("C", 0.0, 3.0))
            .AddNode(new StructuralNode("D", 4.0, 3.0))
            .AddMember(new StructuralMember("F1", "A", "C", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("F2", "C", "D", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("F3", "B", "D", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("T1", "A", "D", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(StructuralSupport.Fixed("SB", "B"));
}
