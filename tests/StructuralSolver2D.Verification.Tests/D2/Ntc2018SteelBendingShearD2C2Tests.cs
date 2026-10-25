using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D2;

public sealed class Ntc2018SteelBendingShearD2C2Tests
{
    private static readonly CommercialRolledSteelCatalogM88B2 Sections =
        CommercialRolledSteelCatalogM88B2.Load(Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));
    private const double Ipe200Av = 1399.6d;
    private const double Ipe200Wpl = 220640d; // STAD published nominal IPE200 strong Wpl,y
    private const double Ipe200Wel = 194300d; // OPPO/independent strong Wel,y
    private static readonly double Ipe200Vc = Ipe200Av * 235d / (Math.Sqrt(3d) * 1.05d) / 1000d;
    private static readonly double Ipe200Mpl = Ipe200Wpl * 235d / 1.05d / 1_000_000d;

    private static Ntc2018SteelBendingShearEvidenceD2C2 Evidence() => new(
        "M1", "IPE-200", "S235", "FEM-1", "SLU-STR-A1", 1.75,
        DesignEffectSectionSide.Continuous, Ntc2018SteelHoleStateD1.NoRelevantHoles,
        "Physical IPE200 without relevant connection holes, checked",
        HotRolledParallelFlangeAndDoublySymmetricConfirmed: true,
        FemMzStrongAxisAndVyWebPlaneConfirmed: true,
        UnstiffenedWebConfirmed: true,
        OtherSpatialActionsAndTorsionExcludedConfirmed: true,
        SectionGeometryEvidence: "Independent member schedule h=200 b=100 tf=8.5 tw=5.6 r=12",
        MaterialYieldEvidence: "S235 8.5mm strength band verified",
        CombinedActionEvidence: "Simultaneous FEM N=0 and strong Mz plus web-plane Vy, out-of-plane excluded",
        WebShearAreaEvidence: "NTC 2018 [4.2.18] independent Av=1399.6mm2",
        DeclaredWebShearAreaMm2: Ipe200Av,
        ElasticModulusEvidence: "IPE200 OPPO nominal Wel,y=194.3cm3 externally confirmed",
        DeclaredStrongElasticModulusMm3: Ipe200Wel,
        PlasticModulusEvidence: "STAD nominal IPE200 Wpl,y=220.64cm3 independently published",
        DeclaredStrongPlasticModulusMm3: Ipe200Wpl,
        WebAreaEvidence: "Independent IPE200 web depth between flanges=183 mm, tw=5.6 mm",
        DeclaredGeometricWebAreaMm2: 183d * 5.6d);

    private static DesignCheckRequest Request(double n = 0, double v = 80, double m = 40,
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
        }, new[] { Ntc2018SteelBendingShearInteractionRuleD2C2.Id });

    private static DesignCheckResult Check(DesignCheckRequest? request = null,
        Ntc2018SteelBendingShearEvidenceD2C2? e = null,
        CommercialRolledSteelCatalogM88B2? sections = null)
    {
        var rule = new Ntc2018SteelBendingShearInteractionRuleD2C2(
            sections ?? Sections, Materials, new[] { e ?? Evidence() });
        return Assert.Single(new DesignCheckEngine(new[] { rule }).Evaluate(request ?? Request()).Results);
    }

    [Fact]
    public void LowShearClassOneDoesNotReduceBendingAndChecksBothActions()
    {
        var r = Check();
        Assert.Equal(DesignCheckStatus.Passed, r.Status);
        Assert.Equal(Math.Max(80d / Ipe200Vc, 40d / Ipe200Mpl), r.Demand!.Value, 10);
        Assert.Equal(1d, r.Resistance!.Value);
        Assert.Equal("ratio", r.Unit);
        Assert.Contains("LowShear-NoMomentReduction", r.CalculationTrace!);
        Assert.Contains("class=1", r.CalculationTrace!);
        Assert.Contains("rho=0", r.CalculationTrace!);
        Assert.Contains("Aw_web_bound_used=not needed", r.CalculationTrace!);
        Assert.Contains("OPPO NOMINAL, NON CERTIFICATO", r.CalculationTrace!);
        Assert.False(new Ntc2018SteelBendingShearInteractionRuleD2C2(
            Sections, Materials, new[] { Evidence() }).EstablishesCompleteMemberCompliance);
    }

    [Fact]
    public void ExactlyHalfVcIsLowShearAndHigherValuesUseRho()
    {
        var boundary = Check(Request(v: 0.5d * Ipe200Vc, m: Ipe200Mpl));
        Assert.Equal(DesignCheckStatus.Passed, boundary.Status);
        Assert.Contains("LowShear-NoMomentReduction", boundary.CalculationTrace!);
        var above = Check(Request(v: 0.5d * Ipe200Vc + 0.001d, m: Ipe200Mpl));
        Assert.Equal(DesignCheckStatus.Failed, above.Status);
        Assert.Contains("HighShear-PlasticMomentReduction", above.CalculationTrace!);
    }

    [Fact]
    public void HighShearClassOneUsesNativeRhoAndConservativeWebAreaPenalty()
    {
        const double v = 150d;
        double rho = Math.Pow(2d * v / Ipe200Vc - 1d, 2d);
        // Conservative A_w upper bound = A - 2b*tf = 2848 - 1700.
        double mvRd = (Ipe200Wpl - rho * (1148d * 1148d) / (4d * 5.6d)) *
            235d / 1.05d / 1_000_000d;
        var passed = Check(Request(v: v, m: 40));
        Assert.Equal(DesignCheckStatus.Passed, passed.Status);
        Assert.Equal(Math.Max(v / Ipe200Vc, 40d / mvRd), passed.Utilization!.Value, 10);
        Assert.Contains("HighShear-PlasticMomentReduction", passed.CalculationTrace!);
        Assert.Contains("Aw_web_bound_used=1148", passed.CalculationTrace!);
        Assert.Contains("Av_used_min=", passed.CalculationTrace!);
        Assert.Equal(DesignCheckStatus.Failed, Check(Request(v: v, m: 46)).Status);
        Assert.Equal(DesignCheckStatus.Failed, Check(Request(v: -v, m: -46)).Status);
    }

    [Fact]
    public void ShearAboveVcFailsWithoutExtrapolatingRho()
    {
        var r = Check(Request(v: Ipe200Vc + 0.01d, m: 1));
        Assert.Equal(DesignCheckStatus.Failed, r.Status);
        Assert.True(r.Utilization > 1);
        Assert.Contains("rho/M_VRd NOT evaluated", r.CalculationTrace!);
        Assert.Equal(DesignCheckStatus.Passed, Check(Request(v: Ipe200Vc, m: 1)).Status);
    }

    [Fact]
    public void ClassTwoHighShearWorksWithExternallyPublishedPlasticModulus()
    {
        // ArcelorMittal Orange Book: HE180A rounded Wpl,y=325 cm3.
        var p = Sections.Find("HEA-180").Definition;
        double av = p.AreaCm2 * 100d - 2d * p.WidthMm * p.FlangeThicknessMm +
            (p.WebThicknessMm + 2d * p.RootRadiusMm) * p.FlangeThicknessMm;
        double vc = av * 355d / (Math.Sqrt(3d) * 1.05d) / 1000d;
        var e = Evidence() with
        {
            SectionId = "HEA-180", SteelGrade = "S355",
            SectionGeometryEvidence = "HEA180 h=171 b=180 tf=9.5 tw=6 r=15",
            MaterialYieldEvidence = "S355 9.5mm verified grade band",
            WebShearAreaEvidence = "HEA180 geometric NTC web Av derived externally",
            DeclaredWebShearAreaMm2 = av,
            ElasticModulusEvidence = "HEA180 OPPO Wel,y=293.6cm3 independently verified",
            DeclaredStrongElasticModulusMm3 = 293600d,
            PlasticModulusEvidence = "ArcelorMittal HE180A Wpl,y=325cm3 published rounded",
            DeclaredStrongPlasticModulusMm3 = 325000d,
            WebAreaEvidence = "HEA180 web depth between flanges confirmed independently",
            DeclaredGeometricWebAreaMm2 = (p.HeightMm - 2d * p.FlangeThicknessMm) * p.WebThicknessMm
        };
        var r = Check(Request(section: "HEA-180", v: vc * 0.75d, m: 60), e);
        Assert.Equal(DesignCheckStatus.Passed, r.Status);
        Assert.Contains("class=2", r.CalculationTrace!);
        Assert.Contains("HighShear-PlasticMomentReduction", r.CalculationTrace!);
    }

    [Fact]
    public void ClassThreeIsOnlyAllowedWhenVBelowHalfVc()
    {
        var e = Evidence() with
        {
            SectionId = "HEA-280", SteelGrade = "S355",
            SectionGeometryEvidence = "HEA280 OPPO geometry physically attested",
            MaterialYieldEvidence = "S355 13mm thickness band attested",
            WebShearAreaEvidence = "Independent NTC Av=3174mm2",
            DeclaredWebShearAreaMm2 = 3174,
            ElasticModulusEvidence = "Wel,y=1013 cm3 independently attested",
            DeclaredStrongElasticModulusMm3 = 1_013_000d,
            PlasticModulusEvidence = null, DeclaredStrongPlasticModulusMm3 = null,
            WebAreaEvidence = null, DeclaredGeometricWebAreaMm2 = null
        };
        double vc = 3174d * 355d / (Math.Sqrt(3d) * 1.05d) / 1000d;
        var low = Check(Request(section: "HEA-280", v: vc * 0.4, m: 250), e);
        Assert.Equal(DesignCheckStatus.Passed, low.Status);
        Assert.Contains("class=3", low.CalculationTrace!);
        Assert.Contains("LowShear-NoMomentReduction", low.CalculationTrace!);
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(Request(section: "HEA-280", v: vc * 0.75, m: 250), e).Status);
    }

    [Fact]
    public void HighShearNeedsAwDistinctFromAvAndEvidence()
    {
        var e = Evidence();
        Assert.Equal(DesignCheckStatus.Passed,
            Check(Request(v: 150), e with { DeclaredGeometricWebAreaMm2 = 890.4d }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(v: 150), e with { DeclaredGeometricWebAreaMm2 = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(v: 150), e with { WebAreaEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(v: 150), e with { DeclaredGeometricWebAreaMm2 = Ipe200Av }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(v: 150), e with { DeclaredGeometricWebAreaMm2 = 800 }).Status);
        // For low shear, A_w is not used in the NTC reduction and may be absent.
        Assert.Equal(DesignCheckStatus.Passed,
            Check(e: e with { DeclaredGeometricWebAreaMm2 = null, WebAreaEvidence = null }).Status);
    }

    [Fact]
    public void NoIndependentGeometryPlasticModulusOrShearAreaNoPass()
    {
        var e = Evidence();
        Assert.Equal(DesignCheckStatus.MissingData, Check(e: e with { DeclaredWebShearAreaMm2 = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(e: e with { DeclaredWebShearAreaMm2 = 1405 }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(e: e with { DeclaredStrongElasticModulusMm3 = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(e: e with { DeclaredStrongElasticModulusMm3 = 190000 }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(e: e with { DeclaredStrongPlasticModulusMm3 = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(e: e with { PlasticModulusEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(e: e with { SectionGeometryEvidence = " " }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(e: e with { MaterialYieldEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(e: e with { CombinedActionEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(e: e with { WebShearAreaEvidence = null }).Status);
    }

    [Theory]
    [InlineData(1, 80, 40)]
    [InlineData(-1, 80, 40)]
    [InlineData(0, 0, 40)]
    [InlineData(0, 80, 0)]
    public void OnlyTrueConcurrentBendingAndShearWithZeroAxialForceApplies(
        double n, double v, double m) =>
        Assert.Equal(DesignCheckStatus.NotApplicable, Check(Request(n: n, v: v, m: m)).Status);

    [Fact]
    public void MissingFiniteSimultaneousActionNeverDefaultsToZero()
    {
        var r = Request();
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { AxialForceKn = null } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { ShearForceYKn = null } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { BendingMomentZKnm = null } }).Status);
        Assert.Equal(DesignCheckStatus.InvalidInput,
            Check(r with { Effects = r.Effects with { BendingMomentZKnm = double.PositiveInfinity } }).Status);
        Assert.Equal(DesignCheckStatus.InvalidInput,
            Check(r with { Effects = r.Effects with { ShearForceYKn = double.NaN } }).Status);
    }

    [Fact]
    public void WrongFemProvenanceAndPhysicalFlagsNeverPass()
    {
        var e = Evidence();
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(member: "M2")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(section: "IPE-180")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(analysis: "OTHER")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(combination: "OTHER")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(x: 1.750001d)).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(side: DesignEffectSectionSide.BeforePointLoad)).Status);
        var r = Request();
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { TargetKind = DesignEffectTargetKind.LoadCase } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { Origin = DesignEffectOrigin.ExternalManual } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { SectionSide = null } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(e: e with { HoleState = Ntc2018SteelHoleStateD1.RelevantConnectionHoles }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(e: e with { HoleStateEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(e: e with { FemMzStrongAxisAndVyWebPlaneConfirmed = false }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(e: e with { UnstiffenedWebConfirmed = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(e: e with { OtherSpatialActionsAndTorsionExcludedConfirmed = false }).Status);
    }

    [Fact]
    public void WebBucklingFixtureCannotReturnPassingCombinedResistance()
    {
        var original = Sections.Find("IPE-270").Definition;
        const double area = 60d;
        const double height = 480d;
        const double ix = 14000d;
        const double iy = 419.9d;
        var synthetic = original with
        {
            Id = "IPE-SLENDER-D2C2-FIXTURE", Name = "SYNTHETIC not OPPO product",
            HeightMm = height, AreaCm2 = area, MassKgPerM = area * 0.785d,
            StrongInertiaCm4 = ix, WeakInertiaCm4 = iy,
            StrongElasticModulusCm3 = 20d * ix / height,
            WeakElasticModulusCm3 = 20d * iy / original.WidthMm,
            StrongRadiusCm = Math.Sqrt(ix / area), WeakRadiusCm = Math.Sqrt(iy / area)
        };
        var fixture = new CommercialRolledSteelCatalogM88B2("SYNTHETIC NOT OPPO", new[] { synthetic });
        double av = area * 100d - 2d * synthetic.WidthMm * synthetic.FlangeThicknessMm +
            (synthetic.WebThicknessMm + 2d * synthetic.RootRadiusMm) * synthetic.FlangeThicknessMm;
        var e = Evidence() with
        {
            SectionId = synthetic.Id, SteelGrade = "S355",
            SectionGeometryEvidence = "SYNTHETIC slender test-only geometry",
            DeclaredWebShearAreaMm2 = av,
            DeclaredStrongElasticModulusMm3 = synthetic.StrongElasticModulusCm3 * 1000d
        };
        var result = Check(Request(section: synthetic.Id, v: 10), e, fixture);
        Assert.Equal(DesignCheckStatus.NotApplicable, result.Status);
        Assert.Contains("shear-buckling", result.Message);
        Assert.Null(result.Resistance);
    }

    [Fact]
    public void ClassFourNeedsMissingEffectiveProperties()
    {
        var original = Sections.Find("IPE-270").Definition;
        const double height = 900d;
        const double area = 80d;
        const double ix = 20000d;
        const double iy = 200d;
        var synthetic = original with
        {
            Id = "IPE-CLASS4-D2C2-FIXTURE", Name = "SYNTHETIC not OPPO",
            HeightMm = height, AreaCm2 = area, MassKgPerM = 0.785d * area,
            StrongInertiaCm4 = ix, WeakInertiaCm4 = iy,
            StrongElasticModulusCm3 = 20d * ix / height,
            WeakElasticModulusCm3 = 20d * iy / original.WidthMm,
            StrongRadiusCm = Math.Sqrt(ix / area), WeakRadiusCm = Math.Sqrt(iy / area)
        };
        var fixture = new CommercialRolledSteelCatalogM88B2("SYNTHETIC CLASS4 TEST", new[] { synthetic });
        var e = Evidence() with
        {
            SectionId = synthetic.Id, SteelGrade = "S355",
            SectionGeometryEvidence = "SYNTHETIC class4 geometry",
            DeclaredStrongElasticModulusMm3 = synthetic.StrongElasticModulusCm3 * 1000d
        };
        var result = Check(Request(section: synthetic.Id, v: 1), e, fixture);
        Assert.Equal(DesignCheckStatus.NotApplicable, result.Status);
        Assert.Contains("Class 4", result.Message);
    }

    [Fact]
    public void WrongNormativeProfileTaperedInpsAndMissingGradeNeverPass()
    {
        var p = Ntc2018SteelTensionProfileD1.Create();
        Assert.Equal(DesignCheckStatus.Unsupported,
            Check(Request(profile: p with { Code = new DesignCodeEdition("EN 1993-1-1", "2005", "IT") })).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(profile: p with { Coefficients = p.Coefficients.Select(c =>
                c.Id == "gamma_M0" ? c with { Value = 1d } : c).ToArray() })).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(Request(section: "INP-200"), Evidence() with { SectionId = "INP-200" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(e: Evidence() with { SteelGrade = "S999" }).Status);
    }

    [Fact]
    public void UnimplementedMemberObligationPreventsOverallCompletion()
    {
        var e = Evidence();
        var rule = new Ntc2018SteelBendingShearInteractionRuleD2C2(Sections, Materials, new[] { e });
        var report = new DesignCheckEngine(new[] { rule }).Evaluate(Request() with
        {
            RuleIds = new[] { rule.RuleId, "NTC2018-LATERAL-TORSIONAL-MEMBER-NOT-IMPLEMENTED" }
        });
        Assert.Equal(DesignCheckStatus.Passed, report.Results[0].Status);
        Assert.Equal(DesignCheckStatus.Unsupported, report.Results[1].Status);
        Assert.Equal(DesignOverallStatus.Incomplete, report.OverallStatus);
        Assert.False(report.IsComplete);
        Assert.Throws<ArgumentException>(() => new Ntc2018SteelBendingShearInteractionRuleD2C2(
            Sections, Materials, new[] { e, e }));
    }
}
