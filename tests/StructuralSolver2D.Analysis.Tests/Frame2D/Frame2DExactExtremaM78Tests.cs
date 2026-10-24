using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

public sealed class Frame2DExactExtremaM78Tests
{
    private static StructuralModel Beam(double length = 5.0) =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", length, 0))
            .AddMaterial(new StructuralMaterial("MAT", "steel", 210_000_000))
            .AddSection(new StructuralSection("SEC", "section", .004, .00002))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("G", "permanent"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"));

    [Fact]
    public void UniformLoad_FindTrueMidspanMaximumWithOnlyTwoGraphicsSamples()
    {
        StructuralModel model = Beam().AddLoad(StructuralLoad.UniformDistributedLoad(
            "Q", "G", "M1", StructuralLoadDirection.GlobalY, -10));
        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "G");
        MemberInternalForceDiagram coarse = new Frame2DInternalForceSampler().SampleMember(model, result, "M1", 2);
        Assert.True(coarse.MaxAbsBendingMoment < 0.000001);

        ExactMemberInternalForceExtremaM78 extremes = new Frame2DExactExtremaFinderM78().Find(model, result, "M1");
        ExactInternalForceExtremeM78 maximum = extremes.Maximum(InternalForceQuantity.BendingMoment);
        Assert.Equal(31.25, maximum.Value, 7);
        Assert.Equal(2.5, maximum.Distance, 8);
        Assert.Equal(SectionLimitM78.Continuous, maximum.Side);
        Assert.Equal(25, extremes.MaximumAbsolute(InternalForceQuantity.ShearForce).Value, 6);
    }

