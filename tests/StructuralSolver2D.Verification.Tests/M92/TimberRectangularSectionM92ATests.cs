using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Timber;

namespace StructuralSolver2D.Verification.Tests.M92;

public sealed class TimberRectangularSectionM92ATests
{
    private static readonly DesignCodeEdition Code = new("EN 1995-1-1",
        TimberRectangularSectionRuleM92A.Edition, "IT-TEST-ANNEX-EXPLICIT");
    private static readonly CharacteristicMaterialCatalogM88C Catalog =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory,
            "characteristic-strength-classes.json"));

    private static TimberRectangularAssignmentM92A Assignment(
        string grade = "C24", string target = "ULS-A", double width = 100, double depth = 200,
        int? service = 1, TimberLoadDurationM92A? duration = TimberLoadDurationM92A.MediumTerm,
        bool? intact = true) =>
        new("W1", "RECT-100x200", grade, width, depth, "Test geometry: measured b/h",
            service, duration, "External TEST service classification", "External TEST duration assessment",
            "CALC-1", target, DesignEffectTargetKind.LoadCombination,
            true, true, intact, true, true);

    private static NormativeProfile Profile(bool kmod = true, bool gamma = true, bool kcr = true,
        double kmodValue = 0.8, double gammaValue = 1.3, double crackValue = 0.67)
    {
        var coeff = new List<NormativeCoefficient>();
        if (kmod) coeff.Add(new("kmod", kmodValue, "TEST - externally declared service/duration basis", "No automatic normative value"));
        if (gamma) coeff.Add(new("gamma_M", gammaValue, "TEST - external Annex value", "Test only"));
        if (kcr) coeff.Add(new("kcr", crackValue, "TEST - external crack allowance", "Test only"));
        return new NormativeProfile("EC5-M92A-TEST", Code, coeff);
    }

    private static DesignCheckReport Evaluate(TimberPureEffectM92A effect,
        double n = 0, double v = 0, double m = 0,
        TimberRectangularAssignmentM92A? assignment = null,
        NormativeProfile? profile = null, string target = "ULS-A",
        DesignMaterial material = DesignMaterial.Timber,
        double station = 1.5)
    {
        var rules = new[] {new TimberRectangularSectionRuleM92A(Code, effect, Catalog,
            new[] {assignment ?? Assignment()})};
        var request = new DesignCheckRequest(
            new DesignMemberContext("W1", material, "RECT-100x200", 3),
            DesignLimitState.Ultimate, profile ?? Profile(),
            new DesignEffectSnapshot("CALC-1", target, DesignEffectOrigin.FemAnalysis, station, n, v, m)
            {
                MemberId = "W1", TargetKind = DesignEffectTargetKind.LoadCombination,
                SectionSide = DesignEffectSectionSide.Continuous
            }, new[] {rules[0].RuleId});
        return new DesignCheckEngine(rules).Evaluate(request);
    }
    private static DesignCheckResult Single(TimberPureEffectM92A effect, double n=0,
        double v=0, double m=0, TimberRectangularAssignmentM92A? a=null,
        NormativeProfile? p=null, string target="ULS-A") =>
        Assert.Single(Evaluate(effect, n, v, m, a, p, target).Results);

    [Theory]
    [InlineData(TimberPureEffectM92A.TensionParallel, 100, 0, 0, 5, 14.5 * 0.8 / 1.3)]
    [InlineData(TimberPureEffectM92A.CompressionParallel, -100, 0, 0, 5, 21 * 0.8 / 1.3)]
    [InlineData(TimberPureEffectM92A.StrongAxisBending, 0, 0, 6, 9, 24 * 0.8 / 1.3)]
    [InlineData(TimberPureEffectM92A.InPlaneShear, 0, 20, 0, 1.5 * 20000 / (0.67 * 20000), 4 * 0.8 / 1.3)]
    public void PureRectangularC24Tests_AgreeWithHandCalculations(TimberPureEffectM92A effect,
        double n, double v, double m, double stress, double capacity)
    {
        var result = Single(effect, n, v, m);
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.Equal(stress, Assert.IsType<double>(result.Demand), 10);
        Assert.Equal(capacity, Assert.IsType<double>(result.Resistance), 10);
        Assert.Contains("duration=MediumTerm", Assert.IsType<string>(result.CalculationTrace));
        Assert.Contains("NOT checked", Assert.IsType<string>(result.CalculationTrace));
        Assert.Equal("MPa", result.Unit);
    }

    [Fact]
    public void ExcessBendingStress_IsReportedAsFailed_NotIncomplete()
    {
        var report = Evaluate(TimberPureEffectM92A.StrongAxisBending, m: 12);
        Assert.Equal(DesignOverallStatus.Failed, report.OverallStatus);
        Assert.Equal(DesignCheckStatus.Failed, Assert.Single(report.Results).Status);
    }

    [Fact]
    public void SolidTimberAndHomogeneousGlulamUseDifferentStrengths()
    {
        var solid = Single(TimberPureEffectM92A.TensionParallel, n:100);
        var glulam = Single(TimberPureEffectM92A.TensionParallel, n:100, a: Assignment("GL24h"));
        Assert.Equal(DesignCheckStatus.Passed, glulam.Status);
        Assert.True(glulam.Resistance > solid.Resistance);
        Assert.Equal(19.2 * 0.8 / 1.3, Assert.IsType<double>(glulam.Resistance), 10);
    }

    [Theory]
    [InlineData(TimberPureEffectM92A.TensionParallel, -10, 0, 0)]
    [InlineData(TimberPureEffectM92A.CompressionParallel, 10, 0, 0)]
    [InlineData(TimberPureEffectM92A.StrongAxisBending, 0, 0, 0)]
    [InlineData(TimberPureEffectM92A.InPlaneShear, 0, 0, 0)]
    public void WrongSignOrZeroAction_IsNotAPass(TimberPureEffectM92A effect,double n,double v,double m)
    {
        Assert.Equal(DesignCheckStatus.NotApplicable, Single(effect,n,v,m).Status);
    }

    [Theory]
    [InlineData(TimberPureEffectM92A.TensionParallel, 100, 1, 0)]
    [InlineData(TimberPureEffectM92A.CompressionParallel, -100, 0, 2)]
    [InlineData(TimberPureEffectM92A.StrongAxisBending, 10, 0, 4)]
    [InlineData(TimberPureEffectM92A.InPlaneShear, 0, 10, 2)]
    public void ConcurrentMixedActionsCannotPassPureRule(TimberPureEffectM92A effect, double n,double v,double m)
    {
        Assert.Equal(DesignCheckStatus.NotApplicable, Single(effect,n,v,m).Status);
    }

    [Fact]
    public void MissingKmodOrGammaNeverPasses()
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.TensionParallel,n:10,p:Profile(kmod:false)).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.TensionParallel,n:10,p:Profile(gamma:false)).Status);
    }

    [Theory]
    [InlineData(-0.1,1.3)]
    [InlineData(0,1.3)]
    [InlineData(2.1,1.3)]
    [InlineData(0.8,0)]
    public void InvalidCoefficientsNeverReturnPass(double kmod, double gamma)
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.TensionParallel,n:10,p:Profile(kmodValue:kmod,gammaValue:gamma)).Status);
    }

    [Fact]
    public void InstantaneousKmodAboveOneIsNotRejectedByArbitraryUnitCeiling()
    {
        var a = Assignment(duration:TimberLoadDurationM92A.Instantaneous);
        var result = Single(TimberPureEffectM92A.TensionParallel,n:10,a:a,p:Profile(kmodValue:1.1));
        Assert.Equal(DesignCheckStatus.Passed,result.Status);
    }

    [Fact]
    public void ShearRequiresExplicitCrackFactorAndDirection()
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.InPlaneShear,v:10,p:Profile(kcr:false)).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.InPlaneShear,v:10,p:Profile(crackValue:0)).Status);
        var noAxis = Assignment() with {ShearDirectionConfirmed=false};
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.InPlaneShear,v:10,a:noAxis).Status);
    }

    [Fact]
    public void ServiceClassAndDurationEvidenceAreMandatory()
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.StrongAxisBending,m:1,a:Assignment(service:null)).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.StrongAxisBending,m:1,a:Assignment(duration:null)).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.StrongAxisBending,m:1,a:Assignment() with {LoadDurationEvidence=""}).Status);
    }

    [Fact]
    public void DamagedSectionOrUnknownClassNeedsFurtherEvidence()
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.TensionParallel,n:1,a:Assignment(intact:false)).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.TensionParallel,n:1,a:Assignment(grade:"UNKNOWN")).Status);
    }

    [Theory]
    [InlineData(0,200)]
    [InlineData(100,-1)]
    [InlineData(double.NaN,200)]
    public void InvalidRectangularGeometryIsRejected(double b,double h)
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.TensionParallel,n:10,a:Assignment(width:b,depth:h)).Status);
    }

    [Fact]
    public void DifferentCombinationDoesNotReuseOtherCombinationKmod()
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.TensionParallel,n:10,target:"ULS-B").Status);
        var correct = Assignment(target:"ULS-B",duration:TimberLoadDurationM92A.ShortTerm);
        Assert.Equal(DesignCheckStatus.Passed,
            Single(TimberPureEffectM92A.TensionParallel,n:10,a:correct,target:"ULS-B").Status);
    }

    [Fact]
    public void DuplicateMemberTargetAssignmentsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new TimberRectangularSectionRuleM92A(Code,
            TimberPureEffectM92A.TensionParallel,Catalog,new[]{Assignment(),Assignment()}));
    }

    [Fact]
    public void OtherMaterialCannotUseTimberRules()
    {
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Assert.Single(Evaluate(TimberPureEffectM92A.TensionParallel,n:10,material:DesignMaterial.Steel).Results).Status);
    }

    [Fact]
    public void OtherEc5EditionAndNtcAreNotSilentlyImplemented()
    {
        Assert.Throws<NotSupportedException>(() => new TimberRectangularSectionRuleM92A(
            new("EN 1995-1-1","2025","IT"),TimberPureEffectM92A.TensionParallel,Catalog,new[]{Assignment()}));
        Assert.Throws<NotSupportedException>(() => new TimberRectangularSectionRuleM92A(
            new("NTC","2018","IT"),TimberPureEffectM92A.TensionParallel,Catalog,new[]{Assignment()}));
    }

    [Fact]
    public void OtherTimberChecksRemainUnsupportedAndOverallIncomplete()
    {
        var rule = new TimberRectangularSectionRuleM92A(Code,TimberPureEffectM92A.TensionParallel,Catalog,new[]{Assignment()});
        var request = new DesignCheckRequest(new("W1",DesignMaterial.Timber,"RECT-100x200",3),
            DesignLimitState.Ultimate,Profile(),new("CALC-1","ULS-A",DesignEffectOrigin.FemAnalysis,1.5,10,0,0)
            {MemberId="W1",TargetKind=DesignEffectTargetKind.LoadCombination,SectionSide=DesignEffectSectionSide.Continuous},
            new[]{rule.RuleId,"EC5-BUCKLING-NOT-IMPLEMENTED"});
        var report = new DesignCheckEngine(new[]{rule}).Evaluate(request);
        Assert.Equal(DesignOverallStatus.Incomplete,report.OverallStatus);
        Assert.Contains(report.Results,x => x.Status == DesignCheckStatus.Unsupported);
    }

    [Fact]
    public void NonfiniteLoadCannotBeReportedAsPass()
    {
        Assert.Equal(DesignCheckStatus.InvalidInput,
            Single(TimberPureEffectM92A.StrongAxisBending,m:double.PositiveInfinity).Status);
    }

    [Fact]
    public void NullEvidenceDoesNotImplyFavourableSizeFactor()
    {
        var a = Assignment() with {NoStrengthEnhancementClaimed=null};
        Assert.Equal(DesignCheckStatus.MissingData,
            Single(TimberPureEffectM92A.TensionParallel,n:10,a:a).Status);
    }
}
