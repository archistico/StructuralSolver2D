using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Integration;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Tests.M87;

/// <summary>M87-C station screening only; no EC3/EC5/NTC calculations.</summary>
public sealed class FemCandidateStationSelectorM87CTests
{
    private static readonly NormativeProfile Profile = new(
        "SYNTHETIC", new DesignCodeEdition("NOT-A-NORM", "TEST", "NONE"),
        Array.Empty<NormativeCoefficient>());
    private static DesignMemberContext Member() => new("M", DesignMaterial.Steel, "SEC", 4);
    private static StructuralModel Beam() => new StructuralModel()
        .AddNode(new StructuralNode("A", 0, 0))
        .AddNode(new StructuralNode("B", 4, 0))
        .AddMaterial(new StructuralMaterial("MAT", "Generic", 210_000_000))
        .AddSection(new StructuralSection("SEC", "Generic", .003, .00002))
        .AddMember(new StructuralMember("M", "A", "B", "MAT", "SEC", MemberType.Frame2D))
        .AddLoadCase(new StructuralLoadCase("G", "G"))
        .AddLoadCase(new StructuralLoadCase("Q", "Q"))
        .AddSupport(StructuralSupport.Hinge("SA", "A"))
        .AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
    private static IReadOnlyList<FemSectionCandidateM87C> Capture(StructuralModel model,
        FemCandidateTargetM87C? target = null,
        IReadOnlyList<FemExtraSectionM87C>? extra = null, int grid = 0) =>
        new FemCandidateStationSelectorM87C().CaptureCandidates(model, Member(), DesignLimitState.Ultimate,
            Profile, new[] { "EC3-NOT-IMPLEMENTED" },
            target ?? new FemCandidateTargetM87C(StructuralAnalysisTargetKind.LoadCase, "G"), extra, grid);

    [Fact]
    public void UniformLoad_ExactInteriorBendingMaximumIsCapturedWithConcomitantZeroShear()
    {
        // L=4m, q=-8 kN/m, reactions +16, Mmax = q*L^2/8 = 16 at x=2.
        StructuralModel model = Beam().AddLoad(StructuralLoad.UniformDistributedLoad(
            "g", "G", "M", StructuralLoadDirection.GlobalY, -8));
        var stations = Capture(model);
        Assert.Contains(stations, c => c.Request.Effects.PositionMetres == 0);
        Assert.Contains(stations, c => c.Request.Effects.PositionMetres == 4);
        var mid = Assert.Single(stations, c => Math.Abs(c.Request.Effects.PositionMetres - 2) < 1e-9);
        Assert.Equal(0, mid.Request.Effects.AxialForceKn!.Value, 8);
        Assert.Equal(0, mid.Request.Effects.ShearForceYKn!.Value, 8);
        Assert.Equal(16, mid.Request.Effects.BendingMomentZKnm!.Value, 8);
        Assert.Contains("BendingMoment:maximum", mid.Reasons);
        Assert.Equal("G", mid.Request.Effects.CombinationId);
        Assert.Equal(DesignEffectTargetKind.LoadCase, mid.Request.Effects.TargetKind);
    }

    [Fact]
    public void MemberPointLoad_PreservesTwoSidedShearJumpAtSameLocation()
    {
        StructuralModel model = Beam().AddLoad(StructuralLoad.PointLoadOnMember(
            "p", "G", "M", StructuralLoadDirection.GlobalY, -12, .5));
        var atPoint = Capture(model).Where(c => Math.Abs(c.Request.Effects.PositionMetres - 2) < 1e-9).ToArray();
        Assert.Equal(2, atPoint.Length);
        var before = Assert.Single(atPoint, c => c.Request.Effects.SectionSide == DesignEffectSectionSide.BeforePointLoad);
        var after = Assert.Single(atPoint, c => c.Request.Effects.SectionSide == DesignEffectSectionSide.AfterPointLoad);
        Assert.Equal(6, before.Request.Effects.ShearForceYKn!.Value, 8);
        Assert.Equal(-6, after.Request.Effects.ShearForceYKn!.Value, 8);
        Assert.Equal(12, before.Request.Effects.BendingMomentZKnm!.Value, 8);
        Assert.Equal(12, after.Request.Effects.BendingMomentZKnm!.Value, 8);
        Assert.Contains("member-point-load", before.Reasons);
        Assert.Contains("member-point-load", after.Reasons);
    }

    [Fact]
    public void MixedAxialAndBending_AlwaysUsesConcomitantSignedComponents()
    {
        StructuralModel model = Beam()
            .AddLoad(StructuralLoad.UniformDistributedLoad("g", "G", "M", StructuralLoadDirection.GlobalY, -8))
            .AddLoad(StructuralLoad.NodalForce("axial", "G", "B", StructuralLoadDirection.GlobalX, -9));
        // One horizontal reaction is permitted by the hinged left support.
        var candidates = Capture(model);
        var mid = Assert.Single(candidates, c => Math.Abs(c.Request.Effects.PositionMetres - 2) < 1e-9);
        Assert.Equal(-9, mid.Request.Effects.AxialForceKn!.Value, 7);
        Assert.Equal(0, mid.Request.Effects.ShearForceYKn!.Value, 7);
        Assert.Equal(16, mid.Request.Effects.BendingMomentZKnm!.Value, 7);
    }