    [Fact]
    public void TriangularLoad_QuadraticShearRootIsRecoveredWithoutSamplingAlias()
    {
        const double length = 6;
        const double tipLoad = 12;
        StructuralModel model = Beam(length).AddLoad(StructuralLoad.LinearDistributedLoad(
            "T", "G", "M1", StructuralLoadDirection.GlobalY, 0, -tipLoad));
        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "G");
        var extreme = new Frame2DExactExtremaFinderM78().Find(model, result, "M1")
            .Maximum(InternalForceQuantity.BendingMoment);
        Assert.Equal(length / Math.Sqrt(3), extreme.Distance, 7);
        Assert.Equal(tipLoad * length * length / (9 * Math.Sqrt(3)), extreme.Value, 6);
    }

    [Fact]
    public void InteriorPointLoad_OneSidedShearAndExactMomentPeakArePreserved()
    {
        StructuralModel model = Beam(8).AddLoad(StructuralLoad.PointLoadOnMember(
            "P", "G", "M1", StructuralLoadDirection.GlobalY, -12, .25));
        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "G");
        var extrema = new Frame2DExactExtremaFinderM78().Find(model, result, "M1");
        var moment = extrema.Maximum(InternalForceQuantity.BendingMoment);
        Assert.Equal(18, moment.Value, 7);
        Assert.Equal(2, moment.Distance, 8);
        var maxShear = extrema.Maximum(InternalForceQuantity.ShearForce);
        var minShear = extrema.Minimum(InternalForceQuantity.ShearForce);
        Assert.Equal(9, maxShear.Value, 7);
        Assert.Equal(-3, minShear.Value, 7);
        // Without distributed load, V=+9 on [0,2) and V=-3 on (2,8].
        // Both extrema occur over whole intervals. M78 resolves ties in stable
        // candidate order: start of member, end of member, then jump limits.
        // Consequently the representative maximum is x=0 and minimum x=8,
        // both Continuous; neither side label is forced for a tied plateau.
        Assert.Equal(0, maxShear.Distance, 8);
        Assert.Equal(SectionLimitM78.Continuous, maxShear.Side);
        Assert.Equal(8, minShear.Distance, 8);
        Assert.Equal(SectionLimitM78.Continuous, minShear.Side);

        // Independently verify the actual one-sided physical values at x=2.
        // The shear jumps by -12 kN but the bending moment stays continuous.
        var profile = Frame2DInternalForceSampler.PrepareMember(model, result, "M1");
        var before = profile.Evaluate(2, after: false);
        var after = profile.Evaluate(2, after: true);
        Assert.Equal(9, before.ShearForce, 7);
        Assert.Equal(-3, after.ShearForce, 7);
        Assert.Equal(-12, after.ShearForce - before.ShearForce, 7);
        Assert.Equal(18, before.BendingMoment, 7);
        Assert.Equal(before.BendingMoment, after.BendingMoment, 8);
        Assert.Equal(-3, profile.Evaluate(8, after: false).ShearForce, 7);
    }

    [Fact]
    public void PointLoadWithDistributedGradient_UniqueShearExtremaRetainBothOneSidedLimits()
    {
        // A: 5 kN, B: -1 kN (upward 1 kN/m plus downward 12 kN at x=2 m).
        // V(x) increases from +5 to +7 on the left and jumps to -5 on the right.
        // Unlike a flat shear plateau, both extrema are unique at the jump.
        StructuralModel model = Beam(8)
            .AddLoad(StructuralLoad.UniformDistributedLoad(
                "U", "G", "M1", StructuralLoadDirection.GlobalY, +1))
            .AddLoad(StructuralLoad.PointLoadOnMember(
                "P", "G", "M1", StructuralLoadDirection.GlobalY, -12, .25));
        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "G");
        var extrema = new Frame2DExactExtremaFinderM78().Find(model, result, "M1");

        var maxShear = extrema.Maximum(InternalForceQuantity.ShearForce);
        var minShear = extrema.Minimum(InternalForceQuantity.ShearForce);
        Assert.Equal(7, maxShear.Value, 7);
        Assert.Equal(2, maxShear.Distance, 8);
        Assert.Equal(SectionLimitM78.BeforePointLoad, maxShear.Side);
        Assert.Equal(-5, minShear.Value, 7);
        Assert.Equal(2, minShear.Distance, 8);
        Assert.Equal(SectionLimitM78.AfterPointLoad, minShear.Side);
    }

    [Fact]
    public void NegativeCombination_PreservesSignedExtremaAndResultIdentity()
    {
        StructuralModel model = Beam().AddLoad(StructuralLoad.UniformDistributedLoad(
            "Q", "G", "M1", StructuralLoadDirection.GlobalY, -10))
            .AddLoadCombination(new StructuralLoadCombination("NEG", "negative", new[]
            {
                new StructuralLoadCombinationTerm("G", -2),
            }));
        StructuralAnalysisResult result = new Frame2DAnalyzer().AnalyzeCombination(model, "NEG");
        var extrema = new Frame2DExactExtremaFinderM78().Find(model, result, "M1");
        var minimum = extrema.Minimum(InternalForceQuantity.BendingMoment);
        Assert.Equal(-62.5, minimum.Value, 7);
        Assert.Equal(2.5, minimum.Distance, 7);
        Assert.Equal("NEG", minimum.ResultId);
        Assert.Equal(-62.5, extrema.MaximumAbsolute(InternalForceQuantity.BendingMoment).Value, 7);
    }

    [Fact]
    public void Cantilever_ExactFixedEndMomentAndMultipleMembers()
    {
        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2, 0))
            .AddNode(new StructuralNode("C", 4, 0))
            .AddMaterial(new StructuralMaterial("MAT", "steel", 210_000_000))
            .AddSection(new StructuralSection("SEC", "section", .004, .00002))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("M2", "B", "C", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("G", "permanent"))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "G", "M1", StructuralLoadDirection.GlobalY, -10))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q2", "G", "M2", StructuralLoadDirection.GlobalY, -10));
        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "G");
        var all = new StructuralSolver2DService().FindExactStaticMemberForceExtrema(model, result);
        Assert.Equal(2, all.Count);
        var first = Assert.Single(all, x => x.MemberId == "M1");
        Assert.Equal(-80, first.Minimum(InternalForceQuantity.BendingMoment).Value, 6);
        Assert.Equal(0, first.Minimum(InternalForceQuantity.BendingMoment).Distance, 8);
    }
}
