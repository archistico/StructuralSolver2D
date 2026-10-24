using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.M91;

public sealed class SteelFlexuralBucklingRuleM91ATests
{
    private static readonly DesignCodeEdition Code = new("EN 1993-1-1", "2005+A1:2014", "IT-TEST-ANNEX-EXPLICIT");
    private static readonly CommercialRolledSteelCatalogM88B2 Sections = CommercialRolledSteelCatalogM88B2.Load(
        Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials = CharacteristicMaterialCatalogM88C.Load(
        Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));

    private static SteelFlexuralBucklingAssignmentM91A Assignment => new(
        "COL1", "HEB-200", "S235", 1,
        "Bernuzzi, Progetto e verifica strutture in acciaio, 2nd ed., A7.1, p.192",
        7.5, "Bernuzzi A7.1: effective major-axis length 7.5m, assumed braced frame",
        3.75, "Bernuzzi A7.1: mid-height lateral bracing; effectiveness externally verified",
        210000, "EN 1993-1-1 steel E=210000 MPa / Bernuzzi A7.1",
        true, true, true, true);

    private static DesignCheckRequest Request(double n=-1000, double v=0, double m=0,
        double gamma=1.05, string section="HEB-200", DesignCodeEdition? code=null) => new(
        new DesignMemberContext("COL1", DesignMaterial.Steel, section, 7.5),
        DesignLimitState.Ultimate,
        new NormativeProfile("TEST-NO-OVERALL-APPROVAL", code ?? Code,
            new[] {new NormativeCoefficient("gamma_M1", gamma,
                "TEST ONLY: explicitly chosen for Bernuzzi A7.1; verify National Annex in practice", "buckling partial factor")}),
        new DesignEffectSnapshot("FEM-1", "ULS-1", DesignEffectOrigin.FemAnalysis,
            3.75, n, v, m) { MemberId="COL1", TargetKind=DesignEffectTargetKind.LoadCombination,
                SectionSide=DesignEffectSectionSide.Continuous },
        new[] { SteelFlexuralBucklingRuleM91A.Id });

    private static DesignCheckResult Check(DesignCheckRequest? request=null,
        SteelFlexuralBucklingAssignmentM91A? assignment=null)
    {
        var rule = new SteelFlexuralBucklingRuleM91A(Code, Sections, Materials,
            new[] { assignment ?? Assignment });
        return Assert.Single(new DesignCheckEngine(new[] {rule}).Evaluate(request ?? Request()).Results);
    }

    [Fact]
    public void BernuzziA7_1_Heb200S235_BothAxes_AndGoverningStrongAgree()
    {
        var result = Check(Request(n:-1100));
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        // Bernuzzi A7.1 (p. 193): Ncr,y=2098.78; Ncr,z=2952.1 kN;
        // chi_y≈0.6387; chi_z≈0.6693; NbRd≈1116.1kN (printed interpolation).
        // OPPO A=78.08cm2 vs textbook 78.1cm2; exact EC3 chi avoids table interpolation.
        Assert.InRange(result.Resistance!.Value, 1115.5, 1116.6);
        Assert.Equal(1100d, result.Demand!.Value);
        Assert.Contains("curve=b", result.CalculationTrace!);
        Assert.Contains("curve=c", result.CalculationTrace!);
        Assert.Contains("governing=y-y", result.CalculationTrace!);
        Assert.Contains("Ncr=2098", result.CalculationTrace!);
        Assert.Contains("Ncr=2952", result.CalculationTrace!);
        Assert.Contains("NOT CHECKED", result.CalculationTrace!);
        Assert.Equal(DesignCheckStatus.Failed, Check(Request(n:-1200)).Status);
    }

    [Fact]
    public void BernuzziAxisCalculations_MatchEulerAndIndependentHandExpressions()
    {
        var y = SteelFlexuralBucklingRuleM91A.CalculateAxis("y-y", "b", 7.5, 5696, 78.08, 235, 210000, 1.05)!;
        var z = SteelFlexuralBucklingRuleM91A.CalculateAxis("z-z", "c", 3.75, 2003, 78.08, 235, 210000, 1.05)!;
        Assert.InRange(y.EulerCriticalForceKn, 2098.77, 2098.79);
        Assert.InRange(z.EulerCriticalForceKn, 2952.13, 2952.15);
        Assert.InRange(y.NonDimensionalSlenderness, 0.9349, 0.9352);
        Assert.InRange(z.NonDimensionalSlenderness, 0.7882, 0.7886);
        Assert.InRange(y.ReductionFactor, 0.6385, 0.6389);
        Assert.InRange(z.ReductionFactor, 0.6692, 0.6697);
        Assert.True(y.BucklingResistanceKn < z.BucklingResistanceKn);
    }

    [Theory]
    [InlineData(200,100,8.5,"a","b")]
    [InlineData(200,200,15,"b","c")]
    [InlineData(200,150,40,"a","b")]
    [InlineData(200,150,50,"b","c")]
    [InlineData(200,200,100,"b","c")]
    [InlineData(200,200,101,"d","d")]
    public void RolledICurveSelection_MatchesEC3Table6_2(
        double h,double b,double tf,string expectedY,string expectedZ)
    {
        var curves=SteelFlexuralBucklingRuleM91A.SelectRolledICurves(h,b,tf);
        Assert.Equal(expectedY, curves.Strong);
        Assert.Equal(expectedZ, curves.Weak);
    }

