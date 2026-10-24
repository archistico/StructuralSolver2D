using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Integration;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Tests.M87;

/// <summary>All rules here are synthetics for testing the bridge, NOT EC3/EC5/NTC checks.</summary>
public sealed class FemStationVerificationBridgeM87BTests
{
    private const double L = 4;
    private static readonly NormativeProfile TestProfile = new(
        "SYNTHETIC", new DesignCodeEdition("NOT-A-NORM", "SAMPLE", "NA-TEST"),
        Array.Empty<NormativeCoefficient>());

    private static StructuralModel Beam() => new StructuralModel()
        .AddNode(new StructuralNode("A", 0, 0))
        .AddNode(new StructuralNode("B", L, 0))
        .AddMaterial(new StructuralMaterial("MAT", "Generic", 210_000_000))
        .AddSection(new StructuralSection("SEC", "Generic", 0.003, 0.00002))
        .AddMember(new StructuralMember("M", "A", "B", "MAT", "SEC", MemberType.Frame2D))
        .AddLoadCase(new StructuralLoadCase("G", "G"))
        .AddLoadCase(new StructuralLoadCase("Q", "Q"));

    private static DesignMemberContext Member() => new("M", DesignMaterial.Steel, "SEC", L);

    private static DesignCheckRequest Capture(StructuralModel model, string id, double x,
        StructuralAnalysisTargetKind kind = StructuralAnalysisTargetKind.LoadCase,
        SectionLimitM78 side = SectionLimitM78.Continuous) =>
        new FemStationVerificationBridgeM87B().Capture(model, Member(), DesignLimitState.Ultimate,
            TestProfile, new[] { "EC3-NOT-IMPLEMENTED" }, new(kind, id, x, side));

    [Fact]
    public void ExactOffGridSection_CarriesConcomitantLocalForcesAndCompleteProvenance()
    {
        // Cantilever P=10 kN downward at free end: V=+10, M=-10*(4-x), N=0.
        StructuralModel model = Beam()
            .AddSupport(StructuralSupport.Fixed("S", "A"))
            .AddLoad(StructuralLoad.NodalForce("P", "G", "B", StructuralLoadDirection.GlobalY, -10));
        DesignCheckRequest r = Capture(model, "G", 1.37);
        Assert.Equal(1.37, r.Effects.PositionMetres, 12);
        Assert.Equal(0, r.Effects.AxialForceKn!.Value, 8);
        Assert.Equal(10, r.Effects.ShearForceYKn!.Value, 8);
        Assert.Equal(-26.3, r.Effects.BendingMomentZKnm!.Value, 8);
        Assert.Equal("G", r.Effects.CombinationId);
        Assert.Equal("FEM:linear-static:first-order", r.Effects.AnalysisId);
        Assert.Equal("M", r.Effects.MemberId);
        Assert.Equal(DesignEffectOrigin.FemAnalysis, r.Effects.Origin);
        Assert.Equal(DesignEffectTargetKind.LoadCase, r.Effects.TargetKind);
        Assert.Equal(DesignEffectSectionSide.Continuous, r.Effects.SectionSide);
        Assert.Null(r.Effects.DeflectionMetres); // No deflection is silently inferred.
    }

