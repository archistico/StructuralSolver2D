using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.M89;

public sealed class ElasticBendingRuleM89ATests
{
    private static readonly DesignCodeEdition Code = new("EN 1993-1-1", "2005+A1:2014", "IT-USER-DECLARED-NO-AUTOMATIC-LEGAL-COMPLIANCE");
    private static readonly CommercialRolledSteelCatalogM88B2 Sections = CommercialRolledSteelCatalogM88B2.Load(Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials = CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));
    private static readonly SteelElasticBendingAssignmentM89A Assignment = new(
        "M1", "IPE-200", "S235", 3, "TEST: class 3 independently declared, not classified by software", true, true);
    private static readonly ElasticBendingRuleM89A Rule = new(Code, Sections, Materials, new[] { Assignment });
    private static readonly DesignCheckEngine Engine = new(new[] { Rule });

    private static DesignCheckRequest Request(double moment = 20, double axial = 0, double shear = 0,
        DesignCodeEdition? code = null, string sectionId = "IPE-200", string gradeSource = "USER-SUPPLIED gamma_M0") => new(
        new DesignMemberContext("M1", DesignMaterial.Steel, sectionId, 5),
        DesignLimitState.Ultimate,
        new NormativeProfile("EC3-M89A-NOT-COMPLETE", code ?? Code,
            new[] { new NormativeCoefficient(ElasticBendingRuleM89A.GammaId, 1.0, gradeSource, "Explicit user input, not default") }),
        new DesignEffectSnapshot("ANALYSIS1", "ULS1", DesignEffectOrigin.FemAnalysis, 2.5,
            AxialForceKn: axial, ShearForceYKn: shear, BendingMomentZKnm: moment)
        { MemberId = "M1", TargetKind = DesignEffectTargetKind.LoadCombination,
          SectionSide = DesignEffectSectionSide.Continuous },
        new[] { ElasticBendingRuleM89A.Id });

    private static DesignCheckResult Check(DesignCheckRequest? request = null) =>
        Assert.Single(Engine.Evaluate(request ?? Request()).Results);
    private static DesignCheckResult Custom(SteelElasticBendingAssignmentM89A assignment, DesignCheckRequest? request = null) =>
        Assert.Single(new DesignCheckEngine(new[] { new ElasticBendingRuleM89A(Code, Sections, Materials, new[] { assignment }) })
            .Evaluate(request ?? Request()).Results);

    [Fact]
    public void IPE200_S235_Class3_PureBending_UsesIndependentClosedFormInKnm()
    {
        // OPPO Wx = 194.3 cm3; S235 thickness tf=8.5 mm -> fy=235 MPa.
        const double expected = 194.3 * 235.0 / 1000.0;
        var check = Check();
        Assert.Equal(DesignCheckStatus.Passed, check.Status);
        Assert.InRange(check.Resistance!.Value, expected - 1e-10, expected + 1e-10);
        Assert.InRange(check.Utilization!.Value, 20/expected - 1e-12, 20/expected + 1e-12);
        Assert.Contains("thickness=max(tf,tw)", check.CalculationTrace!);
        Assert.Contains("NOT checked", check.CalculationTrace!);
        Assert.False(Rule.EstablishesCompleteMemberCompliance);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void BendingSignIsNotMistakenForNegativeDemand(int sign)
    {
        var check = Check(Request(moment: sign * 50));
        Assert.Equal(DesignCheckStatus.Failed, check.Status);
        Assert.Equal(50d, check.Demand!.Value);
    }

    [Fact]
    public void ExactlyAtResistancePasses_AndSlightlyAboveFails()
    {
        double r = 194.3 * 235 / 1000;
        Assert.Equal(DesignCheckStatus.Passed, Check(Request(moment: r)).Status);
        Assert.Equal(DesignCheckStatus.Failed, Check(Request(moment: r + 0.01)).Status);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 0.0001)]
    [InlineData(-10, -3)]
    public void ConcomitantNormalOrShearEffectsRejectPureBendingScope(double n, double v)
    {
        Assert.Equal(DesignCheckStatus.NotApplicable, Check(Request(axial: n, shear: v)).Status);
    }

    [Fact]
    public void MissingConcomitantShearNeverPasses()
    {
        var req = Request();
        req = req with { Effects = req.Effects with { ShearForceYKn = null } };
        Assert.Equal(DesignCheckStatus.MissingData, Check(req).Status);
    }

    [Fact]
    public void UserSuppliedGammaM0IsUsedWithoutSilentDefaults()
    {
        var r = Request();
        var q = r with { Profile = r.Profile with { Coefficients = new [] {
            new NormativeCoefficient("gamma_M0", 1.25, "USER-DEMONSTRATION-NOT-NATIONAL-ANNEX", "Explicit for test") } } };
        var check = Check(q);
        Assert.Equal(DesignCheckStatus.Passed, check.Status);
        Assert.InRange(check.Resistance!.Value, 194.3*235/1250-1e-10, 194.3*235/1250+1e-10);
    }

    [Fact]
    public void MissingOrZeroGammaIsNotAccepted()
    {
        var r = Request();
        var missing = r with { Profile = r.Profile with { Coefficients = Array.Empty<NormativeCoefficient>() } };
        Assert.Equal(DesignCheckStatus.MissingData, Check(missing).Status);
        var zero = r with { Profile = r.Profile with { Coefficients = new[] {
            new NormativeCoefficient("gamma_M0", 0, "SOURCE", "Invalid") } } };
        Assert.Equal(DesignCheckStatus.MissingData, Check(zero).Status);
    }

    [Fact]
    public void ExplicitHoleAndOrientationDeclarationsAreRequired()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Custom(Assignment with { NoRelevantHolesConfirmed = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Custom(Assignment with { StrongAxisMappingConfirmed = false }).Status);
    }

    [Fact]
    public void Class3EvidenceRequired_Class4And1AreNotSubstituted()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Custom(Assignment with { ClassificationEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Custom(Assignment with { SectionClass = null }).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable, Custom(Assignment with { SectionClass = 4 }).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable, Custom(Assignment with { SectionClass = 1 }).Status);
    }

    [Fact]
    public void SectionIdCannotBeSwappedAfterAssignment()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(sectionId:"IPE-240")).Status);
    }

    [Fact]
    public void UnknownGradeOrSectionCannotReturnPass()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Custom(Assignment with { SteelGrade = "S900" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Custom(Assignment with { SectionId = "IPE-999" }, Request(sectionId:"IPE-999")).Status);
    }

    [Fact]
    public void WrongNormativeEditionOrAnnexIsUnsupported()
    {
        Assert.Equal(DesignCheckStatus.Unsupported, Check(Request(code:new DesignCodeEdition("EN 1993-1-1","2022","IT"))).Status);
        Assert.Equal(DesignCheckStatus.Unsupported, Check(Request(code:new DesignCodeEdition("EN 1993-1-1","2005+A1:2014","OTHER"))).Status);
    }

    [Fact]
    public void Edition2022CannotBeRegisteredAsAnM89AImplementation()
    {
        Assert.Throws<NotSupportedException>(() => new ElasticBendingRuleM89A(
            new DesignCodeEdition("EN 1993-1-1", "2022", "IT"), Sections, Materials, new[] { Assignment }));
    }

    [Fact]
    public void RulePassAndUnsupportedShearAreNotAnOverallApproval()
    {
        var r = Request();
        var both = r with { RuleIds = new[] { ElasticBendingRuleM89A.Id, "EC3-SHEAR-NOT-IMPLEMENTED" } };
        var report = Engine.Evaluate(both);
        Assert.Equal(DesignCheckStatus.Passed, report.Results[0].Status);
        Assert.Equal(DesignCheckStatus.Unsupported, report.Results[1].Status);
        Assert.Equal(DesignOverallStatus.Incomplete, report.OverallStatus);
        Assert.False(Rule.EstablishesCompleteMemberCompliance);
    }

    [Fact]
    public void S355_ThicknessBandIsNotAssumedConstant()
    {
        // HEB-240 flange thickness is above 16 mm in the catalog; fy must NOT be 355 MPa.
        var assn = Assignment with { SectionId = "HEB-240", SteelGrade = "S355" };
        var req = Request(sectionId: "HEB-240");
        var check = Custom(assn, req);
        var sec = Sections.Find("HEB-240").Definition;
        var fy = Materials.GetSteelYieldMpa("S355", Math.Max(sec.FlangeThicknessMm, sec.WebThicknessMm));
        Assert.True(sec.FlangeThicknessMm > 16);
        Assert.True(fy < 355);
        Assert.InRange(check.Resistance!.Value,
            sec.StrongElasticModulusCm3 * fy / 1000 - 1e-10,
            sec.StrongElasticModulusCm3 * fy / 1000 + 1e-10);
    }

    [Fact]
    public void CatalogSourcesRemainExplicitlyIndicative()
    {
        var trace = Check().CalculationTrace!;
        Assert.Contains("IndicativeThirdParty", trace);
        Assert.Contains("https://www.oppo.it/tabelle/profilati_ipe.htm", trace);
    }

    [Fact]
    public void WrongMemberProvenanceIsRejectedBeforeRule()
    {
        var r = Request();
        r = r with { Effects = r.Effects with { MemberId = "OTHER" } };
        Assert.Equal(DesignCheckStatus.InvalidInput, Check(r).Status);
    }

    [Fact]
    public void MultipleAssignmentsSameMemberCannotBeRegistered()
    {
        Assert.Throws<ArgumentException>(() => new ElasticBendingRuleM89A(Code, Sections, Materials,
            new[] { Assignment, Assignment }));
    }
}
