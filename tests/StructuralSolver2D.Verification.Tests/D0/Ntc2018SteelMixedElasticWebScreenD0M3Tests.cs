using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D0;

public sealed class Ntc2018SteelMixedElasticWebScreenD0M3Tests
{
    private static DesignCheckRequest Request(double n = -100, double m = 10,
        double v = 0, string section = "I-EXAMPLE") => new(
        new DesignMemberContext("M1", DesignMaterial.Steel, section, 4),
        DesignLimitState.Ultimate, Ntc2018SteelTensionProfileD1.Create(),
        new DesignEffectSnapshot("FEM-A1", "ULS-1", DesignEffectOrigin.FemAnalysis,
            2, n, v, m)
        {
            MemberId = "M1", TargetKind = DesignEffectTargetKind.LoadCombination,
            SectionSide = DesignEffectSectionSide.Continuous
        }, Array.Empty<string>());

    private static Ntc2018SteelMixedClassificationEvidenceD0M1 Evidence() => new(
        UnperforatedConfirmed: true,
        HotRolledParallelFlangeDoublySymmetricConfirmed: true,
        StrongAxisMappedToFemMomentConfirmed: true,
        GeometryEvidence: "Independent shop drawing and nonperforated rolled I/H proof",
        GradeAndYieldEvidence: "Independent NTC 2018 steel yield grade confirmation");

    // h=224, tf=8, r=4 => web flat=200, edge y=100 mm.
    // A=10000 mm2, Ix=50,000,000 mm4 => C=-N/10 MPa and B=2|M| MPa.
    private static Ntc2018SteelISectionDimensionsD0 Dimensions(double tw = 10) =>
        new(224, 100, tw, 8, 4);

    private static Ntc2018SteelGrossElasticPropertiesD0M3 Properties() =>
        new(10000, 50_000_000, "Independent gross area and strong-axis second moment");

    private static Ntc2018SteelMixedElasticWebScreenD0M3 Screen(
        DesignCheckRequest? request = null,
        Ntc2018SteelTensileStrainEvidenceD0M3? strain = null,
        Ntc2018SteelGrossElasticPropertiesD0M3? properties = null,
        Ntc2018SteelISectionDimensionsD0? dimensions = null,
        Ntc2018SteelMixedClassificationEvidenceD0M1? evidence = null) =>
        Ntc2018SteelMixedElasticWebScreenerD0M3.ScreenGeometry(
            request ?? Request(), dimensions ?? Dimensions(), properties ?? Properties(),
            "S235", 235, evidence ?? Evidence(), strain);

    [Fact]
    public void AxialCompressionAndBending_DeriveElasticPsiWithoutManualAlpha()
    {
        // N=-100 kN -> C=10 MPa; M=10 kNm -> B=20 MPa.
        var r = Screen();
        Assert.Equal(30d, r.CompressiveEdgeStressMpa, 11);
        Assert.Equal(-10d, r.OppositeEdgeStressMpa, 11);
        Assert.Equal(-1d / 3d, r.Psi, 11);
        Assert.Equal(42d / (0.67 - 0.11), r.Class3Limit, 11);
        Assert.Equal(20d, r.WebSlenderness, 11);
        Assert.Equal(3, r.ConservativeWebClassUpperBound);
        Assert.Null(r.FootnoteStressAlternativeSatisfied);
        Assert.False(r.EstablishesDefinitiveSectionClass);
        Assert.False(r.EstablishesPlasticClassesOneOrTwo);
        Assert.False(r.ProvidesSectionResistanceOrNmInteraction);
        Assert.False(r.EstablishesCompleteMemberCompliance);
        Assert.Contains("WEB ELASTIC SCREEN ONLY", r.Trace);
        Assert.Contains("FEM=FEM-A1/ULS-1", r.Trace);
    }