    [Fact]
    public void ManualSignedCombination_IsTransferredFromOneAnalyzedTargetWithoutEnvelopeMixing()
    {
        // Simply supported 4 m span; G=-4, Q=+2 kN/m, U=1.5G-0.5Q -> q=-7.
        StructuralModel model = Beam()
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("g", "G", "M", StructuralLoadDirection.GlobalY, -4))
            .AddLoad(StructuralLoad.UniformDistributedLoad("q", "Q", "M", StructuralLoadDirection.GlobalY, 2))
            .AddLoadCombination(new StructuralLoadCombination("U", "Manual",
                new[] { new StructuralLoadCombinationTerm("G", 1.5), new StructuralLoadCombinationTerm("Q", -0.5) }));
        DesignCheckRequest r = Capture(model, "U", 1.3, StructuralAnalysisTargetKind.LoadCombination);
        Assert.Equal(0, r.Effects.AxialForceKn!.Value, 7);
        Assert.Equal(14 - 7 * 1.3, r.Effects.ShearForceYKn!.Value, 7);
        Assert.Equal(14 * 1.3 - 7 * 1.3 * 1.3 / 2, r.Effects.BendingMomentZKnm!.Value, 7);
        Assert.Equal(DesignEffectTargetKind.LoadCombination, r.Effects.TargetKind);
        Assert.Equal("U", r.Effects.CombinationId);
    }

    [Fact]
    public void CompressionSign_IsNotReversedOrReplacedByAbsoluteValue()
    {
        // Axial force at B towards A produces negative local compression at any station.
        StructuralModel model = Beam()
            .AddSupport(StructuralSupport.Fixed("S", "A"))
            .AddLoad(StructuralLoad.NodalForce("N", "G", "B", StructuralLoadDirection.GlobalX, -20));
        DesignCheckRequest r = Capture(model, "G", 2.31);
        Assert.Equal(-20, r.Effects.AxialForceKn!.Value, 8);
        Assert.Equal(0, r.Effects.ShearForceYKn!.Value, 8);
        Assert.Equal(0, r.Effects.BendingMomentZKnm!.Value, 8);
    }

    [Fact]
    public void MemberPointLoad_MustSelectSide_AndNeverAveragesShearJump()
    {
        // Midspan P=12 kN downward -> reactions = 6, Vleft=+6, Vright=-6, M=+12.
        StructuralModel model = Beam()
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.PointLoadOnMember("P", "G", "M", StructuralLoadDirection.GlobalY, -12, 0.5));
        Assert.Throws<ArgumentException>(() => Capture(model, "G", 2));
        DesignCheckRequest before = Capture(model, "G", 2, side: SectionLimitM78.BeforePointLoad);
        DesignCheckRequest after = Capture(model, "G", 2, side: SectionLimitM78.AfterPointLoad);
        Assert.Equal(6, before.Effects.ShearForceYKn!.Value, 8);
        Assert.Equal(-6, after.Effects.ShearForceYKn!.Value, 8);
        Assert.Equal(12, before.Effects.BendingMomentZKnm!.Value, 8);
        Assert.Equal(12, after.Effects.BendingMomentZKnm!.Value, 8);
        Assert.Equal(DesignEffectSectionSide.BeforePointLoad, before.Effects.SectionSide);
        Assert.Equal(DesignEffectSectionSide.AfterPointLoad, after.Effects.SectionSide);
        Assert.Throws<ArgumentException>(() => Capture(model, "G", 1, side: SectionLimitM78.BeforePointLoad));
    }

    [Fact]
    public void SectionsAndProvenanceNeverSilentlyMismatched()
    {
        StructuralModel model = Beam()
            .AddSupport(StructuralSupport.Fixed("S", "A"))
            .AddLoad(StructuralLoad.NodalForce("P", "G", "B", StructuralLoadDirection.GlobalY, -10));
        var bridge = new FemStationVerificationBridgeM87B();
        DesignCheckRequest Make(DesignMemberContext member, StructuralAnalysisTargetKind kind, string id) =>
            bridge.Capture(model, member, DesignLimitState.Ultimate, TestProfile,
                Array.Empty<string>(), new(kind, id, 2));

        Assert.Throws<ArgumentException>(() => Make(Member() with { SectionId = "WRONG" }, StructuralAnalysisTargetKind.LoadCase, "G"));
        Assert.Throws<ArgumentException>(() => Make(Member() with { MemberLengthMetres = 4.1 }, StructuralAnalysisTargetKind.LoadCase, "G"));
        Assert.Throws<ArgumentException>(() => Make(Member(), StructuralAnalysisTargetKind.LoadCombination, "G"));
        Assert.Throws<ArgumentException>(() => Make(Member(), StructuralAnalysisTargetKind.LoadCase, "UNKNOWN"));
        Assert.Throws<ArgumentOutOfRangeException>(() => bridge.Capture(model, Member(), DesignLimitState.Ultimate,
            TestProfile, Array.Empty<string>(), new(StructuralAnalysisTargetKind.LoadCase, "G", 5)));
        model.AddLoadCombination(new StructuralLoadCombination("G", "Colliding", new[] { new StructuralLoadCombinationTerm("Q", 1) }));
        Assert.Throws<ArgumentException>(() => Make(Member(), StructuralAnalysisTargetKind.LoadCase, "G"));
    }

    [Fact]
    public void NoNormativeRulesMeansIncomplete_NotCodeApproved()
    {
        StructuralModel model = Beam()
            .AddSupport(StructuralSupport.Fixed("S", "A"))
            .AddLoad(StructuralLoad.NodalForce("P", "G", "B", StructuralLoadDirection.GlobalY, -10));
        DesignCheckReport report = new FemStationVerificationBridgeM87B().Evaluate(model, Member(),
            DesignLimitState.Ultimate, TestProfile, new[] { "EC3-NOT-IMPLEMENTED" },
            new(StructuralAnalysisTargetKind.LoadCase, "G", 2),
            new DesignCheckEngine(Array.Empty<IDesignCheckRule>()));
        Assert.Equal(DesignCheckStatus.Unsupported, Assert.Single(report.Results).Status);
        Assert.Equal(DesignOverallStatus.Incomplete, report.OverallStatus);
        Assert.False(report.IsComplete);
    }

    [Fact]
    public void RuleEngineRejectsForgedMemberProvenance()
    {
        StructuralModel model = Beam()
            .AddSupport(StructuralSupport.Fixed("S", "A"))
            .AddLoad(StructuralLoad.NodalForce("P", "G", "B", StructuralLoadDirection.GlobalY, -10));
        DesignCheckRequest request = Capture(model, "G", 1) with
        {
            Effects = Capture(model, "G", 1).Effects with { MemberId = "ANOTHER" }
        };
        // A synthetic rule would be refused before evaluation because member IDs disagree.
        DesignCheckReport report = new DesignCheckEngine(new[] { new SyntheticRule() }).Evaluate(
            request with { RuleIds = new[] { "SYNTHETIC-NONNORMATIVE" } });
        Assert.Equal(DesignCheckStatus.InvalidInput, Assert.Single(report.Results).Status);
    }

    private sealed class SyntheticRule : IDesignCheckRule
    {
        public string RuleId => "SYNTHETIC-NONNORMATIVE";
        public DesignCodeEdition Code => TestProfile.Code;
        public DesignMaterial Material => DesignMaterial.Steel;
        public DesignLimitState LimitState => DesignLimitState.Ultimate;
        public string SourceClause => "TEST ONLY";
        public DesignRuleEvaluation Evaluate(DesignCheckRequest _) =>
            throw new InvalidOperationException("Invalid data must not reach this rule.");
    }
}
