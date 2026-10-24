using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Verification.Integration;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.M90;

public sealed class SteelMixedISectionClassifierM90BTests
{
    private static readonly CommercialRolledSteelCatalogM88B2 Profiles =
        CommercialRolledSteelCatalogM88B2.Load(Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));
    private static readonly DesignCodeEdition Code =
        new("EN 1993-1-1", "2005+A1:2014", "IT-USER-DECLARED-TEST");
    private static SteelISectionDimensionsM90A Ipe600 => new(600,220,12,19,24);

    private static SteelMixedWebTransitionDomainM90B Domain() =>
        SteelMixedISectionClassifierM90B.ComputeStrongAxisWebTransitions(
            "IPE-600", "S275", Ipe600, 275, 156, 3069, 3512,
            "Bernuzzi 2018 A4.3 p136 geometry", "S275 nominal fy 275 MPa", "Bernuzzi A4.3 Wpl 3512 cm3");

    private static SteelMixedIClassificationM90B Classify(
        SteelISectionDimensionsM90A dims, double alpha, double psi, double fy=235) =>
        SteelMixedISectionClassifierM90B.ClassifyDeclaredStressParameters(
            "DECLARED-I/H", "S235", dims, fy,
            new(alpha, psi, "plastic stress block independently justified", "elastic gradient independently justified"),
            "geometry source", "fy source");

    private static DesignCheckRequest Request(double n=-100, double m=20, double v=5,
        string section="IPE-200", DesignCodeEdition? code=null, DesignEffectSnapshot? effects=null) =>
        new(new("M1", DesignMaterial.Steel, section, 4), DesignLimitState.Ultimate,
            new("TEST-NO-APPROVAL", code ?? Code, Array.Empty<NormativeCoefficient>()),
            effects ?? new DesignEffectSnapshot("FEM:linear-static:first-order", "ULS1",
                DesignEffectOrigin.FemAnalysis, 2, n, v, m)
            { MemberId="M1", TargetKind=DesignEffectTargetKind.LoadCombination,
              SectionSide=DesignEffectSectionSide.Continuous }, Array.Empty<string>());

    [Fact]
    public void BernuzziA43_PublishedWebTransitionNumbersAreRecoveredWithoutRoundingThemIntoFormulas()
    {
        // Bernuzzi 2018 pp. 136-138, A4.3: N12=794.16, M12=918.02,
        // N23=1131.91, M23=868.74, N34=3681.8, M34=120.09.
        // Book rounds intermediate epsilon and alpha; tolerance covers published rounding.
        var d=Domain();
        Assert.InRange(d.Class1To2.AxialCompressionKn,792,797);
        Assert.InRange(d.Class1To2.StrongAxisMomentKnm,916,920);
        Assert.InRange(d.Class2To3.AxialCompressionKn,1129,1135);
        Assert.InRange(d.Class2To3.StrongAxisMomentKnm,866,871);
        Assert.InRange(d.Class3To4.AxialCompressionKn,3678,3685);
        Assert.InRange(d.Class3To4.StrongAxisMomentKnm,118.5,121.5);
        Assert.Equal(1, d.FlangeClass);
        Assert.Equal(514d/12, d.WebSlenderness, 12);
        Assert.False(d.IsCompleteResistanceDomain);
        Assert.False(d.AllowsAutomaticApproval);
        Assert.Contains("Bernuzzi",d.Trace);
    }

    [Fact]
    public void TransitionPointsAreOrderedInN_AndDoNotPretendToSolveTheWholeDomain()
    {
        var d=Domain();
        Assert.True(d.Class1To2.AxialCompressionKn < d.Class2To3.AxialCompressionKn);
        Assert.True(d.Class2To3.AxialCompressionKn < d.Class3To4.AxialCompressionKn);
        Assert.True(d.Class1To2.StrongAxisMomentKnm > d.Class2To3.StrongAxisMomentKnm);
        Assert.True(d.Class2To3.StrongAxisMomentKnm > d.Class3To4.StrongAxisMomentKnm);
        Assert.InRange(d.Class1To2.AlphaOrPsi, .73,.75);
        Assert.InRange(d.Class2To3.AlphaOrPsi, .83,.84);
        Assert.InRange(d.Class3To4.AlphaOrPsi, .71,.73);
    }

    [Theory]
    [InlineData(33,1)]
    [InlineData(38,2)]
    [InlineData(42,3)]
    [InlineData(43,4)]
    public void AlphaOnePsiOne_RecoversPureCompressionTableLimits(double webSlenderness, int expected)
    {
        var dims=new SteelISectionDimensionsM90A(44+10*webSlenderness,100,10,10,12);
        // Flange c/t=(100-10-24)/(2*10)=3.3, class 1.
        var c=Classify(dims,1,1);
        Assert.Equal(expected,c.Web.Class);
        Assert.Equal(expected,c.Class);
        Assert.False(c.AllowsAutomaticApproval);
    }

    [Theory]
    [InlineData(72,1)]
    [InlineData(83,2)]
    [InlineData(124,3)]
    [InlineData(125,4)]
    public void AlphaHalfPsiMinusOne_RecoversPureBendingWebLimits(double webSlenderness, int expected)
    {
        var c=Classify(new(44+10*webSlenderness,100,10,10,12),.5,-1);
        Assert.Equal(expected,c.Web.Class);
    }

    [Fact]
    public void IntermediateMixedAlphaPsi_CanProduceClassTwoOrThreeWithoutMixingStates()
    {
        var d=new SteelISectionDimensionsM90A(600,220,12,19,24);
        Assert.Equal(2,Classify(d,.80,0,275).Class);
        Assert.Equal(3,Classify(d,.90,-.30,275).Class);
        Assert.Equal(4,Classify(new(550,210,6,14,18),.75,0,235).Class);
    }

    [Fact]
    public void ElasticPsiBelowMinusOne_UsesSecondBranchOfTable52()
    {
        var c=Classify(new(44+10*100,100,10,10,12),.5,-2,235);
        Assert.InRange(c.Web.Class3Limit,262,264);
        Assert.Equal(3,c.Web.Class); // limits 72, 83, approx. 263; web c/t=100 lies in class 3.
    }

    [Theory]
    [InlineData(0,0)]
    [InlineData(-.1,0)]
    [InlineData(1.01,0)]
    [InlineData(.5,1.01)]
    [InlineData(.5,double.NaN)]
    [InlineData(double.PositiveInfinity,0)]
    public void InvalidDeclaredStressParametersAreRejected(double alpha,double psi)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Classify(Ipe600,alpha,psi));
    }

    [Fact]
    public void StressParametersRequireTwoSeparateEvidenceSources()
    {
        Assert.Throws<ArgumentException>(() => SteelMixedISectionClassifierM90B.ClassifyDeclaredStressParameters(
            "X","S275",Ipe600,275,new(.7,-.5,"","from psi"),"geometry","material"));
    }

    [Fact]
    public void PhysicallyInconsistentAlphaPsiIsNotSilentlyLabeledValid()
    {
        // Would yield class-2 threshold larger than class-3 threshold.
        Assert.Throws<NotSupportedException>(()=>Classify(Ipe600,.5,1,275));
    }

    [Fact]
    public void MissingPlasticModulusSourceOrWrongModulusOrderIsRejected()
    {
        Assert.Throws<ArgumentException>(() => SteelMixedISectionClassifierM90B.ComputeStrongAxisWebTransitions(
            "X","S275",Ipe600,275,156,3069,3512,"geometry","fy",""));
        Assert.Throws<ArgumentException>(() => SteelMixedISectionClassifierM90B.ComputeStrongAxisWebTransitions(
            "X","S275",Ipe600,275,156,3512,3069,"geometry","fy","Wpl"));
    }

    [Fact]
    public void UnsupportedWebTransitionRegimeIsRejectedRatherThanExtrapolated()
    {
        Assert.Throws<NotSupportedException>(()=>SteelMixedISectionClassifierM90B.ComputeStrongAxisWebTransitions(
            "compact","S235",new(200,100,6,8,9),235,26,150,180,"shape","fy","Wpl"));
    }

    [Fact]
    public void ConcomitantFEM_UsesSameTargetStationAndBothSignedForces()
    {
        var r=Request();
        var c=SteelMixedISectionClassifierM90B.ScreenConcomitantElasticFEM(r,Profiles,Materials,"S235");
        Assert.Equal("M1",c.MemberId);
        Assert.Equal("IPE-200",c.SectionId);
        Assert.Equal("ULS1",c.TargetId);
        Assert.Equal(2,c.PositionMetres);
        Assert.Equal(-100,c.SignedAxialKn);
        Assert.Equal(20,c.SignedMomentKnm);
        Assert.Equal(DesignEffectTargetKind.LoadCombination,c.TargetKind);
        Assert.Equal(DesignEffectSectionSide.Continuous,c.Side);
        Assert.True(c.CompressionEdgeStressMpa > c.OppositeEdgeStressMpa);
        Assert.True(c.Psi <= 1);
        Assert.True(c.ConservativeClassUpperBound is 3 or 4);
        Assert.False(c.IsDefinitiveSectionClass);
        Assert.False(c.AllowsAutomaticApproval);
        Assert.Contains("V=5",c.Trace);
    }

    [Fact]
    public void ElasticScreenPreservesSign_AndChangesWithAxialCompression()
    {
        var compressed=SteelMixedISectionClassifierM90B.ScreenConcomitantElasticFEM(
            Request(n:-100),Profiles,Materials,"S235");
        var tensioned=SteelMixedISectionClassifierM90B.ScreenConcomitantElasticFEM(
            Request(n:100),Profiles,Materials,"S235");
        Assert.True(compressed.Psi > tensioned.Psi);
        Assert.Equal(20,tensioned.SignedMomentKnm);
    }

    [Fact]
    public void PointLoadLeftAndRightAreKeptDistinct()
    {
        var before=Request(effects:new DesignEffectSnapshot("FEM:first-order","G1",
            DesignEffectOrigin.FemAnalysis,1.5,-50,10,8)
        { MemberId="M1", TargetKind=DesignEffectTargetKind.LoadCase, SectionSide=DesignEffectSectionSide.BeforePointLoad });
        var after=before with { Effects=before.Effects with { SectionSide=DesignEffectSectionSide.AfterPointLoad,ShearForceYKn=-18 } };
        var a=SteelMixedISectionClassifierM90B.ScreenConcomitantElasticFEM(before,Profiles,Materials,"S235");
        var b=SteelMixedISectionClassifierM90B.ScreenConcomitantElasticFEM(after,Profiles,Materials,"S235");
        Assert.Equal(DesignEffectSectionSide.BeforePointLoad,a.Side);
        Assert.Equal(DesignEffectSectionSide.AfterPointLoad,b.Side);
        Assert.Contains("V=10",a.Trace);
        Assert.Contains("V=-18",b.Trace);
        Assert.Equal(a.Psi,b.Psi,12);
    }

    [Fact]
    public void WrongMemberOrUnsupportedEditionOrSectionAreRejected()
    {
        var mismatch=Request(effects:new DesignEffectSnapshot("FEM:first-order","ULS1",
            DesignEffectOrigin.FemAnalysis,2,-10,0,4)
        { MemberId="OTHER", TargetKind=DesignEffectTargetKind.LoadCombination,
          SectionSide=DesignEffectSectionSide.Continuous });
        Assert.Throws<ArgumentException>(()=>SteelMixedISectionClassifierM90B.ScreenConcomitantElasticFEM(mismatch,Profiles,Materials,"S235"));
        Assert.Throws<NotSupportedException>(()=>SteelMixedISectionClassifierM90B.ScreenConcomitantElasticFEM(
            Request(code:new("EN 1993-1-1","2022","IT")),Profiles,Materials,"S235"));
        Assert.Throws<NotSupportedException>(()=>SteelMixedISectionClassifierM90B.ScreenConcomitantElasticFEM(
            Request(section:"INP-200"),Profiles,Materials,"S235"));
    }

    [Theory]
    [InlineData(0,10,0)]
    [InlineData(-1,0,0)]
    [InlineData(double.NaN,10,0)]
    public void PureOrNonfiniteActionsAreNotPretendedMixed(double n,double m,double v)
    {
        Assert.Throws<NotSupportedException>(()=>SteelMixedISectionClassifierM90B.ScreenConcomitantElasticFEM(
            Request(n,m,v),Profiles,Materials,"S235"));
    }

    [Fact]
    public void MissingWebCompressionCannotBeInterpretedAsAutomaticPass()
    {
        Assert.Throws<NotSupportedException>(()=>SteelMixedISectionClassifierM90B.ScreenConcomitantElasticFEM(
            Request(n:1000,m:1),Profiles,Materials,"S235"));
    }

    [Fact]
    public void RealFrame2DFemCaptureFeedsOnlyOneAnalyzedTargetAtOneStation()
    {
        var solverSection=Profiles.Find("IPE-200").ToSolverSection();
        var model=new StructuralModel()
            .AddNode(new StructuralNode("A",0,0))
            .AddNode(new StructuralNode("B",4,0))
            .AddMaterial(new StructuralMaterial("MAT","Elastic steel",210_000_000))
            .AddSection(solverSection)
            .AddMember(new StructuralMember("M1","A","B","MAT","IPE-200",MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("G","Permanent"))
            .AddSupport(StructuralSupport.Fixed("S","A"))
            .AddLoad(StructuralLoad.NodalForce("NX","G","B",StructuralLoadDirection.GlobalX,-150))
            .AddLoad(StructuralLoad.NodalForce("NY","G","B",StructuralLoadDirection.GlobalY,-20));
        var captured=new FemStationVerificationBridgeM87B().Capture(
            model,new DesignMemberContext("M1",DesignMaterial.Steel,"IPE-200",4),
            DesignLimitState.Ultimate,
            new NormativeProfile("TEST-ONLY",Code,Array.Empty<NormativeCoefficient>()),
            Array.Empty<string>(),
            new FemStationSelectionM87B(StructuralAnalysisTargetKind.LoadCase,"G",2));
        var screened=SteelMixedISectionClassifierM90B.ScreenConcomitantElasticFEM(
            captured,Profiles,Materials,"S235");
        Assert.Equal("G",screened.TargetId);
        Assert.Equal(DesignEffectTargetKind.LoadCase,screened.TargetKind);
        Assert.Equal(2,screened.PositionMetres,8);
        Assert.Equal(captured.Effects.AxialForceKn!.Value,screened.SignedAxialKn,10);
        Assert.Equal(captured.Effects.BendingMomentZKnm!.Value,screened.SignedMomentKnm,10);
        Assert.True(Math.Abs(screened.SignedAxialKn)>1);
        Assert.True(Math.Abs(screened.SignedMomentKnm)>1);
        Assert.False(screened.AllowsAutomaticApproval);
    }

}