    [Fact]
    public void ReversingStrongMomentSignLeavesConservativeWebBoundaryIdentical()
    {
        var positive = Screen(Request(m: 10));
        var negative = Screen(Request(m: -10));
        Assert.Equal(positive.Psi, negative.Psi);
        Assert.Equal(positive.Class3Limit, negative.Class3Limit);
        Assert.Equal(-10d, negative.SignedMomentKnm);
    }

    [Fact]
    public void NetTensionWithCompressedWebEdge_UsesNtcSpecialPsiBranch()
    {
        // N=+100 kN -> C=-10 MPa; M=10 kNm -> B=20 MPa.
        // sigmaC=10, sigmaOpp=-30; psi=-3, first footnote condition holds.
        var r = Screen(Request(n: 100));
        Assert.Equal(10d, r.CompressiveEdgeStressMpa, 11);
        Assert.Equal(-30d, r.OppositeEdgeStressMpa, 11);
        Assert.Equal(-3d, r.Psi, 11);
        Assert.Equal(62d * 4d * Math.Sqrt(3d), r.Class3Limit, 11);
        Assert.Equal(true, r.FootnoteStressAlternativeSatisfied);
        Assert.Null(r.FootnoteStrainAlternativeSatisfied);
        Assert.Contains("psi<-1", r.Trace);
        Assert.False(r.EstablishesCompleteMemberCompliance);
    }

    [Fact]
    public void SpecialBranchCompressionAboveYield_RequiresIndependentlyProvenStrain()
    {
        // C=-300 MPa, B=600 MPa -> sigmaC=300, psi=-3.
        var request = Request(n: 3000, m: 300);
        Assert.Throws<NotSupportedException>(() => Screen(request));
        Assert.Throws<NotSupportedException>(() => Screen(request,
            new(235d / 210000d, 210000, "Exact tensile yield strain")));
        var r = Screen(request, new(0.003, 210000, "Measured tensile strain and E"));
        Assert.Equal(false, r.FootnoteStressAlternativeSatisfied);
        Assert.Equal(true, r.FootnoteStrainAlternativeSatisfied);
        Assert.Equal(-3d, r.Psi, 11);
    }

