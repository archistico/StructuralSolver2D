using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

/// <summary>
/// Validates local end moment releases for Frame2D members.
/// Moment releases are implemented as element-level releases and should not require the user
/// to artificially restrain otherwise inactive global nodal rotations.
/// </summary>
public sealed class Frame2DMemberReleaseTests
{
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;

    [Fact]
    public void Analyze_PinEndedBeamWithUniformLoad_ShouldReturnZeroEndMomentsAndExpectedReactions()
    {
        const double length = 5.0;
        const double load = 10.0;

        StructuralModel model = CreateReleasedBeamModel(length, releaseStart: true, releaseEnd: true)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -load));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var endForces = result.MemberEndForces.First(force => force.MemberId == "M1");
        var diagram = new Frame2DInternalForceSampler().SampleMember(model, result, "M1", sampleCount: 11);

        Assert.Equal(load * length / 2.0, result.GetReaction("SA").Fy, precision: 6);
        Assert.Equal(load * length / 2.0, result.GetReaction("SB").Fy, precision: 6);
        Assert.Equal(0.0, endForces.StartMoment, precision: 9);
        Assert.Equal(0.0, endForces.EndMoment, precision: 9);
        Assert.Equal(load * length * length / 8.0, diagram.MaxAbsBendingMoment, precision: 6);
    }

    [Fact]
    public void Analyze_StartMomentRelease_ShouldKeepStartMomentZero()
    {
        const double length = 4.0;
        const double load = 8.0;

        StructuralModel model = CreateReleasedBeamModel(length, releaseStart: true, releaseEnd: false)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(StructuralSupport.Fixed("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "M1", StructuralLoadDirection.GlobalY, -load));

        var result = new Frame2DAnalyzer().Analyze(model, "LC1");
        var endForces = result.MemberEndForces.First(force => force.MemberId == "M1");

        Assert.Equal(0.0, endForces.StartMoment, precision: 9);
        Assert.NotEqual(0.0, Math.Round(endForces.EndMoment, 9));
    }

    private static StructuralModel CreateReleasedBeamModel(double length, bool releaseStart, bool releaseEnd) =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", length, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember(
                "M1",
                "A",
                "B",
                "MAT",
                "SEC",
                MemberType.Frame2D,
                "Released beam",
                releaseStart,
                releaseEnd))
            .AddLoadCase(new StructuralLoadCase("LC1", "First load case"));
}
