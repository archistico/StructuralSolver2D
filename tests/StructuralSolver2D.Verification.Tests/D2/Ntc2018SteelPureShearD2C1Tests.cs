using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D2;

public sealed class Ntc2018SteelPureShearD2C1Tests
{
    private static readonly CommercialRolledSteelCatalogM88B2 Sections =
        CommercialRolledSteelCatalogM88B2.Load(Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));

    // IPE200 OPPO: A=2848 mm2, b=100, tf=8.5, tw=5.6, r=12 mm.
    // NTC2018 [4.2.18]: Av=2848 - 2*100*8.5 + (5.6+2*12)*8.5 = 1399.6 mm2.
    private const double Ipe200AvMm2 = 1399.6d;
    private static readonly double Ipe200S235Vrd = Ipe200AvMm2 * 235d / (Math.Sqrt(3d) * 1.05d) / 1000d;

    private static Ntc2018SteelPureShearEvidenceD2C1 Evidence() => new(
        "M1", "IPE-200", "S235", "FEM-1", "SLU-STR-A1", 1.75,
        DesignEffectSectionSide.Continuous,
        Ntc2018SteelHoleStateD1.NoRelevantHoles,
        "Steel IPE200 without relevant connection holes, physically confirmed",
        HotRolledParallelFlangeAndDoublySymmetricConfirmed: true,
        FemLocalVyActsInWebPlaneConfirmed: true,
        UnstiffenedWebConfirmed: true,
        OtherSpatialActionsAndTorsionExcludedConfirmed: true,
        SectionGeometryEvidence: "Independent member schedule: IPE200 h=200 b=100 tf=8.5 tw=5.6 r=12",
        MaterialYieldEvidence: "S235 grade and controlling 8.5mm thickness verified",
        PureActionEvidence: "One-sided FEM section pure local Vy in web plane, simultaneous N=Mz=0",
        ShearAreaEvidence: "Independent NTC 2018 [4.2.18] geometric sheet calculation: Av=1399.6 mm2",
        DeclaredWebPlaneShearAreaMm2: Ipe200AvMm2);

    private static DesignCheckRequest Request(double n = 0, double v = 100, double m = 0,
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
        }, new[] { Ntc2018SteelPureShearResistanceRuleD2C1.Id });

    private static DesignCheckResult Check(DesignCheckRequest? request = null,
        Ntc2018SteelPureShearEvidenceD2C1? evidence = null,
        CommercialRolledSteelCatalogM88B2? sections = null)
    {
        var rule = new Ntc2018SteelPureShearResistanceRuleD2C1(
            sections ?? Sections, Materials, new[] { evidence ?? Evidence() });
        return Assert.Single(new DesignCheckEngine(new[] { rule }).Evaluate(request ?? Request()).Results);
    }

    [Fact]
    public void Ipe200S235UsesNativeNtcShearAreaAndGammaM0()
    {
        var result = Check();
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.Equal(100, result.Demand!.Value, 12);
        Assert.Equal(Ipe200S235Vrd, result.Resistance!.Value, 9);
        Assert.Equal(100 / Ipe200S235Vrd, result.Utilization!.Value, 10);
        Assert.Contains("Av_NTC=", result.CalculationTrace!);
        Assert.Contains("gammaM0=1.05", result.CalculationTrace!);
        Assert.Contains("eta=1", result.CalculationTrace!);
        Assert.Contains("NOT TRIGGERED", result.CalculationTrace!);
        Assert.Contains("NON CERTIFICATO", result.CalculationTrace!);
        Assert.False(new Ntc2018SteelPureShearResistanceRuleD2C1(
            Sections, Materials, new[] { Evidence() }).EstablishesCompleteMemberCompliance);
    }

    [Fact]
    public void SignedShearAndPassFailThresholdsAreSymmetric()
    {
        Assert.Equal(DesignCheckStatus.Passed, Check(Request(v: -100)).Status);
        Assert.Equal(DesignCheckStatus.Passed, Check(Request(v: Ipe200S235Vrd)).Status);
        Assert.Equal(DesignCheckStatus.Failed, Check(Request(v: Ipe200S235Vrd + 0.0001d)).Status);
        Assert.Equal(DesignCheckStatus.Failed, Check(Request(v: -200)).Status);
    }

    [Fact]
    public void Hea280S355WorksEvenThoughCompressionClassThree()
    {
        // Pure shear [4.2.17] has no compressive/bending class predicate.
        var e = Evidence() with
        {
            SectionId = "HEA-280", SteelGrade = "S355",
            SectionGeometryEvidence = "Declared HEA280 geometry and NTC shear calculation",
            MaterialYieldEvidence = "S355 13mm flange thickness band declared",
            ShearAreaEvidence = "A=9726, b=280, tf=13, tw=8, r=24 => Av=3174 mm2",
            DeclaredWebPlaneShearAreaMm2 = 3174
        };
        var result = Check(Request(v: 100, section: "HEA-280"), e);
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.Equal(3174d * 355d / (Math.Sqrt(3d) * 1.05d) / 1000d,
            result.Resistance!.Value, 9);
    }

    [Fact]
    public void ConservativeRoundedAreaGovernsCapacity()
    {
        var e = Evidence() with { DeclaredWebPlaneShearAreaMm2 = 1399.2d };
        var result = Check(Request(v: 1), e);
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.Equal(1399.2d * 235d / (Math.Sqrt(3d) * 1.05d) / 1000d,
            result.Resistance!.Value, 9);
        Assert.Contains("Av_used_min=", result.CalculationTrace!);
        Assert.Contains("Av_declared=", result.CalculationTrace!);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(evidence: e with { DeclaredWebPlaneShearAreaMm2 = 1402d }).Status);
    }

    [Fact]
    public void SlenderUnstiffenedWebDoesNotGetFalseShearBucklingPass()
    {
        // Geometry-only, TEST FIXTURE — not a real OPPO profile/product.
        // hw_upper/tw=459.6/6.6=69.64 > 72*sqrt(235/355)=58.58.
        var original = Sections.Find("IPE-270").Definition;
        const double height = 480d;
        const double area = 60d;
        const double ix = 14000d;
        const double iy = 419.9d;
        var synthetic = original with
        {
            Id = "IPE-SHEAR-BUCKLING-FIXTURE", Name = "SYNTHETIC; NOT OPPO PRODUCT",
            HeightMm = height, AreaCm2 = area, MassKgPerM = area * 0.785d,
            StrongInertiaCm4 = ix, WeakInertiaCm4 = iy,
            StrongElasticModulusCm3 = 20d * ix / height,
            WeakElasticModulusCm3 = 20d * iy / original.WidthMm,
            StrongRadiusCm = Math.Sqrt(ix / area), WeakRadiusCm = Math.Sqrt(iy / area)
        };
        var fixture = new CommercialRolledSteelCatalogM88B2("SYNTHETIC D2C1 NOT OPPO", new[] { synthetic });
        double av = area * 100d - 2d * synthetic.WidthMm * synthetic.FlangeThicknessMm +
            (synthetic.WebThicknessMm + 2d * synthetic.RootRadiusMm) * synthetic.FlangeThicknessMm;
        var e = Evidence() with
        {
            SectionId = synthetic.Id, SteelGrade = "S355",
            SectionGeometryEvidence = "SYNTHETIC test-only dimensional fixture",
            ShearAreaEvidence = "SYNTHETIC derived NTC Av",
            DeclaredWebPlaneShearAreaMm2 = av
        };
        var r = Check(Request(v: 1, section: synthetic.Id), e, fixture);
        Assert.Equal(DesignCheckStatus.NotApplicable, r.Status);
        Assert.Contains("shear-buckling", r.Message);
        Assert.Null(r.Resistance);
    }

    [Theory]
    [InlineData(1, 100, 0)]
    [InlineData(-1, 100, 0)]
    [InlineData(0, 100, 0.0001)]
    [InlineData(0, 100, -0.0001)]
    [InlineData(0, 0, 0)]
    public void AnyConcurrentAxialMomentOrZeroShearExitsPureRule(double n, double v, double m) =>
        Assert.Equal(DesignCheckStatus.NotApplicable, Check(Request(n: n, v: v, m: m)).Status);

    [Fact]
    public void UnknownOrNonfiniteConcomitantActionNeverDefaultsToZero()
    {
        var request = Request();
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(request with { Effects = request.Effects with { AxialForceKn = null } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(request with { Effects = request.Effects with { BendingMomentZKnm = null } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(request with { Effects = request.Effects with { ShearForceYKn = null } }).Status);
        Assert.Equal(DesignCheckStatus.InvalidInput,
            Check(request with { Effects = request.Effects with { ShearForceYKn = double.NaN } }).Status);
        Assert.Equal(DesignCheckStatus.InvalidInput,
            Check(request with { Effects = request.Effects with { ShearForceYKn = double.PositiveInfinity } }).Status);
    }

    [Fact]
    public void EvidenceMustBindExactFemStationAndOneSidedCombination()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(member: "M2")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(analysis: "OTHER")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(combination: "OTHER")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(x: 1.750001d)).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(side: DesignEffectSectionSide.BeforePointLoad)).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(section: "IPE-180")).Status);
        var request = Request();
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(request with { Effects = request.Effects with { Origin = DesignEffectOrigin.ExternalManual } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(request with { Effects = request.Effects with { TargetKind = DesignEffectTargetKind.LoadCase } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(request with { Effects = request.Effects with { SectionSide = null } }).Status);
    }

    [Fact]
    public void IncompleteGeometryShearAreaHoleAndTorsionProofFailsClosed()
    {
        var e = Evidence();
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredWebPlaneShearAreaMm2 = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredWebPlaneShearAreaMm2 = 0 }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredWebPlaneShearAreaMm2 = double.NaN }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { ShearAreaEvidence = " " }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { SectionGeometryEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { MaterialYieldEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { PureActionEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { HoleStateEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { HoleState = Ntc2018SteelHoleStateD1.Unknown }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { HoleState = Ntc2018SteelHoleStateD1.RelevantConnectionHoles }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { FemLocalVyActsInWebPlaneConfirmed = false }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { UnstiffenedWebConfirmed = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { OtherSpatialActionsAndTorsionExcludedConfirmed = false }).Status);
    }

    [Fact]
    public void TaperedSectionUnknownSectionOrGradeCannotPass()
    {
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(Request(section: "INP-200"), Evidence() with { SectionId = "INP-200" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(section: "IPE-999"), Evidence() with { SectionId = "IPE-999" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(evidence: Evidence() with { SteelGrade = "S999" }).Status);
    }

    [Fact]
    public void NtcProfileFactorsAndEditionMustBeExact()
    {
        var p = Ntc2018SteelTensionProfileD1.Create();
        Assert.Equal(DesignCheckStatus.Unsupported,
            Check(Request(profile: p with { Code = new DesignCodeEdition("EN 1993-1-1", "2005", "IT") })).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(profile: p with { ProfileId = "WRONG" })).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(profile: p with { Coefficients = p.Coefficients.Select(c =>
                c.Id == "gamma_M0" ? c with { Value = 1d } : c).ToArray() })).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(profile: p with { Coefficients = p.Coefficients.Select(c =>
                c.Id == "gamma_M1" ? c with { SourceClause = "WRONG" } : c).ToArray() })).Status);
    }

    [Fact]
    public void DuplicatedOrMalformedEvidenceAndUnimplementedMemberObligationNeverPassOverall()
    {
        var e = Evidence();
        Assert.Throws<ArgumentException>(() => new Ntc2018SteelPureShearResistanceRuleD2C1(
            Sections, Materials, new[] { e, e }));
        Assert.Throws<ArgumentException>(() => new Ntc2018SteelPureShearResistanceRuleD2C1(
            Sections, Materials, new[] { e with { PositionMetres = double.NaN } }));
        var rule = new Ntc2018SteelPureShearResistanceRuleD2C1(Sections, Materials, new[] { e });
        var report = new DesignCheckEngine(new[] { rule }).Evaluate(Request() with
        {
            RuleIds = new[] { rule.RuleId, "NTC2018-STEEL-WEB-BUCKLING-NOT-IMPLEMENTED" }
        });
        Assert.Equal(DesignCheckStatus.Passed, report.Results[0].Status);
        Assert.Equal(DesignCheckStatus.Unsupported, report.Results[1].Status);
        Assert.Equal(DesignOverallStatus.Incomplete, report.OverallStatus);
        Assert.False(report.IsComplete);
    }
}