    [Fact]
    public void StressAlternativeSufficesWithoutStrain_ButMalformedSuppliedStrainIsRejected()
    {
        Assert.NotNull(Screen(Request(n: 100)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Screen(Request(n: 100),
            new(0.005, 0, "E missing")));
        Assert.Throws<NotSupportedException>(() => Screen(Request(n: 100),
            new(double.NaN, 210000, "Invalid strain")));
        Assert.Throws<NotSupportedException>(() => Screen(Request(n: 100),
            new(0.005, 210000, null)));
    }

    [Fact]
    public void NoCompressedWebEdgeOrExactlyVanishingNormalForce_FailsClosed()
    {
        Assert.Throws<NotSupportedException>(() => Screen(Request(n: 100, m: 1)));
        Assert.Throws<NotSupportedException>(() => Screen(Request(n: 100, m: 5)));
        Assert.Throws<NotSupportedException>(() => Screen(Request(n: 0)));
        Assert.Throws<NotSupportedException>(() => Screen(Request(n: -100, m: 0)));
    }

    [Fact]
    public void CanOnlyClaimClass3OrBetterOrClass4ForWeb_NotPlasticClass()
    {
        // Independent worked example: c/t=100, A=2000, Iy=20e6,
        // N=-20kN, M=4kNm => same C=10/B=20/psi=-1/3 as reference.
        var r = Screen(Request(n: -20, m: 4),
            properties: new(2000, 20_000_000, "Independently checked thin I/H"),
            dimensions: Dimensions(tw: 2));
        Assert.Equal(100d, r.WebSlenderness);
        Assert.Equal(4, r.ConservativeWebClassUpperBound);
        Assert.False(r.EstablishesDefinitiveSectionClass);
        Assert.False(r.EstablishesCompleteMemberCompliance);
    }

    [Theory]
    [InlineData(double.NaN, 10, 0)]
    [InlineData(double.PositiveInfinity, 10, 0)]
    [InlineData(-100, double.NaN, 0)]
    [InlineData(-100, 10, 1)]
    public void OnlyFiniteConcomitantSignedFemNmWithoutShearIsAllowed(
        double n, double m, double v)
    {
        Assert.Throws<NotSupportedException>(() => Screen(Request(n, m, v)));
    }

    [Fact]
    public void GrossAreaInertiaAndTheirEvidenceMustBePhysicalAndComplete()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Screen(properties:
            new(0, 50_000_000, "area")));
        Assert.Throws<ArgumentOutOfRangeException>(() => Screen(properties:
            new(10000, double.PositiveInfinity, "inertia")));
        Assert.Throws<NotSupportedException>(() => Screen(properties:
            new(10000, 50_000_000, null)));
        Assert.Throws<ArgumentException>(() => Screen(properties:
            new(10000, 200_000_000, "Inertia larger than A*h^2/4")));
        Assert.Throws<ArgumentException>(() => Screen(dimensions:
            new(24, 100, 10, 8, 4)));
    }

    [Fact]
    public void CatalogIpeUsesNativeNtcMaterialAndOpPoGrossProperties()
    {
        var profiles = CommercialRolledSteelCatalogM88B2.Load(
            Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
        var materials = CharacteristicMaterialCatalogM88C.Load(
            Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));
        var r = Ntc2018SteelMixedElasticWebScreenerD0M3.ScreenCatalogProfile(
            Request(section: "IPE-200"), profiles, materials, "S235", Evidence());
        Assert.Equal("NTC2018", r.Code.EditionId);
        // Independent rational worked check from published indicative values:
        // A=2848 mm2; Ix=19430000 mm4; web y=(200-2*(8.5+12))/2=79.5mm.
        // N=-100kN M=10kNm -> psi=-8029/105179, class3 bound 65.13557.
        Assert.Equal(-8029d / 105179d, r.Psi, 11);
        Assert.Equal(65.1355728574723d, r.Class3Limit, 10);
        Assert.Equal(3, r.ConservativeWebClassUpperBound);
        Assert.Contains("OPPO", r.Trace);
        Assert.Contains("NOT CERTIFIED", r.Trace);
        Assert.False(r.EstablishesCompleteMemberCompliance);
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelMixedElasticWebScreenerD0M3.ScreenCatalogProfile(
                Request(section: "INP-200"), profiles, materials, "S235", Evidence()));
    }

    [Fact]
    public void EvidenceMustExplicitlyConfirmStrongAxisAndRolledUnperforatedGeometry()
    {
        var bad = Evidence() with { StrongAxisMappedToFemMomentConfirmed = false };
        Assert.Throws<NotSupportedException>(() => Screen(evidence: bad));
        bad = Evidence() with { UnperforatedConfirmed = null };
        Assert.Throws<NotSupportedException>(() => Screen(evidence: bad));
    }

    [Fact]
    public void NoSubstitutionOfEc3OrDifferentFemMemberTargetStationSide()
    {
        var request = Request();
        Assert.Throws<NotSupportedException>(() => Screen(request with
        {
            Profile = Ntc2018SteelTensionProfileD1.Create() with
            { Code = new("EN 1993-1-1", "2005+A1:2014", "IT") }
        }));
        Assert.Throws<ArgumentException>(() => Screen(request with
        { Effects = request.Effects with { MemberId = "OTHER" } }));
        Assert.Throws<ArgumentException>(() => Screen(request with
        { Effects = request.Effects with { TargetKind = DesignEffectTargetKind.LoadCase } }));
        Assert.Throws<ArgumentException>(() => Screen(request with
        { Effects = request.Effects with { SectionSide = null } }));
        Assert.Throws<ArgumentException>(() => Screen(request with
        { Effects = request.Effects with { PositionMetres = 4.5 } }));
    }
}