    [Fact]
    public void ShortColumn_ChisAreCappedAtUnity()
    {
        var axis=SteelFlexuralBucklingRuleM91A.CalculateAxis("y-y", "b", 0.01, 5696, 78.08, 235, 210000, 1.05);
        Assert.NotNull(axis);
        Assert.InRange(axis!.ReductionFactor, 0, 1);
        Assert.True(axis.ReductionFactor > 0.9999);
    }

    [Fact]
    public void MissingEitherBucklingLengthOrItsEvidence_IsNeverPassed()
    {
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {EffectiveLengthWeakAxisMetres=null}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {StrongAxisLengthEvidence=""}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {EffectiveLengthStrongAxisMetres=0}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {EffectiveLengthWeakAxisMetres=double.NaN}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {YoungModulusMpa=double.PositiveInfinity}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {YoungModulusEvidence=null}).Status);
    }

    [Fact]
    public void NoAutomaticOutOfPlaneRestraint_WhenFEMMemberDefinesOnlyInPlaneLength()
    {
        var request=Request();
        request=request with {Member=request.Member with {UnbracedLengthInPlaneMetres=7.5,IsLaterallyRestrained=true}};
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(request,Assignment with {EffectiveLengthWeakAxisMetres=null}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(request,Assignment with {StrongWeakAxisMappingConfirmed=null}).Status);
    }

    [Fact]
    public void MixedActionsOrTension_AreNotApproximatedByPureBuckling()
    {
        Assert.Equal(DesignCheckStatus.NotApplicable,Check(Request(n:1000)).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,Check(Request(n:0)).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,Check(Request(v:0.01)).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,Check(Request(m:0.01)).Status);
    }

    [Fact]
    public void Class4AndUnclassifiedSection_AreRejected()
    {
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(assignment:Assignment with {CompressionSectionClass=4}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {CompressionSectionClass=null}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {ClassificationEvidence=""}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {UniformConcentricCompressionConfirmed=false}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {CompressionSectionClass=2}).Status);
    }

    [Fact]
    public void EditionAndGammaM1AreExplicit_NotBorrowedFromGammaM0()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Check(Request(gamma:0)).Status);
        var request=Request();
        request=request with {Profile=request.Profile with {Coefficients=new[] {
            new NormativeCoefficient("gamma_M0",1,"not gamma M1","wrong partial coefficient")}}};
        Assert.Equal(DesignCheckStatus.MissingData, Check(request).Status);
        var edition=new DesignCodeEdition("EN 1993-1-1","2022","IT-TEST-ANNEX-EXPLICIT");
        Assert.Equal(DesignCheckStatus.Unsupported, Check(Request(code:edition)).Status);
        Assert.Throws<NotSupportedException>(() =>
            new SteelFlexuralBucklingRuleM91A(edition,Sections,Materials,new[]{Assignment}));
    }

    [Fact]
    public void CatalogueAndMaterialScopeAreStrict()
    {
        Assert.Equal(DesignCheckStatus.MissingData,Check(Request(section:"IPE-200")).Status);
        Assert.Equal(DesignCheckStatus.MissingData,Check(assignment:Assignment with {SteelGrade="S999"}).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(Request(section:"INP-200"),Assignment with {SectionId="INP-200"}).Status);
    }

    [Fact]
    public void FEMProvenanceAndSourceEvidenceAreRequired()
    {
        var request=Request();
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(request with {Effects=request.Effects with {TargetKind=null}}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(request with {Effects=request.Effects with {Origin=DesignEffectOrigin.ExternalManual}}).Status);
        Assert.Equal(DesignCheckStatus.InvalidInput,
            Check(request with {Effects=request.Effects with {MemberId="OTHER"}}).Status);
        Assert.Equal(DesignCheckStatus.MissingData,
            Check(assignment:Assignment with {NoRelevantHolesConfirmed=null}).Status);
    }

    [Fact]
    public void APassingBucklingCheck_DoesNotApproveTorsionalOrWholeMemberStability()
    {
        var rule=new SteelFlexuralBucklingRuleM91A(Code,Sections,Materials,new[]{Assignment});
        Assert.False(rule.EstablishesCompleteMemberCompliance);
        var request=Request();
        var report=new DesignCheckEngine(new[]{rule}).Evaluate(request with {
            RuleIds=new[]{SteelFlexuralBucklingRuleM91A.Id,"EC3-TORSIONAL-FLEXURAL-BUCKLING-NOT-IMPLEMENTED"}});
        Assert.Equal(DesignCheckStatus.Passed,report.Results[0].Status);
        Assert.Equal(DesignCheckStatus.Unsupported,report.Results[1].Status);
        Assert.Equal(DesignOverallStatus.Incomplete,report.OverallStatus);
    }

    [Fact]
    public void InvalidAxisArgumentsCannotCreateResistance()
    {
        Assert.Null(SteelFlexuralBucklingRuleM91A.CalculateAxis("z-z","d",double.NaN,2003,78.08,235,210000,1.05));
        Assert.Null(SteelFlexuralBucklingRuleM91A.CalculateAxis("z-z","unknown",3.75,2003,78.08,235,210000,1.05));
        Assert.Null(SteelFlexuralBucklingRuleM91A.CalculateAxis("z-z","c",3.75,2003,-1,235,210000,1.05));
        Assert.Null(SteelFlexuralBucklingRuleM91A.CalculateAxis("z-z","c",3.75,2003,78.08,235,210000,0));
    }

    [Fact]
    public void DuplicateMemberAssignmentsAreNotSilentlyOverwritten()
    {
        Assert.Throws<ArgumentException>(()=>new SteelFlexuralBucklingRuleM91A(Code,Sections,Materials,
            new[]{Assignment,Assignment}));
    }
}
