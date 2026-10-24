using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.M90;

public sealed class SteelClass3ElasticNmvRuleM90CTests
{
    private static readonly DesignCodeEdition Code = new("EN 1993-1-1", "2005+A1:2014", "IT-USER-TEST-ONLY");
    private static readonly CommercialRolledSteelCatalogM88B2 Sections = CommercialRolledSteelCatalogM88B2.Load(
        Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials = CharacteristicMaterialCatalogM88C.Load(
        Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));
    private const double Av = 13.996;
    private static readonly SteelClass3NmvAssignmentM90C Assignment = new(
        "M1", "IPE-200", "S235", 3,
        "DECLARED TEST classification (independent engineering check required)",
        true, true, true, Av, "TEST ONLY: declared Av in cm2", true, true);

    private static DesignCheckRequest Request(double n=-100, double m=20, double v=10,
        DesignCodeEdition? code=null, double gamma=1, string section="IPE-200") => new(
        new DesignMemberContext("M1", DesignMaterial.Steel, section, 5),
        DesignLimitState.Ultimate,
        new NormativeProfile("M90C-NOT-OVERALL-COMPLIANCE", code ?? Code,
            new[] { new NormativeCoefficient("gamma_M0",gamma,"USER EXPLICIT TEST VALUE","Not a default") }),
        new DesignEffectSnapshot("FEM-ANALYSIS-1", "ULS-1", DesignEffectOrigin.FemAnalysis,
            2.5, n, v, m)
        { MemberId="M1", TargetKind=DesignEffectTargetKind.LoadCombination,
          SectionSide=DesignEffectSectionSide.Continuous },
        new[] { SteelClass3ElasticNmvRuleM90C.Id });

    private static DesignCheckResult Check(DesignCheckRequest? request=null,
        SteelClass3NmvAssignmentM90C? assignment=null)
    {
        var rule = new SteelClass3ElasticNmvRuleM90C(Code,Sections,Materials,
            new[] { assignment ?? Assignment });
        return Assert.Single(new DesignCheckEngine(new[] { rule }).Evaluate(request ?? Request()).Results);
    }

    [Fact]
    public void Ipe200Class3_NMvElasticInteraction_EqualsHandCalculatedStress()
    {
        var check=Check();
        // sigma_max = 100000/2848 + 20e6/194300 = 35.112...+102.933... MPa
        var expected=100d*10/28.48 + 20d*1000/194.3;
        Assert.Equal(DesignCheckStatus.Passed, check.Status);
        Assert.InRange(check.Demand!.Value,expected-1e-10,expected+1e-10);
        Assert.Equal(235d,check.Resistance!.Value);
        Assert.InRange(check.Utilization!.Value,expected/235-1e-12,expected/235+1e-12);
        Assert.Contains("N=-100kN",check.CalculationTrace!);
        Assert.Contains("V=10kN",check.CalculationTrace!);
        Assert.Contains("M=20kNm",check.CalculationTrace!);
        Assert.Contains("NOT CHECKED",check.CalculationTrace!);
    }

    [Theory]
    [InlineData(-100,20)]
    [InlineData(100,20)]
    [InlineData(-100,-20)]
    [InlineData(100,-20)]
    public void SignsRemainConcomitant_AndAbsoluteEdgeStressIsConservative(double n,double m)
    {
        var r=Check(Request(n,m));
        Assert.Equal(DesignCheckStatus.Passed,r.Status);
        var expected=Math.Abs(n)*10/28.48 + Math.Abs(m)*1000/194.3;
        Assert.InRange(r.Demand!.Value,expected-1e-10,expected+1e-10);
    }

    [Fact]
    public void ActionJustBelowCapacityPasses_HigherActionFails()
    {
        var limitM=(235d-100*10/28.48)*194.3/1000;
        var exactly=Check(Request(-100,limitM-1e-9,0));
        Assert.Equal(DesignCheckStatus.Passed,exactly.Status);
        Assert.Equal(DesignCheckStatus.Failed,Check(Request(-100,limitM+0.1,0)).Status);
    }

    [Fact]
    public void UserGammaIsAppliedToStressCapacityNotToAppliedAction()
    {
        var r=Check(Request(gamma:1.25));
        Assert.Equal(DesignCheckStatus.Passed,r.Status);
        Assert.Equal(188d,r.Resistance!.Value);
        Assert.Contains("gamma_M0=1.25",r.CalculationTrace!);
    }

    [Fact]
    public void ShearExactlyHalfVplIsPermittedButAboveHalfIsNotExtrapolated()
    {
        double vpl=Av*235/(10*Math.Sqrt(3));
        Assert.Equal(DesignCheckStatus.Passed,Check(Request(v:vpl*0.5)).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,Check(Request(v:vpl*0.51)).Status);
    }

    [Fact]
    public void ZeroShearDoesNotRequireAvButAllConcomitantComponentsRemainPresent()
    {
        var assn=Assignment with { ShearAreaCm2=null,ShearAreaEvidence=null,
            InPlaneShearAxisMappingConfirmed=null, ShearBucklingExcludedConfirmed=null };
        Assert.Equal(DesignCheckStatus.Passed,Check(Request(v:0),assn).Status);
        Assert.Equal(DesignCheckStatus.MissingData,Check(Request(v:1),assn).Status);
        var missing= Request(v:0);
        missing=missing with { Effects = missing.Effects with { ShearForceYKn=null } };
        Assert.Equal(DesignCheckStatus.MissingData,Check(missing).Status);
    }

