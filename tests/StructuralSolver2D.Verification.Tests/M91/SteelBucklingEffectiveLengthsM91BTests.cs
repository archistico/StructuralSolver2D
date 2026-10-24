using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.M91;

public sealed class SteelBucklingEffectiveLengthsM91BTests
{
    private const string EndProof = "Isolated pinned-end idealization documented by structural engineer; no global sidesway";
    private const string BraceProof = "Independent bracing verification report B-7: stiffness, strength and anchors";

    private static SteelAxisRestraintPlanM91B Strong => new(
        "y-y", 7.5, "Drawing COLUMN-7: geometric span 7.50m",
        SteelEffectiveLengthMethodM91B.PinnedPinned, true, EndProof);

    private static SteelAxisRestraintPlanM91B Weak => new(
        "z-z", 7.5, "Drawing COLUMN-7: geometric span 7.50m",
        SteelEffectiveLengthMethodM91B.PinnedPinned, true, EndProof,
        new [] { new SteelIntermediateBraceM91B(3.75, true, true, true, BraceProof) });

    private static SteelMemberRestraintPlanM91B Plan => new(
        "COL1", Strong, Weak, true, "Drawing SECTION-1: principal y-y/z-z orientation confirmed",
        true, "Prismatic HEB200, uniform concentric compression explicitly assessed");

    private static SteelFlexuralBucklingAssignmentM91A Original => new(
        "COL1", "HEB-200", "S235", 1,
        "Bernuzzi A7.1 verified class 1", null, null, null, null,
        210000, "Material characteristic table / EC3 steel E", true, true, true, true);

    private static DesignCheckRequest Request => new(
        new DesignMemberContext("COL1", DesignMaterial.Steel, "HEB-200", 7.5),
        DesignLimitState.Ultimate,
        new NormativeProfile("TEST-M91B-NO-COMPLIANCE", new DesignCodeEdition(
            "EN 1993-1-1", "2005+A1:2014", "IT-TEST-ANNEX-EXPLICIT"),
            new[] { new NormativeCoefficient("gamma_M1", 1.05,
                "Bernuzzi A7.1 test value; not National Annex approval", "partial factor") }),
        new DesignEffectSnapshot("FEM-1", "ULS-1", DesignEffectOrigin.FemAnalysis,
            3.75, -1100, 0, 0) { MemberId="COL1", TargetKind=DesignEffectTargetKind.LoadCombination,
                SectionSide=DesignEffectSectionSide.Continuous },
        new [] {SteelFlexuralBucklingRuleM91A.Id});

    [Fact]
    public void BernuzziA7_1_MidHeightWeakAxisBrace_PreparesOriginalM91A_AndMatchesResistance()
    {
        var resolved = SteelBucklingEffectiveLengthsM91B.ResolveMember(Plan);
        Assert.Equal(SteelLengthResolutionStatusM91B.Resolved, resolved.Status);
        Assert.Equal(7.5, resolved.StrongAxis.EffectiveLengthMetres!.Value);
        Assert.Equal(3.75, resolved.WeakAxis.EffectiveLengthMetres!.Value);
        Assert.Equal(new [] {3.75, 3.75}, resolved.WeakAxis.IdealizedSegmentsMetres);
        var prepared = SteelBucklingEffectiveLengthsM91B.PrepareAssignment(Plan, Original);
        Assert.Equal(SteelLengthResolutionStatusM91B.Resolved, prepared.Status);
        Assert.NotNull(prepared.Assignment);
        Assert.Contains(BraceProof, prepared.Assignment!.WeakAxisLengthEvidence!);
        Assert.Contains("NOT a calculation", prepared.Assignment.WeakAxisLengthEvidence!);
        var code = Request.Profile.Code;
        var sections = CommercialRolledSteelCatalogM88B2.Load(
            Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
        var materials = CharacteristicMaterialCatalogM88C.Load(
            Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));
        var engine = new DesignCheckEngine(new [] {
            new SteelFlexuralBucklingRuleM91A(code, sections, materials,
                new [] {prepared.Assignment!})
        });
        var result = Assert.Single(engine.Evaluate(Request).Results);
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.InRange(result.Resistance!.Value, 1115.5, 1116.6);
        Assert.Contains("governing=y-y", result.CalculationTrace!);
    }

