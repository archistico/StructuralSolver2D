using StructuralSolver2D.Analysis;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

/// <summary>
/// Analytical benchmark tests for the first Frame2D solver.
/// These tests intentionally use simple structural schemes with closed-form reference results.
/// </summary>
public sealed class Frame2DAnalyzerValidationTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;
    private const double FlexuralRigidity = ElasticModulus * Inertia;
    private const double AxialRigidity = ElasticModulus * Area;

    [Fact]
    public void Analyze_SimplySupportedBeamWithMidspanPointLoad_ShouldMatchClosedFormReactionsAndDeflection()
    {
        const double length = 6.0;
        const double force = 12.0;

        StructuralModel model = CreateThreeNodeBeamModel(length)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "C", StructuralLoadDirection.GlobalY, -force));

        Frame2DAnalyzer analyzer = new();

        var result = analyzer.Analyze(model, "LC1");

        double expectedReaction = force / 2.0;
        double expectedMidspanDeflection = -(force * Math.Pow(length, 3)) / (48.0 * FlexuralRigidity);

        Assert.Equal(expectedReaction, result.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(expectedReaction, result.GetReaction("SB").Fy, precision: 6);
        Assert.Equal(expectedMidspanDeflection, result.GetDisplacement("C").Uy, precision: 9);
        Assert.Equal(0.0, result.GetDisplacement("C").Rz, precision: 9);
    }

    [Fact]
    public void Analyze_CantileverWithUniformLoad_ShouldMatchClosedFormReactionMomentDeflectionAndRotation()
    {
        const double length = 5.0;
        const double load = 10.0;

        StructuralModel model = CreateSingleMemberBeamModel(length)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -load));

        Frame2DAnalyzer analyzer = new();

        var result = analyzer.Analyze(model, "LC1");

        double expectedReaction = load * length;
        double expectedMoment = load * length * length / 2.0;
        double expectedTipDeflection = -(load * Math.Pow(length, 4)) / (8.0 * FlexuralRigidity);
        double expectedTipRotation = -(load * Math.Pow(length, 3)) / (6.0 * FlexuralRigidity);

        Assert.Equal(expectedReaction, result.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(expectedMoment, result.GetReaction("SA").Mz, precision: 6);
        Assert.Equal(expectedTipDeflection, result.GetDisplacement("B").Uy, precision: 9);
        Assert.Equal(expectedTipRotation, result.GetDisplacement("B").Rz, precision: 9);
    }

    [Fact]
    public void Analyze_AxiallyLoadedMember_ShouldMatchClosedFormElongationAndReaction()
    {
        const double length = 5.0;
        const double force = 100.0;

        StructuralModel model = CreateSingleMemberBeamModel(length)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(new StructuralSupport("SB", "B", false, true, true, SupportType.Custom, "Transverse and rotational restraint"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalX, force));

        Frame2DAnalyzer analyzer = new();

        var result = analyzer.Analyze(model, "LC1");

        double expectedElongation = force * length / AxialRigidity;

        Assert.Equal(-force, result.GetReaction("SA").Fx, precision: 6);
        Assert.Equal(expectedElongation, result.GetDisplacement("B").Ux, precision: 12);
        Assert.Equal(0.0, result.GetDisplacement("B").Uy, precision: 12);
        Assert.Equal(0.0, result.GetDisplacement("B").Rz, precision: 12);
    }

    [Fact]
    public void Analyze_ModelWithNoLoads_ShouldReturnZeroDisplacementsReactionsAndEndForces()
    {
        StructuralModel model = CreateSingleMemberBeamModel(5.0)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"));

        Frame2DAnalyzer analyzer = new();

        var result = analyzer.Analyze(model, "LC1");

        Assert.All(result.Displacements, displacement =>
        {
            Assert.Equal(0.0, displacement.Ux, precision: 12);
            Assert.Equal(0.0, displacement.Uy, precision: 12);
            Assert.Equal(0.0, displacement.Rz, precision: 12);
        });

        Assert.All(result.Reactions, reaction =>
        {
            Assert.Equal(0.0, reaction.Fx, precision: 12);
            Assert.Equal(0.0, reaction.Fy, precision: 12);
            Assert.Equal(0.0, reaction.Mz, precision: 12);
        });

        Assert.All(result.MemberEndForces, memberEndForce =>
        {
            Assert.Equal(0.0, memberEndForce.StartAxial, precision: 12);
            Assert.Equal(0.0, memberEndForce.StartShear, precision: 12);
            Assert.Equal(0.0, memberEndForce.StartMoment, precision: 12);
            Assert.Equal(0.0, memberEndForce.EndAxial, precision: 12);
            Assert.Equal(0.0, memberEndForce.EndShear, precision: 12);
            Assert.Equal(0.0, memberEndForce.EndMoment, precision: 12);
        });
    }

    [Fact]
    public void Analyze_ShouldUseOnlyLoadsFromRequestedLoadCase()
    {
        StructuralModel model = CreateSingleMemberBeamModel(5.0)
            .AddLoadCase(new StructuralLoadCase("LC2", "Second load case"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC2", "B", StructuralLoadDirection.GlobalY, -10.0));

        Frame2DAnalyzer analyzer = new();

        var result = analyzer.Analyze(model, "LC1");

        Assert.Equal(0.0, result.GetReaction("SA").Fy, precision: 12);
        Assert.Equal(0.0, result.GetReaction("SB").Fy, precision: 12);
        Assert.Equal(0.0, result.GetDisplacement("B").Uy, precision: 12);
    }

    [Fact]
    public void Analyze_SymmetricPortalWithSymmetricVerticalLoads_ShouldReturnSymmetricResults()
    {
        StructuralModel model = CreatePortalModel()
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(StructuralSupport.Fixed("SD", "D"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC1", "B", StructuralLoadDirection.GlobalY, -10.0))
            .AddLoad(StructuralLoad.NodalForce("P2", "LC1", "C", StructuralLoadDirection.GlobalY, -10.0));

        Frame2DAnalyzer analyzer = new();

        var result = analyzer.Analyze(model, "LC1");

        Assert.Equal(10.0, result.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(10.0, result.GetReaction("SD").Fy, precision: 6);
        Assert.Equal(-result.GetReaction("SD").Fx, result.GetReaction("SA").Fx, precision: 6);
        Assert.Equal(result.GetDisplacement("C").Uy, result.GetDisplacement("B").Uy, precision: 9);
        Assert.Equal(-result.GetDisplacement("C").Ux, result.GetDisplacement("B").Ux, precision: 9);
        Assert.Equal(-result.GetDisplacement("C").Rz, result.GetDisplacement("B").Rz, precision: 9);
    }

    [Fact]
    public void Analyze_PointLoadOnMember_ShouldThrowClearNotSupportedException()
    {
        StructuralModel model = CreateSingleMemberBeamModel(5.0)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.PointLoadOnMember("P1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10.0, 0.5));

        Frame2DAnalyzer analyzer = new();

        StructuralAnalysisException exception = Assert.Throws<StructuralAnalysisException>(() => analyzer.Analyze(model, "LC1"));
        Assert.Contains("Point loads on members are not supported", exception.Message);
    }

    private static StructuralModel CreateSingleMemberBeamModel(double length) =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", length, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("LC1", "First load case"));

    private static StructuralModel CreateThreeNodeBeamModel(double length) =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("C", length / 2.0, 0.0))
            .AddNode(new StructuralNode("B", length, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember("M1", "A", "C", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("M2", "C", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("LC1", "First load case"));

    private static StructuralModel CreatePortalModel() =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 0.0, 3.0))
            .AddNode(new StructuralNode("C", 4.0, 3.0))
            .AddNode(new StructuralNode("D", 4.0, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("M2", "B", "C", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("M3", "D", "C", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("LC1", "First load case"));
}