    [Fact]
    public void MultipleManualCombinations_NeverMixProvenance()
    {
        StructuralModel model = Beam()
            .AddLoad(StructuralLoad.UniformDistributedLoad("g", "G", "M", StructuralLoadDirection.GlobalY, -8))
            .AddLoad(StructuralLoad.UniformDistributedLoad("q", "Q", "M", StructuralLoadDirection.GlobalY, 4))
            .AddLoadCombination(new StructuralLoadCombination("ZERO", "zero", new[] {
                new StructuralLoadCombinationTerm("G", 1), new StructuralLoadCombinationTerm("Q", 2) }))
            .AddLoadCombination(new StructuralLoadCombination("HIGH", "high", new[] {
                new StructuralLoadCombinationTerm("G", 1), new StructuralLoadCombinationTerm("Q", -1) }));
        var all = new FemCandidateStationSelectorM87C().CaptureAcrossTargets(model, Member(),
            DesignLimitState.Ultimate, Profile, Array.Empty<string>(),
            new[] {new FemCandidateTargetM87C(StructuralAnalysisTargetKind.LoadCombination, "ZERO"),
                new FemCandidateTargetM87C(StructuralAnalysisTargetKind.LoadCombination, "HIGH")});
        Assert.All(all, c => Assert.Equal(DesignEffectTargetKind.LoadCombination, c.Request.Effects.TargetKind));
        Assert.All(all.Where(c => c.Request.Effects.CombinationId == "ZERO"), c =>
            Assert.Equal(0, c.Request.Effects.BendingMomentZKnm!.Value, 8));
        var highMid = Assert.Single(all.Where(c => c.Request.Effects.CombinationId == "HIGH"), c =>
            Math.Abs(c.Request.Effects.PositionMetres - 2) < 1e-9);
        Assert.Equal(24, highMid.Request.Effects.BendingMomentZKnm!.Value, 8);
    }

    [Fact]
    public void OptionalGridAndExplicitStations_RemainInSameAnalyzedResult()
    {
        StructuralModel model = Beam().AddLoad(StructuralLoad.UniformDistributedLoad(
            "g", "G", "M", StructuralLoadDirection.GlobalY, -8));
        var stations = Capture(model, extra: new[] { new FemExtraSectionM87C(1.37) }, grid: 4);
        var custom = Assert.Single(stations, c => Math.Abs(c.Request.Effects.PositionMetres - 1.37) < 1e-12);
        Assert.Equal(16 * 1.37 - 4 * 1.37 * 1.37,
            custom.Request.Effects.BendingMomentZKnm!.Value, 8);
        Assert.Contains("explicit-station", custom.Reasons);
        Assert.Contains(stations, c => Math.Abs(c.Request.Effects.PositionMetres - 1) < 1e-12);
        Assert.Contains(stations, c => Math.Abs(c.Request.Effects.PositionMetres - 3) < 1e-12);
        Assert.All(stations, c => Assert.Equal("G", c.Request.Effects.CombinationId));
    }

    [Fact]
    public void IncompleteRuleRegistry_CannotCreateAMemberPass()
    {
        StructuralModel model = Beam().AddLoad(StructuralLoad.UniformDistributedLoad(
            "g", "G", "M", StructuralLoadDirection.GlobalY, -8));
        var engine = new DesignCheckEngine(Array.Empty<IDesignCheckRule>());
        var reports = Capture(model).Select(c => engine.Evaluate(c.Request)).ToArray();
        Assert.NotEmpty(reports);
        Assert.All(reports, r => {
            Assert.Equal(DesignOverallStatus.Incomplete, r.OverallStatus);
            Assert.False(r.IsComplete);
        });
    }

    [Fact]
    public void RejectsInvalidTargetsDuplicateTargetsGridAndAmbiguousJump()
    {
        StructuralModel model = Beam().AddLoad(StructuralLoad.PointLoadOnMember(
            "p", "G", "M", StructuralLoadDirection.GlobalY, -12, .5));
        Assert.Throws<ArgumentException>(() => Capture(model,
            new FemCandidateTargetM87C(StructuralAnalysisTargetKind.LoadCombination, "G")));
        Assert.Throws<ArgumentOutOfRangeException>(() => Capture(model, grid: 2001));
        Assert.Throws<ArgumentException>(() => Capture(model, extra: new[] {new FemExtraSectionM87C(2)}));
        Assert.Throws<ArgumentException>(() => Capture(model, extra: new[] {
            new FemExtraSectionM87C(1, SectionLimitM78.AfterPointLoad)}));
        Assert.Throws<ArgumentOutOfRangeException>(() => Capture(model, extra: new[] {new FemExtraSectionM87C(9)}));
        var selector = new FemCandidateStationSelectorM87C();
        Assert.Throws<ArgumentException>(() => selector.CaptureAcrossTargets(model, Member(),
            DesignLimitState.Ultimate, Profile, Array.Empty<string>(),
            new[] {new FemCandidateTargetM87C(StructuralAnalysisTargetKind.LoadCase, "G"),
                new FemCandidateTargetM87C(StructuralAnalysisTargetKind.LoadCase, "g")}));
    }

    [Fact]
    public void DeclaredMemberSectionAndLengthMustMatchFEM()
    {
        StructuralModel model = Beam().AddLoad(StructuralLoad.UniformDistributedLoad(
            "g", "G", "M", StructuralLoadDirection.GlobalY, -8));
        var selector = new FemCandidateStationSelectorM87C();
        void Call(DesignMemberContext member) => selector.CaptureCandidates(model, member,
            DesignLimitState.Ultimate, Profile, Array.Empty<string>(),
            new FemCandidateTargetM87C(StructuralAnalysisTargetKind.LoadCase, "G"));
        Assert.Throws<ArgumentException>(() => Call(Member() with { MemberLengthMetres = 4.3 }));
        Assert.Throws<ArgumentException>(() => Call(Member() with { SectionId = "BAD" }));
    }
}