    [Fact]
    public void ShearRequiresVerifiedAreaOrientationAndBucklingExclusion()
    {
        Assert.Equal(DesignCheckStatus.MissingData,Check(assignment:Assignment with { ShearAreaCm2=null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,Check(assignment:Assignment with { ShearAreaCm2=0 }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,Check(assignment:Assignment with { ShearAreaCm2=100 }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,Check(assignment:Assignment with { ShearAreaEvidence="" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,Check(assignment:Assignment with { InPlaneShearAxisMappingConfirmed=false }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,Check(assignment:Assignment with { ShearBucklingExcludedConfirmed=null }).Status);
    }

    [Fact]
    public void SectionClassificationAndMappingMustBeExplicit()
    {
        Assert.Equal(DesignCheckStatus.MissingData,Check(assignment:Assignment with { SectionClass=null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,Check(assignment:Assignment with { ClassificationEvidence="" }).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,Check(assignment:Assignment with { SectionClass=1 }).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,Check(assignment:Assignment with { SectionClass=2 }).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,Check(assignment:Assignment with { SectionClass=4 }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,Check(assignment:Assignment with { StrongAxisMappingConfirmed=null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,Check(assignment:Assignment with { NoRelevantHolesConfirmed=false }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,Check(assignment:Assignment with { NoTorsionConfirmed=null }).Status);
    }

    [Theory]
    [InlineData(-100,0)]
    [InlineData(0,20)]
    public void PureActionIsNotSubstitutedForM89(double n,double m)
    {
        Assert.Equal(DesignCheckStatus.NotApplicable,Check(Request(n,m)).Status);
    }

    [Fact]
    public void NoMixedEnvelopeOrUntraceableFEMStationIsAccepted()
    {
        var r=Request();
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects=r.Effects with { TargetKind=null } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects=r.Effects with { SectionSide=null } }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(r with { Effects=r.Effects with { Origin=DesignEffectOrigin.ExternalManual } }).Status);
        Assert.Equal(DesignCheckStatus.InvalidInput,
            Check(r with { Effects=r.Effects with { MemberId="M2" } }).Status);
    }

    [Fact]
    public void PointLoadBeforeAfterSidesRemainSeparateWithDifferentShear()
    {
        var r=Request(v:12);
        r=r with { Effects=r.Effects with { SectionSide=DesignEffectSectionSide.BeforePointLoad } };
        var a=Check(r);
        var b=Check(r with { Effects=r.Effects with { SectionSide=DesignEffectSectionSide.AfterPointLoad,
            ShearForceYKn=-20 } });
        Assert.Equal(DesignCheckStatus.Passed,a.Status);
        Assert.Equal(DesignCheckStatus.Passed,b.Status);
        Assert.Contains("side=BeforePointLoad",a.CalculationTrace!);
        Assert.Contains("side=AfterPointLoad",b.CalculationTrace!);
        Assert.Contains("V=-20kN",b.CalculationTrace!);
        Assert.Equal(a.Demand,b.Demand);
    }

    [Fact]
    public void UnknownProfileOrGradeAndWrongSectionAreFailClosed()
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with { SteelGrade="S999" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(section:"IPE-240")).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(Request(section:"IPE-999"),Assignment with { SectionId="IPE-999" }).Status);
    }

    [Fact]
    public void InpWithInclinedFlangesCannotBeClassifiedAsParallelISection()
    {
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(Request(section:"INP-200"),Assignment with { SectionId="INP-200" }).Status);
    }

    [Fact]
    public void InvalidCoefficientOrEditionCanNeverReturnPass()
    {
        var r=Request();
        Assert.Equal(DesignCheckStatus.MissingData,Check(Request(gamma:0)).Status);
        var other=new DesignCodeEdition("EN 1993-1-1","2022","IT-USER-TEST-ONLY");
        Assert.Equal(DesignCheckStatus.Unsupported,Check(Request(code:other)).Status);
        Assert.Throws<NotSupportedException>(()=>new SteelClass3ElasticNmvRuleM90C(other,
            Sections,Materials,new[]{Assignment}));
        var noGamma=r with { Profile=r.Profile with { Coefficients=Array.Empty<NormativeCoefficient>() } };
        Assert.Equal(DesignCheckStatus.MissingData,Check(noGamma).Status);
    }

    [Fact]
    public void NoGlobalMemberComplianceIsInferredFromSingleRule()
    {
        var rule=new SteelClass3ElasticNmvRuleM90C(Code,Sections,Materials,new[]{Assignment});
        Assert.False(rule.EstablishesCompleteMemberCompliance);
        var r=Request();
        var report=new DesignCheckEngine(new[]{rule}).Evaluate(r with {
            RuleIds=new[]{SteelClass3ElasticNmvRuleM90C.Id,"EC3-BUCKLING-NOT-IMPLEMENTED"}});
        Assert.Equal(DesignCheckStatus.Passed,report.Results[0].Status);
        Assert.Equal(DesignCheckStatus.Unsupported,report.Results[1].Status);
        Assert.Equal(DesignOverallStatus.Incomplete,report.OverallStatus);
    }

    [Fact]
    public void DuplicateAssignmentsCannotBeRegistered()
    {
        Assert.Throws<ArgumentException>(()=>new SteelClass3ElasticNmvRuleM90C(Code,
            Sections,Materials,new[]{Assignment,Assignment}));
    }
}
