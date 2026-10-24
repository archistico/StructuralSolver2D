using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.M91;

public sealed class SteelLateralTorsionalBucklingRuleM91CTests
{
    private static readonly DesignCodeEdition Code = new("EN 1993-1-1", "2005+A1:2014", "IT-TEST-ANNEX-EXPLICIT");
    private static readonly CommercialRolledSteelCatalogM88B2 Sections = CommercialRolledSteelCatalogM88B2.Load(
        Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials = CharacteristicMaterialCatalogM88C.Load(
        Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));

    // HEA 280/S355 is bending class 3 in M90-A reference tables. For unit tests only:
    // the external Mcr=600 kNm is synthetic and does NOT certify an actual beam design.
    private static SteelLateralTorsionalAssignmentM91C Assignment => new(
        MemberId: "BEAM1", SectionId: "HEA-280", SteelGrade: "S355",
        BendingSectionClass: 3, ClassificationEvidence: "Bernuzzi Table 4.5 + independent M90-A strong-axis classification",
        SegmentStartMetres: 0, SegmentEndMetres: 4,
        SegmentRestraintEvidence: "TEST ONLY: manually modelled fork supports at x=0 and x=4m",
        MomentDiagramAndLoadHeightEvidence: "TEST ONLY: independently documented pure moment load diagram",
        ExternallyEstablishedMcrKnm: 600,
        ExternalMcrEvidence: "TEST ONLY: externally chosen illustrative Mcr for algorithm test, NOT a certified beam",
        StandardCase: null,
        PlasticStrongAxisModulusCm3: null, PlasticModulusEvidence: null,
        MajorAxisAndOrientationConfirmed: true,
        DoublySymmetricRolledSectionConfirmed: true,
        PrismaticSectionConfirmed: true,
        NoRelevantHolesConfirmed: true,
        GeneralCurveAndNationalAnnexConfirmed: true,
        GeneralCurveAndAnnexEvidence: "TEST ONLY: explicit EC3 general method 6.3.2.2 and selected mock annex");

    private static DesignCheckRequest Request(double m = 20, double n = 0, double v = 0,
        string section = "HEA-280", double gamma = 1.05, DesignCodeEdition? code = null) => new(
        new DesignMemberContext("BEAM1", DesignMaterial.Steel, section, 4),
        DesignLimitState.Ultimate,
        new NormativeProfile("TEST-NOT-A-REAL-DESIGN", code ?? Code,
            new[] { new NormativeCoefficient("gamma_M1", gamma,
                "TEST ONLY: source must be checked against actual design National Annex", "LTB partial factor") }),
        new DesignEffectSnapshot("FEM-REAL-MODEL-TEST", "ULS-COMB-1", DesignEffectOrigin.FemAnalysis,
            2, n, v, m)
        {
            MemberId = "BEAM1", TargetKind = DesignEffectTargetKind.LoadCombination,
            SectionSide = DesignEffectSectionSide.Continuous
        },
        new[] {SteelLateralTorsionalBucklingRuleM91C.Id});

    private static DesignCheckResult Check(DesignCheckRequest? req = null,
        SteelLateralTorsionalAssignmentM91C? assignment = null)
    {
        var rule = new SteelLateralTorsionalBucklingRuleM91C(Code, Sections, Materials,
            new[] {assignment ?? Assignment});
        return Assert.Single(new DesignCheckEngine(new[] {rule}).Evaluate(req ?? Request()).Results);
    }

    [Fact]
    public void BernuzziA8_2_IndependentGeneralMethodMatchesPublishedRounding()
    {
        // Bernuzzi second edition A8.2, pp. 233-235, IPE330/S275, Wpl=804.3cm3,
        // Mcr=550.4kNm, gammaM1=1.05, general curve b, alphaLT=0.34.
        // Separate standalone function: IPE330 is NOT falsely inserted in M88-B2.
        var c = SteelLateralTorsionalBucklingRuleM91C.CalculateGeneral("b", 804.3, 275, 550.4, 1.05);
        Assert.NotNull(c);
        Assert.InRange(c!.LambdaBarLt, 0.6338, 0.6341);
        Assert.InRange(c.PhiLt, 0.7745, 0.7750);
        Assert.InRange(c.ChiLt, 0.8194, 0.8199);
        Assert.InRange(c.BucklingResistanceKnm, 172.4, 172.8);
        Assert.Equal("b", c.Curve);
    }

    [Fact]
    public void BernuzziA8_2_UniformCaseMcr_WhenMultipliedByDocumentedC1MatchesReference()
    {
        // Bernuzzi A8.2 uses C1=1.141, L=2500mm, E=210000, G=80769,
        // Iz=788.1cm4, It=28.15cm4, Iw=199.9e9mm6 = 199900cm6.
        // The standard-case API implements C1=1 ONLY; C1 must never be guessed.
        var baseMcr = SteelLateralTorsionalBucklingRuleM91C.CalculateForkUniformMomentMcr(
            210000, 80769, 788.1, 28.15, 199900, 2.5);
        Assert.NotNull(baseMcr);
        Assert.InRange(baseMcr!.Value, 482.2, 482.6);
        Assert.InRange(baseMcr.Value * 1.141, 550.1, 550.6);
    }

