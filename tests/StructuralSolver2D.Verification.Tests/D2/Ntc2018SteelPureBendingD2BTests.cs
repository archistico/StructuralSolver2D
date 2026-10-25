using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D2;

public sealed class Ntc2018SteelPureBendingD2BTests
{
    private static readonly CommercialRolledSteelCatalogM88B2 Sections =
        CommercialRolledSteelCatalogM88B2.Load(Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));

    // STAD's independent published 13-Oct-2026 IPE table: IPE 200 Wpl,y=220.64 cm3,
    // Wel,y=194.30 cm3. Both indicative nominal values, NOT a product certificate.
    // https://www.stad.fr/downloads/pdf/stad-technical-data-of-sections-ipe.pdf
    private const double WplIpe200Mm3 = 220_640d;
    private const double MplIpe200S235Knm = WplIpe200Mm3 * 235d / 1.05d / 1_000_000d;

    private static Ntc2018SteelPureBendingEvidenceD2B Evidence() => new(
        "M1", "IPE-200", "S235", "FEM-1", "SLU-STR-A1", 1.75,
        DesignEffectSectionSide.Continuous,
        Ntc2018SteelHoleStateD1.NoRelevantHoles,
        "No relevant holes at this IPE-200 station; checked against member schedule",
        HotRolledParallelFlangeAndDoublySymmetricConfirmed: true,
        FemLocalMzMatchesStrongAxisConfirmed: true,
        OtherSpatialActionsAndTorsionExcludedConfirmed: true,
        SectionGeometryEvidence: "IPE 200 hot-rolled parallel-flange and dimensions from the independent schedule",
        MaterialYieldEvidence: "Declared S235, grade/yield band independently cross-checked",
        PureActionEvidence: "Pure strong-axis bending, signed FEM Mz with N=Vy=0",
        ElasticModulusEvidence: "STAD supplier IPE 200 table 13-Oct-2026, Wel,y=194.30 cm3",
        DeclaredStrongElasticModulusMm3: 194_300d,
        PlasticModulusEvidence: "STAD supplier IPE 200 table 13-Oct-2026, Wpl,y=220.64 cm3",
        DeclaredStrongPlasticModulusMm3: WplIpe200Mm3);

    private static DesignCheckRequest Request(double n = 0, double v = 0, double m = 40,
        string section = "IPE-200", string member = "M1", string analysis = "FEM-1",
        string combination = "SLU-STR-A1", double x = 1.75,
        DesignEffectSectionSide? side = DesignEffectSectionSide.Continuous,
        NormativeProfile? profile = null) => new(
        new DesignMemberContext(member, DesignMaterial.Steel, section, 4),
        DesignLimitState.Ultimate, profile ?? Ntc2018SteelTensionProfileD1.Create(),
        new DesignEffectSnapshot(analysis, combination, DesignEffectOrigin.FemAnalysis,
            x, n, v, m)
        {
            MemberId = member, TargetKind = DesignEffectTargetKind.LoadCombination,
            SectionSide = side
        }, new[] { Ntc2018SteelPureBendingResistanceRuleD2B.Id });

    private static DesignCheckResult Check(DesignCheckRequest? request = null,
        Ntc2018SteelPureBendingEvidenceD2B? evidence = null,
        CommercialRolledSteelCatalogM88B2? sections = null)
    {
        var rule = new Ntc2018SteelPureBendingResistanceRuleD2B(
            sections ?? Sections, Materials, new[] { evidence ?? Evidence() });
        return Assert.Single(new DesignCheckEngine(new[] { rule }).Evaluate(request ?? Request()).Results);
    }

    [Fact]
    public void ClassOneUsesDocumentedWplNotWel_AndNtcGammaM0()
    {
        var r = Check();
        Assert.Equal(DesignCheckStatus.Passed, r.Status);
        Assert.Equal(40, r.Demand!.Value, 12);
        Assert.Equal(MplIpe200S235Knm, r.Resistance!.Value, 9);
        Assert.Contains("class=1", r.CalculationTrace!);
        Assert.Contains("branch=MplRd-Wpl", r.CalculationTrace!);
        Assert.Contains("Wpl_declared=220640", r.CalculationTrace!);
        Assert.Contains("gammaM0=1.05", r.CalculationTrace!);
        Assert.Contains("NO LATERAL-TORSIONAL BUCKLING", r.CalculationTrace!);
        Assert.Contains("NON CERTIFICATO", r.CalculationTrace!);
        Assert.False(new Ntc2018SteelPureBendingResistanceRuleD2B(Sections, Materials,
            new[] { Evidence() }).EstablishesCompleteMemberCompliance);
    }

    [Fact]
    public void SignedMomentsBothDirectionsAndExactCapacityBoundary()
    {
        Assert.Equal(DesignCheckStatus.Passed, Check(Request(m: -40)).Status);
        Assert.Equal(DesignCheckStatus.Passed, Check(Request(m: MplIpe200S235Knm)).Status);
        Assert.Equal(DesignCheckStatus.Failed, Check(Request(m: MplIpe200S235Knm + 0.0001d)).Status);
        Assert.Equal(DesignCheckStatus.Failed, Check(Request(m: -55)).Status);
        Assert.Equal(40d / MplIpe200S235Knm, Check().Utilization!.Value, 10);
    }

    [Fact]
    public void ClassThreeUsesConservativeWelMinNeverPlasticWpl()
    {
        // D0's published nominal HEA-280 / S355 major-axis classification is 3.
        var e = Evidence() with
        {
            SectionId = "HEA-280", SteelGrade = "S355",
            ElasticModulusEvidence = "HEA-280 independently checked Wel,y=1013 cm3",
            DeclaredStrongElasticModulusMm3 = 1_013_000d,
            PlasticModulusEvidence = null,
            DeclaredStrongPlasticModulusMm3 = null
        };
        var r = Check(Request(section: "HEA-280", m: 100), e);
        Assert.Equal(DesignCheckStatus.Passed, r.Status);
        Assert.Equal(1_013_000d * 355d / 1.05d / 1_000_000d, r.Resistance!.Value, 8);
        Assert.Contains("class=3", r.CalculationTrace!);
        Assert.Contains("branch=MelRd-WelMin", r.CalculationTrace!);
        Assert.Contains("Wpl_declared=not supplied", r.CalculationTrace!);
        var lower = Check(Request(section: "HEA-280", m: 100),
            e with { DeclaredStrongElasticModulusMm3 = 1_012_500d });
        Assert.Equal(1_012_500d * 355d / 1.05d / 1_000_000d, lower.Resistance!.Value, 8);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(section: "HEA-280"), e with { DeclaredStrongElasticModulusMm3 = 1_000_000d }).Status);
    }

    [Fact]
    public void ClassTwoAlsoUsesPlasticModulus()
    {
        // Real HEA-180/S355: flange c/t=7.58, thresholds 9eps~7.32,
        // 10eps~8.14, hence bending class 2 (web remains class 1).
        // ArcelorMittal Orange Book HE 180 A lists Wpl,y=325 cm3, rounded;
        // the stronger OPPO nominal precision supplies Wel=293.6 cm3.
        // https://orangebook.arcelormittal.com/node/16
        var e = Evidence() with
        {
            SectionId = "HEA-180", SteelGrade = "S355",
            ElasticModulusEvidence = "HEA 180 independently documented nominal Wel,y=293.6 cm3",
            DeclaredStrongElasticModulusMm3 = 293_600d,
            PlasticModulusEvidence = "ArcelorMittal Orange Book HE 180 A: rounded Wpl,y=325 cm3",
            DeclaredStrongPlasticModulusMm3 = 325_000d
        };
        var r = Check(Request(section: "HEA-180", m: 1), e);
        Assert.Equal(DesignCheckStatus.Passed, r.Status);
        Assert.Contains("class=2", r.CalculationTrace!);
        Assert.Contains("branch=MplRd-Wpl", r.CalculationTrace!);
        Assert.Equal(325_000d * 355d / 1.05d / 1_000_000d, r.Resistance!.Value, 8);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(section: "HEA-180"),
            e with { DeclaredStrongPlasticModulusMm3 = null }).Status);
    }

    [Fact]
    public void ClassFourDoesNotSubstituteElasticOrPlasticModuli()
    {
        var original = Sections.Find("IPE-270").Definition;
        const double area = 80d;
        const double height = 900d;
        const double ix = 20000d;
        const double iy = 200d;
        var synthetic = original with
        {
            Id = "IPE-CLASS4-BENDING-TEST", Name = "SYNTHETIC class 4 - NOT OPPO",
            HeightMm = height, AreaCm2 = area, MassKgPerM = 0.785d * area,
            StrongInertiaCm4 = ix, WeakInertiaCm4 = iy,
            StrongElasticModulusCm3 = 20d * ix / height,
            WeakElasticModulusCm3 = 20d * iy / original.WidthMm,
            StrongRadiusCm = Math.Sqrt(ix / area), WeakRadiusCm = Math.Sqrt(iy / area)
        };
        var sections = new CommercialRolledSteelCatalogM88B2(
            "TEST ONLY: synthetic slender flange/web", new[] { synthetic });
        var e = Evidence() with
        {
            SectionId = synthetic.Id, SteelGrade = "S355",
            SectionGeometryEvidence = "TEST ONLY: synthetic deep I section",
            DeclaredStrongElasticModulusMm3 = synthetic.StrongElasticModulusCm3 * 1000d,
            DeclaredStrongPlasticModulusMm3 = null
        };
        var r = Check(Request(section: synthetic.Id), e, sections);
        Assert.Equal(DesignCheckStatus.NotApplicable, r.Status);
        Assert.Contains("Class 4", r.Message);
        Assert.Null(r.Resistance);
    }

    [Theory]
    [InlineData(1, 0, 40)]
    [InlineData(-1, 0, 40)]
    [InlineData(0, 0.0001, 40)]
    [InlineData(0, -0.0001, 40)]
    [InlineData(0, 0, 0)]
    public void NonpureBendingAlwaysNotApplicable(double n, double v, double m) =>
        Assert.Equal(DesignCheckStatus.NotApplicable, Check(Request(n: n, v: v, m: m)).Status);

    [Fact]
    public void MissingOrInvalidInputCannotBecomePureBending()
    {
        var r = Request();
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { AxialForceKn = null } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { ShearForceYKn = null } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { BendingMomentZKnm = null } }).Status);
        Assert.Equal(DesignCheckStatus.InvalidInput,
            Check(r with { Effects = r.Effects with { BendingMomentZKnm = double.NaN } }).Status);
        Assert.Equal(DesignCheckStatus.InvalidInput,
            Check(r with { Effects = r.Effects with { BendingMomentZKnm = double.PositiveInfinity } }).Status);
    }

    [Fact]
    public void FemEvidenceIsBoundToExactMemberCombinationStationAndSide()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(member: "M2")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(analysis: "FEM-2")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(combination: "OTHER")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(x: 1.75000001)).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(side: DesignEffectSectionSide.AfterPointLoad)).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(section: "IPE-180")).Status);
        var r = Request();
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { Origin = DesignEffectOrigin.ExternalManual } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { TargetKind = DesignEffectTargetKind.LoadCase } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { SectionSide = null } }).Status);
    }

    [Fact]
    public void MustDocumentWelAndWplAndRejectUnphysicalWpl()
    {
        var e = Evidence();
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredStrongPlasticModulusMm3 = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { PlasticModulusEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredStrongPlasticModulusMm3 = 194_299d }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredStrongPlasticModulusMm3 = 1e9d }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredStrongPlasticModulusMm3 = double.PositiveInfinity }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredStrongElasticModulusMm3 = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredStrongElasticModulusMm3 = 180_000d }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredStrongElasticModulusMm3 = double.NaN }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { ElasticModulusEvidence = " " }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { MaterialYieldEvidence = " " }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { HoleStateEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { HoleState = Ntc2018SteelHoleStateD1.Unknown }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { HoleState = Ntc2018SteelHoleStateD1.RelevantConnectionHoles }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { FemLocalMzMatchesStrongAxisConfirmed = false }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { OtherSpatialActionsAndTorsionExcludedConfirmed = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { HotRolledParallelFlangeAndDoublySymmetricConfirmed = false }).Status);
    }

    [Fact]
    public void UnknownOrTaperedProfilesAndSteelGradesAreNotPassed()
    {
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(Request(section: "INP-200"), Evidence() with { SectionId = "INP-200" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(section: "IPE-999"), Evidence() with { SectionId = "IPE-999" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(evidence: Evidence() with { SteelGrade = "S999" }).Status);
    }

    [Fact]
    public void StrictNtcEditionAndGammaAreEnforced()
    {
        var p = Ntc2018SteelTensionProfileD1.Create();
        Assert.Equal(DesignCheckStatus.Unsupported,
            Check(Request(profile: p with { Code = new DesignCodeEdition("EN 1993-1-1", "2005", "IT") })).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(profile: p with { ProfileId = "ANY-OTHER-PROFILE" })).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(profile: p with { Coefficients = p.Coefficients.Select(c =>
                c.Id == "gamma_M0" ? c with { Value = 1d } : c).ToArray() })).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(profile: p with { Coefficients = p.Coefficients.Select(c =>
                c.Id == "gamma_M1" ? c with { SourceClause = "INVALID" } : c).ToArray() })).Status);
    }

    [Fact]
    public void DuplicateEvidenceRejectedAndIncompleteBucklingObligationRemainsIncomplete()
    {
        var e = Evidence();
        Assert.Throws<ArgumentException>(() => new Ntc2018SteelPureBendingResistanceRuleD2B(
            Sections, Materials, new[] { e, e }));
        Assert.Throws<ArgumentException>(() => new Ntc2018SteelPureBendingResistanceRuleD2B(
            Sections, Materials, new[] { e with { PositionMetres = double.NaN } }));
        var rule = new Ntc2018SteelPureBendingResistanceRuleD2B(Sections, Materials, new[] { e });
        var report = new DesignCheckEngine(new[] { rule }).Evaluate(Request() with
        {
            RuleIds = new[] { rule.RuleId, "NTC2018-STEEL-LATERAL-TORSIONAL-BUCKLING-NOT-IMPLEMENTED" }
        });
        Assert.Equal(DesignCheckStatus.Passed, report.Results[0].Status);
        Assert.Equal(DesignCheckStatus.Unsupported, report.Results[1].Status);
        Assert.Equal(DesignOverallStatus.Incomplete, report.OverallStatus);
        Assert.False(report.IsComplete);
    }
}
