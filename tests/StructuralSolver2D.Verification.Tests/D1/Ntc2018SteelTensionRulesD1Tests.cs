using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Rules;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D1;

public sealed class Ntc2018SteelTensionRulesD1Tests
{
    private static readonly CommercialRolledSteelCatalogM88B2 Sections =
        CommercialRolledSteelCatalogM88B2.Load(Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));

    // IPE-200 OPPO: A=28.48 cm2=2848 mm2; S235 t<=16: fy=235 MPa.
    // Npl = 2848*235/(1.05*1000) = 637.4095238095 kN.
    private const double GrossKn = 2848d * 235 / 1.05 / 1000d;
    // Designer-evidenced hypothetical bolt-hole path, Anet=2400 mm2, ftk=360 MPa.
    private const double NetKn = 0.9 * 2400 * 360 / 1.25 / 1000d;

    private static Ntc2018SteelTensionEvidenceD1 Evidence(bool holes = true) => new(
        "M1", "IPE-200", "S235", "A1", "ULS-1",
        holes ? Ntc2018SteelHoleStateD1.RelevantConnectionHoles : Ntc2018SteelHoleStateD1.NoRelevantHoles,
        "Independent connection/shop drawing declares the hole pattern",
        "Section identified on issued drawing; OPPO dimensions only illustrative",
        "S235 declared via product document, catalog thickness band cross-checked",
        ConcentricTensionConfirmed: true,
        OutOfPlaneAndTorsionExcludedConfirmed: true,
        VerifiedNetAreaMm2: holes ? 2400 : null,
        NetAreaEvidence: holes ? "Net ligament computed independently from connection drawing" : null,
        CharacteristicUltimateStrengthMpa: holes ? 360 : null,
        UltimateStrengthEvidence: holes ? "S235 ftk=360 MPa independent material specification" : null,
        CapacityHierarchyRequired: holes,
        CapacityHierarchyApplicabilityEvidence: "Engineer documents applicability for this independent test");

    private static DesignCheckRequest Request(double n = 100, double v = 0, double m = 0,
        string section = "IPE-200", string member = "M1", NormativeProfile? profile = null,
        string analysis = "A1", string combination = "ULS-1") => new(
        new DesignMemberContext(member, DesignMaterial.Steel, section, 4),
        DesignLimitState.Ultimate, profile ?? Ntc2018SteelTensionProfileD1.Create(),
        new DesignEffectSnapshot(analysis, combination, DesignEffectOrigin.FemAnalysis, 2, n, v, m)
        { MemberId = member, TargetKind = DesignEffectTargetKind.LoadCombination,
            SectionSide = DesignEffectSectionSide.Continuous },
        new[] { Ntc2018SteelPureTensionResistanceRuleD1.Id });

    private static IDesignCheckRule Rule(bool hierarchy, Ntc2018SteelTensionEvidenceD1 evidence) =>
        hierarchy ? new Ntc2018SteelTensionCapacityHierarchyRuleD1(Sections, Materials, new[] { evidence }) :
            new Ntc2018SteelPureTensionResistanceRuleD1(Sections, Materials, new[] { evidence });

    private static DesignCheckResult Check(DesignCheckRequest request,
        Ntc2018SteelTensionEvidenceD1? evidence = null, bool hierarchy = false)
    {
        var rule = Rule(hierarchy, evidence ?? Evidence());
        return Assert.Single(new DesignCheckEngine(new[] { rule }).Evaluate(
            request with { RuleIds = new[] { rule.RuleId } }).Results);
    }

    [Fact]
    public void NativeEditionAndPartialFactorsAreLockedAndDifferentFromEc3()
    {
        var p = Ntc2018SteelTensionProfileD1.Create();
        Assert.Equal("DM 17 gennaio 2018", p.Code.DocumentId);
        Assert.Equal("NTC2018", p.Code.EditionId);
        Assert.Equal(1.05, Assert.Single(p.Coefficients, c => c.Id == "gamma_M0").Value);
        Assert.Equal(1.05, Assert.Single(p.Coefficients, c => c.Id == "gamma_M1").Value);
        Assert.Equal(1.25, Assert.Single(p.Coefficients, c => c.Id == "gamma_M2").Value);
        Assert.NotEqual("EN 1993-1-1", p.Code.DocumentId);
    }

    [Fact]
    public void GrossUnweakened_PassesUsingNtcGamma105()
    {
        var result = Check(Request(630), Evidence(holes: false));
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.Equal(GrossKn, result.Resistance!.Value, 9);
        Assert.Equal(630 / GrossKn, result.Utilization!.Value, 11);
        Assert.Contains("GROSS_YIELD", result.CalculationTrace!);
        Assert.Contains("holes=NONE CONFIRMED", result.CalculationTrace!);
        Assert.Contains("NON CERTIFICATO", result.CalculationTrace!);
    }

    [Fact]
    public void RelevantHoles_GrossPassButNetRuptureFails()
    {
        Assert.True(630 < GrossKn);
        Assert.True(630 > NetKn);
        var result = Check(Request(630));
        Assert.Equal(DesignCheckStatus.Failed, result.Status);
        Assert.Equal(NetKn, result.Resistance!.Value, 9);
        Assert.Equal(630 / NetKn, result.Utilization!.Value, 11);
        Assert.Contains("NET_RUPTURE", result.CalculationTrace!);
        Assert.Contains("netEvidence=", result.CalculationTrace!);
        Assert.Contains("ftkEvidence=", result.CalculationTrace!);
    }

    [Fact]
    public void RelevantHoles_GrossControlsWhenNetHasGreaterResistance()
    {
        var declared = Evidence() with { VerifiedNetAreaMm2 = 2700 };
        const double expectedNet = 0.9 * 2700 * 360 / 1.25 / 1000;
        Assert.True(expectedNet > GrossKn);
        var result = Check(Request(630), declared);
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.Equal(GrossKn, result.Resistance!.Value, 9);
        Assert.Contains("GROSS_YIELD", result.CalculationTrace!);
    }

    [Theory]
    [InlineData(0, DesignCheckStatus.Passed)]
    [InlineData(1e-10, DesignCheckStatus.Failed)]
    public void NetResistance_ExactBoundaryAndExcess(double delta, DesignCheckStatus expected)
    {
        var result = Check(Request(NetKn + delta));
        Assert.Equal(expected, result.Status);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0.00001, 0)]
    [InlineData(1, 0, 0.00001)]
    public void MixedOrNonTensileActionsNeverUsePureTension(double n, double v, double m)
    {
        Assert.Equal(DesignCheckStatus.NotApplicable, Check(Request(n, v, m)).Status);
    }

    [Fact]
    public void RelevantHoles_RequireAreaStrengthAndEvidence()
    {
        var e = Evidence();
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { VerifiedNetAreaMm2 = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { VerifiedNetAreaMm2 = -4 }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { VerifiedNetAreaMm2 = 2848 }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { VerifiedNetAreaMm2 = double.NaN }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { NetAreaEvidence = "" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { CharacteristicUltimateStrengthMpa = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { CharacteristicUltimateStrengthMpa = 235 }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { CharacteristicUltimateStrengthMpa = double.PositiveInfinity }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { UltimateStrengthEvidence = "" }).Status);
    }

    [Fact]
    public void HoleBranchUnknownAndContradictoryInputsFailClosed()
    {
        var e = Evidence();
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { HoleState = Ntc2018SteelHoleStateD1.Unknown }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { HoleStateEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { HoleState = Ntc2018SteelHoleStateD1.NoRelevantHoles }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), Evidence(false) with { VerifiedNetAreaMm2 = 2700 }).Status);
    }

    [Fact]
    public void PhysicalEvidenceOrWrongSectionMustNotPass()
    {
        var e = Evidence();
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { SectionGeometryEvidence = "" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { MaterialYieldEvidence = "" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { ConcentricTensionConfirmed = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { OutOfPlaneAndTorsionExcludedConfirmed = false }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(section: "HEA-200")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { SteelGrade = "S999" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(), e with { SectionId = "IPE-999" }).Status);
    }

    [Fact]
    public void EvidenceIsNotTransferableAcrossAnalysesAndCombinations()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(analysis: "A2")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(combination: "ULS-2")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(member: "M2")).Status);
    }

    [Fact]
    public void MissingOrIncorrectFemProvenancePreventsCalculation()
    {
        var r = Request();
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { Origin = DesignEffectOrigin.ExternalManual } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { SectionSide = null } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { TargetKind = DesignEffectTargetKind.LoadCase } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { MemberId = null } }).Status);
    }

    [Fact]
    public void WrongEditionCannotBePresentedAsNtc2018()
    {
        var p = Ntc2018SteelTensionProfileD1.Create();
        var wrong = p with { Code = new DesignCodeEdition("EN 1993-1-1", "2005+A1:2014", "IT") };
        Assert.Equal(DesignCheckStatus.Unsupported, Check(Request(profile: wrong)).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(profile: p with { ProfileId = "CUSTOM" })).Status);
    }

    [Fact]
    public void ChangingGammaOrItsSourceIsNotAccepted()
    {
        var p = Ntc2018SteelTensionProfileD1.Create();
        var modified = p.Coefficients.Select(c => c.Id == "gamma_M0" ? c with { Value = 1 } : c).ToArray();
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(profile: p with { Coefficients = modified })).Status);
        modified = p.Coefficients.Select(c => c.Id == "gamma_M2" ? c with { SourceClause = "SOURCE UNKNOWN" } : c).ToArray();
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(profile: p with { Coefficients = modified })).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(profile: p with { Coefficients = Array.Empty<NormativeCoefficient>() })).Status);
    }

    [Fact]
    public void HierarchyCanFailEvenWhenAppliedForcePasses()
    {
        var r = Check(Request(n: 100), hierarchy: true);
        Assert.Equal(DesignCheckStatus.Failed, r.Status);
        Assert.Equal(GrossKn, r.Demand!.Value, 9);
        Assert.Equal(NetKn, r.Resistance!.Value, 9);
        Assert.Contains("Capacity hierarchy REQUIRED", r.CalculationTrace!);
    }

    [Fact]
    public void HierarchyPassesWhenNetRuptureExceedsGrossYield()
    {
        var e = Evidence() with { VerifiedNetAreaMm2 = 2700 };
        var r = Check(Request(n: 100), e, hierarchy: true);
        Assert.Equal(DesignCheckStatus.Passed, r.Status);
    }

    [Fact]
    public void HierarchyExclusionAndUnknownApplicabilityAreDistinct()
    {
        var e = Evidence();
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(Request(), e with { CapacityHierarchyRequired = false }, hierarchy: true).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(), e with { CapacityHierarchyRequired = null }, hierarchy: true).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(), e with { CapacityHierarchyApplicabilityEvidence = "" }, hierarchy: true).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(), Evidence(holes:false) with { CapacityHierarchyRequired = true }, hierarchy: true).Status);
    }

    [Fact]
    public void IsolatedPassDoesNotApproveWholeMemberAndUnknownRulesRemainUnsupported()
    {
        var e = Evidence(holes:false);
        var rule = Rule(false, e);
        Assert.False(((Ntc2018SteelTensionRuleD1)rule).EstablishesCompleteMemberCompliance);
        var r = Request(100) with { RuleIds = new[] { rule.RuleId, "NTC2018-OUT-OF-PLANE-BUCKLING-NOT-IMPLEMENTED" } };
        var report = new DesignCheckEngine(new[] { rule }).Evaluate(r);
        Assert.Equal(DesignCheckStatus.Passed, report.Results[0].Status);
        Assert.Equal(DesignCheckStatus.Unsupported, report.Results[1].Status);
        Assert.Equal(DesignOverallStatus.Incomplete, report.OverallStatus);
        Assert.False(report.IsComplete);
    }
}