    [Theory]
    [InlineData(200, 100, "a")]
    [InlineData(200, 200, "a")]
    [InlineData(200, 99.9, "b")]
    [InlineData(270, 280, "a")]
    public void EC3Table6_4_GeneralRolledICurveUsesHeightToWidthOnly(double h, double b, string expected) =>
        Assert.Equal(expected, SteelLateralTorsionalBucklingRuleM91C.SelectGeneralRolledICurve(h, b));

    [Fact]
    public void ExplicitSyntheticClassThreeMember_CalculatesPartialCheckWithAuditTrace()
    {
        var result = Check();
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.Equal(20d, result.Demand!.Value);
        Assert.True(result.Resistance!.Value > 0);
        Assert.Contains("M90A independently confirmed", result.CalculationTrace!);
        Assert.Contains("chiLT=", result.CalculationTrace!);
        Assert.Contains("Mcr=600", result.CalculationTrace!);
        Assert.Contains("NOT CHECKED", result.CalculationTrace!);
        Assert.Equal(DesignCheckStatus.Failed,
            Check(Request(m:1e6)).Status);
    }

    [Fact]
    public void BothSignsOfPureMomentGiveSamePartialUtilization()
    {
        var plus = Check(Request(m:25));
        var minus = Check(Request(m:-25));
        Assert.Equal(DesignCheckStatus.Passed, plus.Status);
        Assert.Equal(plus.Utilization, minus.Utilization);
        Assert.Equal(plus.Resistance, minus.Resistance);
    }

    [Theory]
    [InlineData(5, 0, 20)]
    [InlineData(0, 0.01, 20)]
    [InlineData(0, 0, 0)]
    public void NonPureBendingActionsNeverGetApproved(double n, double v, double m)
    {
        Assert.Equal(DesignCheckStatus.NotApplicable, Check(Request(m:m, n:n, v:v)).Status);
    }