    [Fact]
    public void NoBraceMeansNoSilentReductionOfWeakAxisLength()
    {
        var plan = Plan with { WeakAxis = Weak with {IntermediateBraces = null} };
        var result = SteelBucklingEffectiveLengthsM91B.ResolveMember(plan);
        Assert.Equal(SteelLengthResolutionStatusM91B.Resolved, result.Status);
        Assert.Equal(7.5, result.WeakAxis.EffectiveLengthMetres!.Value);
    }

    [Theory]
    [InlineData(2.5, 5.0)]
    [InlineData(3.75, 3.75)]
    [InlineData(5.0, 5.0)]
    public void SingleBrace_LongestPinnedIntervalGoverns(double location, double expected)
    {
        var weak = Weak with {IntermediateBraces = new[] {
            new SteelIntermediateBraceM91B(location, true, true, true, BraceProof)} };
        var result = SteelBucklingEffectiveLengthsM91B.ResolveAxis(weak);
        Assert.Equal(SteelLengthResolutionStatusM91B.Resolved, result.Status);
        Assert.Equal(expected, result.EffectiveLengthMetres!.Value);
        Assert.Equal(expected / 7.5, result.EffectiveLengthFactor!.Value);
    }

    [Fact]
    public void UnsortedBracePositions_AreSortedAndEveryIntervalIsRetained()
    {
        var weak = Weak with { IntermediateBraces = new[] {
            new SteelIntermediateBraceM91B(5.0, true, true, true, "B2 external proof"),
            new SteelIntermediateBraceM91B(2.0, true, true, true, "B1 external proof") } };
        var result = SteelBucklingEffectiveLengthsM91B.ResolveAxis(weak);
        Assert.Equal(SteelLengthResolutionStatusM91B.Resolved, result.Status);
        Assert.Equal(new [] {2.0,3.0,2.5}, result.IdealizedSegmentsMetres);
        Assert.Equal(3.0, result.EffectiveLengthMetres!.Value);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void WeakOrUnverifiedBrace_CannotReduceBucklingLength(bool translation, bool stiffness, bool strength)
    {
        var plan = Plan with { WeakAxis = Weak with { IntermediateBraces = new[] {
            new SteelIntermediateBraceM91B(3.75, translation, stiffness, strength, BraceProof)} } };
        var result = SteelBucklingEffectiveLengthsM91B.PrepareAssignment(plan, Original);
        Assert.Equal(SteelLengthResolutionStatusM91B.MissingData, result.Status);
        Assert.Null(result.Assignment);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7.5)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void NonInteriorOrNonFiniteBraces_AreRejected(double pos)
    {
        var weak = Weak with { IntermediateBraces = new[] {
            new SteelIntermediateBraceM91B(pos, true, true, true, BraceProof)} };
        Assert.Equal(SteelLengthResolutionStatusM91B.InvalidInput,
            SteelBucklingEffectiveLengthsM91B.ResolveAxis(weak).Status);
    }

    [Fact]
    public void DuplicateAndAlmostCoincidentBraces_AreRejected()
    {
        foreach (var offset in new [] {0d, 1e-12})
        {
            var weak = Weak with { IntermediateBraces = new [] {
                new SteelIntermediateBraceM91B(3.75, true, true, true, "B1"),
                new SteelIntermediateBraceM91B(3.75 + offset, true, true, true, "B2") } };
            Assert.Equal(SteelLengthResolutionStatusM91B.InvalidInput,
                SteelBucklingEffectiveLengthsM91B.ResolveAxis(weak).Status);
        }
    }

    [Fact]
    public void MissingProofsAndUnconfirmedEnds_AreNotPermitted()
    {
        Assert.Equal(SteelLengthResolutionStatusM91B.MissingData,
            SteelBucklingEffectiveLengthsM91B.ResolveAxis(Weak with {EndRestraintsVerifiedExternally=null}).Status);
        Assert.Equal(SteelLengthResolutionStatusM91B.MissingData,
            SteelBucklingEffectiveLengthsM91B.ResolveAxis(Weak with {EndRestraintEvidence=""}).Status);
        Assert.Equal(SteelLengthResolutionStatusM91B.MissingData,
            SteelBucklingEffectiveLengthsM91B.ResolveAxis(Weak with {PhysicalLengthEvidence=""}).Status);
        Assert.Equal(SteelLengthResolutionStatusM91B.MissingData,
            SteelBucklingEffectiveLengthsM91B.ResolveAxis(Weak with {IntermediateBraces=new[] {
                new SteelIntermediateBraceM91B(3.75, true, true, true, "")}}).Status);
    }

    [Fact]
    public void Cantilever_UsesTwoTimesLengthOnlyWithExternallyConfirmedIdealEnds()
    {
        var axis = Strong with {Method=SteelEffectiveLengthMethodM91B.FixedFreeCantilever,
            EndRestraintEvidence="Fixed base and free head; structural drawing verified"};
        var result = SteelBucklingEffectiveLengthsM91B.ResolveAxis(axis);
        Assert.Equal(SteelLengthResolutionStatusM91B.Resolved, result.Status);
        Assert.Equal(15.0, result.EffectiveLengthMetres!.Value);
        Assert.Equal(2.0, result.EffectiveLengthFactor!.Value);
        var withBrace = axis with {IntermediateBraces = Weak.IntermediateBraces};
        Assert.Equal(SteelLengthResolutionStatusM91B.Unsupported,
            SteelBucklingEffectiveLengthsM91B.ResolveAxis(withBrace).Status);
    }

    [Fact]
    public void IndependentlyCalculatedLcr_WithSourceIsUsedWithoutGuessingCoefficient()
    {
        var axis = Strong with {Method=SteelEffectiveLengthMethodM91B.IndependentStabilityAnalysis,
            IndependentEffectiveLengthMetres=6.0,
            IndependentStabilityEvidence="Frame eigenvalue report R-12, load combination ULS1"};
        var result = SteelBucklingEffectiveLengthsM91B.ResolveAxis(axis);
        Assert.Equal(SteelLengthResolutionStatusM91B.Resolved, result.Status);
        Assert.Equal(6.0, result.EffectiveLengthMetres!.Value);
        Assert.Equal(0.8, result.EffectiveLengthFactor!.Value, 12);
        Assert.Contains("R-12", result.Evidence);
        Assert.Empty(result.IdealizedSegmentsMetres);
        Assert.Equal(SteelLengthResolutionStatusM91B.MissingData,
            SteelBucklingEffectiveLengthsM91B.ResolveAxis(axis with {IndependentStabilityEvidence=null}).Status);
    }

    [Fact]
    public void CannotMixAxesOrDifferentMemberGeometry()
    {
        Assert.Equal(SteelLengthResolutionStatusM91B.InvalidInput,
            SteelBucklingEffectiveLengthsM91B.ResolveMember(Plan with {WeakAxis=Weak with {Axis="y-y"}}).Status);
        Assert.Equal(SteelLengthResolutionStatusM91B.InvalidInput,
            SteelBucklingEffectiveLengthsM91B.ResolveMember(Plan with {WeakAxis=Weak with {PhysicalLengthMetres=8.0}}).Status);
        Assert.Equal(SteelLengthResolutionStatusM91B.InvalidInput,
            SteelBucklingEffectiveLengthsM91B.PrepareAssignment(Plan with {MemberId="OTHER"}, Original).Status);
    }

    [Fact]
    public void PrincipalAxesAndConcentricAssumptionsMustBeDocumented()
    {
        Assert.Equal(SteelLengthResolutionStatusM91B.MissingData,
            SteelBucklingEffectiveLengthsM91B.ResolveMember(Plan with {PrincipalAxisMappingConfirmed=false}).Status);
        Assert.Equal(SteelLengthResolutionStatusM91B.MissingData,
            SteelBucklingEffectiveLengthsM91B.ResolveMember(Plan with {UniformPrismaticConcentricCompressionConfirmed=null}).Status);
        Assert.Equal(SteelLengthResolutionStatusM91B.MissingData,
            SteelBucklingEffectiveLengthsM91B.ResolveMember(Plan with {PrincipalAxisMappingEvidence=""}).Status);
    }

    [Fact]
    public void UnmodelledMixedOrSwayFrames_RequireIndependentAnalysis_NotUnsupportedAutoK()
    {
        var axis = Strong with {Method=(SteelEffectiveLengthMethodM91B)999};
        var result = SteelBucklingEffectiveLengthsM91B.ResolveAxis(axis);
        Assert.Equal(SteelLengthResolutionStatusM91B.Unsupported, result.Status);
        Assert.Null(result.EffectiveLengthMetres);
        Assert.Equal(SteelLengthResolutionStatusM91B.Unsupported,
            SteelBucklingEffectiveLengthsM91B.PrepareAssignment(Plan with {StrongAxis=axis}, Original).Status);
    }

    [Fact]
    public void InvalidNumericInputsDoNotGenerateFiniteDesignLengths()
    {
        Assert.Equal(SteelLengthResolutionStatusM91B.InvalidInput,
            SteelBucklingEffectiveLengthsM91B.ResolveAxis(Strong with {PhysicalLengthMetres=double.NaN}).Status);
        Assert.Equal(SteelLengthResolutionStatusM91B.InvalidInput,
            SteelBucklingEffectiveLengthsM91B.ResolveAxis(Strong with {PhysicalLengthMetres=0}).Status);
        Assert.Equal(SteelLengthResolutionStatusM91B.InvalidInput,
            SteelBucklingEffectiveLengthsM91B.ResolveAxis(Strong with {
                Method=SteelEffectiveLengthMethodM91B.FixedFreeCantilever,
                PhysicalLengthMetres=1e308}).Status);
        Assert.Equal(SteelLengthResolutionStatusM91B.MissingData,
            SteelBucklingEffectiveLengthsM91B.ResolveAxis(Strong with {
                Method=SteelEffectiveLengthMethodM91B.IndependentStabilityAnalysis,
                IndependentEffectiveLengthMetres=double.PositiveInfinity}).Status);
    }

    [Fact]
    public void BraceSplittingHalvesLcr_AndQuadruplesEulerNcrForSameAxis()
    {
        var full = SteelFlexuralBucklingRuleM91A.CalculateAxis("z-z", "c", 7.5, 2003, 78.08,235,210000,1.05)!;
        var braced = SteelFlexuralBucklingRuleM91A.CalculateAxis("z-z", "c", 3.75, 2003, 78.08,235,210000,1.05)!;
        Assert.Equal(4, braced.EulerCriticalForceKn/full.EulerCriticalForceKn, 12);
        Assert.True(braced.BucklingResistanceKn > full.BucklingResistanceKn);
    }

    [Fact]
    public void AResolvedLengthIsNotAnAutomaticOverallCodeApproval()
    {
        var prepared = SteelBucklingEffectiveLengthsM91B.PrepareAssignment(Plan, Original);
        Assert.NotNull(prepared.Assignment);
        Assert.Contains("NOT a calculation", prepared.Assignment!.WeakAxisLengthEvidence!);
        Assert.Contains("externally verified", prepared.Assignment.WeakAxisLengthEvidence!);
        Assert.Equal(Original.CompressionSectionClass, prepared.Assignment.CompressionSectionClass);
        Assert.Equal(Original.YoungModulusMpa, prepared.Assignment.YoungModulusMpa);
    }
}
