using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D2;

public sealed class Ntc2018SteelPureCompressionD2ATests
{
    private static readonly CommercialRolledSteelCatalogM88B2 Sections =
        CommercialRolledSteelCatalogM88B2.Load(Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));

    private const double Ipe200S235NcRdKn = 2848d * 235d / 1.05d / 1000d;

    private static Ntc2018SteelPureCompressionEvidenceD2A Evidence() => new(
        "M1", "IPE-200", "S235", "FEM-1", "SLU-STR-A1", 1.75,
        DesignEffectSectionSide.Continuous,
        Ntc2018SteelHoleStateD1.NoRelevantHoles,
        "Unperforated steel section confirmed at inspected station",
        HotRolledParallelFlangeAndDoublySymmetricConfirmed: true,
        ConcentricCompressionConfirmed: true,
        OtherSpatialActionsAndTorsionExcludedConfirmed: true,
        SectionGeometryEvidence: "Independent member schedule: hot-rolled parallel-flange IPE-200",
        GrossAreaEvidence: "28.48 cm2 cross-checked with declared nominal member schedule",
        DeclaredGrossAreaMm2: 2848,
        MaterialYieldEvidence: "S235 product grade declared; flange/web thickness band inspected",
        PureActionEvidence: "Concentric compression and no other in-plane moments/shears confirmed");

    private static DesignCheckRequest Request(double n = -600, double v = 0, double m = 0,
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
        }, new[] { Ntc2018SteelPureCompressionResistanceRuleD2A.Id });

    private static DesignCheckResult Check(DesignCheckRequest? request = null,
        Ntc2018SteelPureCompressionEvidenceD2A? evidence = null)
    {
        var rule = new Ntc2018SteelPureCompressionResistanceRuleD2A(
            Sections, Materials, new[] { evidence ?? Evidence() });
        return Assert.Single(new DesignCheckEngine(new[] { rule }).Evaluate(request ?? Request()).Results);
    }

    [Fact]
    public void PureCompressionIpe200S235_UsesNativeNtcGammaAndClassOne()
    {
        var r = Check();
        Assert.Equal(DesignCheckStatus.Passed, r.Status);
        Assert.Equal(600, r.Demand!.Value, 12);
        Assert.Equal(Ipe200S235NcRdKn, r.Resistance!.Value, 9);
        Assert.Equal(600d / Ipe200S235NcRdKn, r.Utilization!.Value, 11);
        Assert.Contains("class=1", r.CalculationTrace!);
        Assert.Contains("gammaM0=1.05", r.CalculationTrace!);
        Assert.Contains("NON CERTIFICATO", r.CalculationTrace!);
        Assert.Contains("NO MEMBER BUCKLING", r.CalculationTrace!);
        Assert.Contains("x=1.75", r.CalculationTrace!);
    }

    [Theory]
    [InlineData(-650, DesignCheckStatus.Failed)]
    [InlineData(-600, DesignCheckStatus.Passed)]
    public void CapacityDiscriminatesPassAndFail(double axial, DesignCheckStatus status) =>
        Assert.Equal(status, Check(Request(n: axial)).Status);

    [Fact]
    public void CapacityExactBoundaryIsInclusive_AndAboveFails()
    {
        Assert.Equal(DesignCheckStatus.Passed, Check(Request(n: -Ipe200S235NcRdKn)).Status);
        Assert.Equal(DesignCheckStatus.Failed, Check(Request(n: -(Ipe200S235NcRdKn + 1e-6))).Status);
    }

    [Theory]
    [InlineData("IPE-180", "S235", 1)]
    [InlineData("IPE-200", "S355", 2)]
    [InlineData("IPE-270", "S355", 3)]
    [InlineData("HEA-280", "S355", 3)]
    [InlineData("HEB-200", "S355", 1)]
    public void ClassOneTwoThree_AlwaysUseGrossSectionArea(
        string section, string grade, int expectedClass)
    {
        var e = Evidence() with
        {
            SectionId = section, SteelGrade = grade,
            DeclaredGrossAreaMm2 = Sections.Find(section).Definition.AreaCm2 * 100d
        };
        var r = Check(Request(n: -1, section: section), e);
        Assert.Equal(DesignCheckStatus.Passed, r.Status);
        var nominal = Sections.Find(section).Definition;
        var thickness = Math.Max(nominal.FlangeThicknessMm, nominal.WebThicknessMm);
        var expected = nominal.AreaCm2 * 100d * Materials.GetSteelYieldMpa(grade, thickness) / 1050d;
        Assert.Equal(expected, r.Resistance!.Value, 8);
        Assert.Contains($"class={expectedClass}", r.CalculationTrace!);
        Assert.Equal(1d / expected, r.Utilization!.Value, 10);
    }

    [Fact]
    public void ClassFourNeverUsesGrossAreaAsEffectiveArea()
    {
        // The current 30 I/H catalog entries include no pure-compression class-4
        // specimen. This is a SYNTHETIC TEST FIXTURE, NOT a published OPPO IPE.
        // It exercises the D2A Aeff fail-closed branch; NEVER ship as product data.
        var original = Sections.Find("IPE-270").Definition;
        const double height = 600d;
        const double area = 80d;
        const double strongI = 20000d;
        const double weakI = 200d;
        var synthetic = original with
        {
            Id = "IPE-CLASS4-TEST-ONLY", Name = "SYNTHETIC class-4 test fixture; NOT OPPO",
            HeightMm = height, AreaCm2 = area, MassKgPerM = 0.785 * area,
            StrongInertiaCm4 = strongI, WeakInertiaCm4 = weakI,
            StrongElasticModulusCm3 = 20 * strongI / height,
            WeakElasticModulusCm3 = 20 * weakI / original.WidthMm,
            StrongRadiusCm = Math.Sqrt(strongI / area),
            WeakRadiusCm = Math.Sqrt(weakI / area)
        };
        var fixture = new CommercialRolledSteelCatalogM88B2(
            "SYNTHETIC D2A CLASS4 TEST - NOT OPPO DATA", new[] { synthetic });
        var e = Evidence() with
        {
            SectionId = synthetic.Id, SteelGrade = "S355",
            SectionGeometryEvidence = "SYNTHETIC class4-only fixture, not a real manufactured section",
            GrossAreaEvidence = "Synthetic positive test area, not a physical declared area",
            DeclaredGrossAreaMm2 = area * 100d
        };
        var rule = new Ntc2018SteelPureCompressionResistanceRuleD2A(fixture, Materials, new[] { e });
        var r = Assert.Single(new DesignCheckEngine(new[] { rule }).Evaluate(
            Request(n: -1, section: synthetic.Id)).Results);
        Assert.Equal(DesignCheckStatus.NotApplicable, r.Status);
        Assert.Contains("Class 4", r.Message);
        Assert.Null(r.Resistance);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(100, 0, 0)]
    [InlineData(-100, 0.001, 0)]
    [InlineData(-100, 0, 0.001)]
    [InlineData(-100, -0.001, 0)]
    [InlineData(-100, 0, -0.001)]
    public void NonpureCompressionIsNotApplicable(double n, double v, double m) =>
        Assert.Equal(DesignCheckStatus.NotApplicable, Check(Request(n: n, v: v, m: m)).Status);

    [Fact]
    public void NullOrNonfiniteEffectsNeverDefaultToZero()
    {
        var r = Request();
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { ShearForceYKn = null } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects = r.Effects with { BendingMomentZKnm = null } }).Status);
        Assert.Equal(DesignCheckStatus.InvalidInput,
            Check(r with { Effects = r.Effects with { AxialForceKn = double.NaN } }).Status);
        Assert.Equal(DesignCheckStatus.InvalidInput,
            Check(r with { Effects = r.Effects with { AxialForceKn = double.PositiveInfinity } }).Status);
    }

    [Fact]
    public void IndependentEvidenceMustBindExactFemStationAndSide()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(member: "M2")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(analysis: "FEM-2")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(combination: "SLU-2")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(x: 1.7500001)).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(side: DesignEffectSectionSide.BeforePointLoad)).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(section: "HEA-200")).Status);
    }

    [Fact]
    public void IncompletePhysicalEvidenceFailsClosed()
    {
        var e = Evidence();
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { HoleState = Ntc2018SteelHoleStateD1.Unknown }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { HoleState = Ntc2018SteelHoleStateD1.RelevantConnectionHoles }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { HoleStateEvidence = " " }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { GrossAreaEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredGrossAreaMm2 = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredGrossAreaMm2 = 0 }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredGrossAreaMm2 = double.NaN }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { DeclaredGrossAreaMm2 = 2900 }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { SectionGeometryEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { MaterialYieldEvidence = "" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { PureActionEvidence = "" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { ConcentricCompressionConfirmed = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { HotRolledParallelFlangeAndDoublySymmetricConfirmed = false }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(evidence: e with { OtherSpatialActionsAndTorsionExcludedConfirmed = null }).Status);
    }

    [Fact]
    public void UnknownOrTaperedSectionAndUnknownGradeDoNotPass()
    {
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(Request(section: "INP-200"), Evidence() with { SectionId = "INP-200" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(section: "IPE-999"), Evidence() with { SectionId = "IPE-999" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(evidence: Evidence() with { SteelGrade = "S999" }).Status);
    }

    [Fact]
    public void SmallDeclaredAreaRoundingUsesConservativeMinimumOnly()
    {
        var rounded = Evidence() with { DeclaredGrossAreaMm2 = 2847.5d };
        var r = Check(Request(n: -1), rounded);
        Assert.Equal(DesignCheckStatus.Passed, r.Status);
        Assert.Equal(2847.5d * 235d / 1.05d / 1000d, r.Resistance!.Value, 9);
        Assert.Contains("A_declared=2847.5", r.CalculationTrace!);
        Assert.Contains("A_used_min=2847.5", r.CalculationTrace!);
    }

    [Fact]
    public void FemProvenanceCannotBeManufactured()
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
    public void NativeGammaProfileCannotBeSwappedWithEc3OrModified()
    {
        var p = Ntc2018SteelTensionProfileD1.Create();
        Assert.Equal(DesignCheckStatus.Unsupported,
            Check(Request(profile: p with { Code = new DesignCodeEdition("EN 1993-1-1", "2005", "IT") })).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(profile: p with { ProfileId = "NTC2018-WRONG" })).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(profile: p with { Coefficients = p.Coefficients.Select(c =>
                c.Id == "gamma_M0" ? c with { Value = 1d } : c).ToArray() })).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(profile: p with { Coefficients = p.Coefficients.Select(c =>
                c.Id == "gamma_M1" ? c with { SourceClause = "OTHER" } : c).ToArray() })).Status);
    }

    [Fact]
    public void DuplicateAndIncompleteEvidenceTargetIsRejected()
    {
        var e = Evidence();
        Assert.Throws<ArgumentException>(() => new Ntc2018SteelPureCompressionResistanceRuleD2A(
            Sections, Materials, new[] { e, e }));
        Assert.Throws<ArgumentException>(() => new Ntc2018SteelPureCompressionResistanceRuleD2A(
            Sections, Materials, new[] { e with { PositionMetres = double.NaN } }));
    }

    [Fact]
    public void SectionPassAndUnimplementedBucklingRemainIncomplete()
    {
        var rule = new Ntc2018SteelPureCompressionResistanceRuleD2A(
            Sections, Materials, new[] { Evidence() });
        Assert.False(rule.EstablishesCompleteMemberCompliance);
        var report = new DesignCheckEngine(new[] { rule }).Evaluate(Request() with
        {
            RuleIds = new[] { rule.RuleId, "NTC2018-STEEL-MEMBER-BUCKLING-NOT-IMPLEMENTED" }
        });
        Assert.Equal(DesignCheckStatus.Passed, report.Results[0].Status);
        Assert.Equal(DesignCheckStatus.Unsupported, report.Results[1].Status);
        Assert.Equal(DesignOverallStatus.Incomplete, report.OverallStatus);
        Assert.False(report.IsComplete);
    }
}