    [Fact]
    public void MissingVerifiedMcrOrBothSourcesIsFailClosed()
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {ExternallyEstablishedMcrKnm=null}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {ExternallyEstablishedMcrKnm=double.NaN}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {ExternallyEstablishedMcrKnm=-1}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {ExternalMcrEvidence=" "}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {ExternallyEstablishedMcrKnm=double.PositiveInfinity}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {StandardCase=new SteelUniformMomentMcrInputM91C(
                210000, "test", 80769, "test", 28.15, "test", 199900, "test", true,true,true,true)}).Status);
    }

    [Fact]
    public void StandardCaseRequiresAllFourBoundaryAssertionsAndPropertySources()
    {
        var standard = new SteelUniformMomentMcrInputM91C(
            210000, "E documented", 80769, "G documented",
            28.15, "It externally documented", 199900, "Iw externally documented",
            true, true, true, true);
        var a = Assignment with {ExternallyEstablishedMcrKnm=null, ExternalMcrEvidence=null, StandardCase=standard};
        Assert.Equal(DesignCheckStatus.Passed, Check(assignment:a).Status);
        Assert.Contains("C1=1", Check(assignment:a).CalculationTrace!);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:a with {StandardCase=standard with {ForkRestraintsAtBothEndsConfirmed=false}}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:a with {StandardCase=standard with {FreeWarpingAtBothEndsConfirmed=null}}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:a with {StandardCase=standard with {UniformEndCouplesOnlyConfirmed=false}}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:a with {StandardCase=standard with {NoIntermediateRestraintsConfirmed=false}}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:a with {StandardCase=standard with {TorsionPropertyEvidence=null}}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:a with {StandardCase=standard with {WarpingConstantCm6=double.NaN}}).Status);
    }

    [Fact]
    public void NoImplicitPhysicalRestraintsOrOutOfPlaneDataFromPlanarFEM()
    {
        var request=Request();
        request = request with { Member=request.Member with {IsLaterallyRestrained=true, UnbracedLengthOutOfPlaneMetres=2} };
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(request, Assignment with { SegmentRestraintEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(request, Assignment with { MomentDiagramAndLoadHeightEvidence = null }).Status);
    }

    [Fact]
    public void ClassFourOrFalselyFavourableClassDoesNotGetPartialPass()
    {
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(assignment:Assignment with {BendingSectionClass=4}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {BendingSectionClass=1}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {ClassificationEvidence=null}).Status);
    }

    [Fact]
    public void MustUseCorrectGammaM1AndExplicitAnnexMethod()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(gamma:0)).Status);
        var req = Request();
        req = req with {Profile=req.Profile with {
            Coefficients = new[] {new NormativeCoefficient("gamma_M0",1,"wrong safety factor","not M1")}}};
        Assert.Equal(DesignCheckStatus.MissingData, Check(req).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {GeneralCurveAndNationalAnnexConfirmed=false}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {GeneralCurveAndAnnexEvidence=null}).Status);
        var edition = new DesignCodeEdition("EN 1993-1-1", "2022", "IT-TEST-ANNEX-EXPLICIT");
        Assert.Equal(DesignCheckStatus.Unsupported, Check(Request(code:edition)).Status);
        Assert.Throws<NotSupportedException>(() => new SteelLateralTorsionalBucklingRuleM91C(
            edition, Sections, Materials, new[] {Assignment}));
    }

    [Fact]
    public void WrongMemberSectionOrFEMProvenanceCannotPass()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(section:"IPE-200")).Status);
        var req = Request();
        req = req with {Effects=req.Effects with {MemberId=null}};
        Assert.Equal(DesignCheckStatus.MissingData, Check(req).Status);
        req=Request(); req=req with {Effects=req.Effects with {TargetKind=null}};
        Assert.Equal(DesignCheckStatus.MissingData,Check(req).Status);
        req=Request(); req=req with {Effects=req.Effects with {PositionMetres=3.7}};
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(req,Assignment with {SegmentEndMetres=3.5}).Status);
    }

    [Fact]
    public void InvalidSegmentGeometryIsRejectedWithoutExtrapolation()
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {SegmentStartMetres=1,SegmentEndMetres=1}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {SegmentEndMetres=5}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {SegmentStartMetres=double.NaN}).Status);
    }

    [Fact]
    public void TableAndCatalogueScopeForInclinedFlangeIsBlocked()
    {
        Assert.Equal(DesignCheckStatus.NotApplicable, Check(Request(section:"INP-200"),
            Assignment with {SectionId="INP-200", BendingSectionClass=1}).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(assignment:Assignment with {SteelGrade="S420"}).Status);
    }

    [Fact]
    public void ClassOneRequiresDocumentedPlasticModulusRatherThanSilentlyUsingElastic()
    {
        // IPE200 S235 is class 1 in major-axis pure bending. Catalog only has Wel.
        var a = Assignment with {SectionId="IPE-200", SteelGrade="S235", BendingSectionClass=1};
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(section:"IPE-200"),a).Status);
        // Clearly test-only illustrative Wpl; must exceed Wel and obey physical area/depth bound.
        a = a with {PlasticStrongAxisModulusCm3=220, PlasticModulusEvidence="TEST only: manually declared Wpl=220 cm3"};
        Assert.Equal(DesignCheckStatus.Passed, Check(Request(section:"IPE-200"),a).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(section:"IPE-200"),a with {PlasticStrongAxisModulusCm3=1}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(section:"IPE-200"),a with {PlasticStrongAxisModulusCm3=9999}).Status);
    }

    [Theory]
    [InlineData(-1, 210000, 788.1, 28.15, 199900, 2.5)]
    [InlineData(80769, 0, 788.1, 28.15, 199900, 2.5)]
    [InlineData(80769, 210000, 788.1, 28.15, -1, 2.5)]
    [InlineData(80769, 210000, 788.1, 28.15, 199900, 0)]
    public void StandardCriticalMoment_RejectsBadInputs(double g, double e, double iz,
        double it, double iw, double l)
    {
        Assert.Null(SteelLateralTorsionalBucklingRuleM91C.CalculateForkUniformMomentMcr(e,g,iz,it,iw,l));
    }

    [Fact]
    public void GeneralReductionRejectsNonFiniteOrInvalidData()
    {
        Assert.Null(SteelLateralTorsionalBucklingRuleM91C.CalculateGeneral("b", 804.3, 275, 0, 1.05));
        Assert.Null(SteelLateralTorsionalBucklingRuleM91C.CalculateGeneral("b", 804.3, 275, double.NaN, 1.05));
        Assert.Null(SteelLateralTorsionalBucklingRuleM91C.CalculateGeneral("x", 804.3, 275, 550.4, 1.05));
        Assert.Null(SteelLateralTorsionalBucklingRuleM91C.CalculateGeneral("b", 804.3, 275, 550.4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SteelLateralTorsionalBucklingRuleM91C.SelectGeneralRolledICurve(double.NaN,100));
    }

    [Fact]
    public void ShortBeamReductionIsCappedAtOneButNeverOverridesSectionCheck()
    {
        var c = SteelLateralTorsionalBucklingRuleM91C.CalculateGeneral("a",500,235,1e12,1.05);
        Assert.NotNull(c);
        Assert.InRange(c!.ChiLt,0,1);
        Assert.True(c.BucklingResistanceKnm <= c.SectionReferenceResistanceKnm);
    }

    [Fact]
    public void RuleIsPartialEvenWhenAllImplementedChecksPass()
    {
        var c = Check();
        Assert.Equal(DesignCheckStatus.Passed,c.Status);
        Assert.Contains("NOT complete member compliance",c.CalculationTrace!);
        Assert.Contains("NOT CHECKED",c.CalculationTrace!);
    }
}
