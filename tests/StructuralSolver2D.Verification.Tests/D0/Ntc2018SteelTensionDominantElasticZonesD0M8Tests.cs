using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D0;

public sealed class Ntc2018SteelTensionDominantElasticZonesD0M8Tests
{
    private static DesignCheckRequest Request(double n = 220, double m = 10,
        double v = 0, string section = "IPE-200") => new(
        new DesignMemberContext("M1", DesignMaterial.Steel, section, 4),
        DesignLimitState.Ultimate, Ntc2018SteelTensionProfileD1.Create(),
        new DesignEffectSnapshot("FEM-A1", "ULS-1", DesignEffectOrigin.FemAnalysis,
            2, n, v, m)
        {
            MemberId = "M1", TargetKind = DesignEffectTargetKind.LoadCombination,
            SectionSide = DesignEffectSectionSide.Continuous
        }, Array.Empty<string>());

    private static Ntc2018SteelMixedClassificationEvidenceD0M1 Evidence() => new(
        true, true, true,
        "Unperforated doubly symmetric parallel-flange rolled I/H and strong-axis FEM",
        "Independently documented characteristic steel grade and fyk");

    private static CommercialRolledSteelCatalogM88B2 Profiles() =>
        CommercialRolledSteelCatalogM88B2.Load(
            Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));

    private static CharacteristicMaterialCatalogM88C Materials() =>
        CharacteristicMaterialCatalogM88C.Load(
            Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));

    private static Ntc2018SteelTensionDominantElasticZoneScreenD0M8 Classify(
        DesignCheckRequest? request = null,
        Ntc2018SteelTensileStrainEvidenceD0M3? tensileStrain = null) =>
        Ntc2018SteelTensionDominantElasticZonesD0M8.ScreenCatalogProfile(
            request ?? Request(), Profiles(), Materials(), "S235", Evidence(), tensileStrain);

    [Theory]
    [InlineData(220, 10, Ntc2018SteelPlasticNeutralAxisRegionD0M6.CompressionSideRootFillet)]
    [InlineData(300, 10, Ntc2018SteelPlasticNeutralAxisRegionD0M6.CompressionSideFlange)]
    public void EntireNominalElasticSectionIsTensileButStillNotCertifiedOrClassified(
        double n, double m, Ntc2018SteelPlasticNeutralAxisRegionD0M6 expectedPna)
    {
        var result = Classify(Request(n: n, m: m));
        Assert.Equal(expectedPna, result.Plastic.Region);
        Assert.Equal(0, result.Plastic.PlasticCompressedFlatWebFractionAlpha);
        Assert.Equal(Ntc2018SteelTensionDominantElasticRegionD0M8.NoCompressedElasticFibres,
            result.ElasticRegion);
        Assert.True(result.MaxSignedExtremeFibreStressMpa < 0);
        Assert.True(result.MaxSignedFlatWebEdgeStressMpa < 0);
        Assert.Null(result.WebClassThreeScreen);
        Assert.False(result.EstablishesDefinitiveNtcSectionClass);
        Assert.False(result.EstablishesPlasticClassOneOrTwo);
        Assert.False(result.EstablishesSectionResistance);
        Assert.False(result.EstablishesNmOrShearInteraction);
        Assert.False(result.EstablishesCompleteMemberCompliance);
        Assert.False(result.EstablishesWholeStructureCompliance);
        Assert.Contains("NO definitive class 1-4", result.Trace);
    }

    [Fact]
    public void CompressionInFlangeOrFilletDoesNotInventWebOrFlangeNtcClass()
    {
        var result = Classify(Request(m: 17));
        Assert.Equal(Ntc2018SteelTensionDominantElasticRegionD0M8.CompressionConfinedToFlangeOrRoot,
            result.ElasticRegion);
        Assert.True(result.MaxSignedExtremeFibreStressMpa > 0);
        Assert.True(result.MaxSignedFlatWebEdgeStressMpa < 0);
        Assert.Null(result.WebClassThreeScreen);
        Assert.False(result.EstablishesDefinitiveNtcSectionClass);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(-20)]
    public void CompressedFlatWebUsesIndependentD0M3ButStillHasNoDefinitiveClass(double moment)
    {
        var result = Classify(Request(m: moment));
        Assert.Equal(Ntc2018SteelTensionDominantElasticRegionD0M8.CompressionReachesFlatWeb,
            result.ElasticRegion);
        Assert.True(result.MaxSignedFlatWebEdgeStressMpa > 0);
        var web = Assert.IsType<Ntc2018SteelMixedElasticWebScreenD0M3>(
            result.WebClassThreeScreen);
        Assert.True(web.Psi < -1);
        Assert.Equal(true, web.FootnoteStressAlternativeSatisfied);
        Assert.Equal(result.MaxSignedFlatWebEdgeStressMpa,
            web.CompressiveEdgeStressMpa, 10);
        Assert.Equal(result.Plastic.AnalysisId, web.AnalysisId);
        Assert.Equal(result.Plastic.CombinationId, web.CombinationId);
        Assert.Equal(result.Plastic.PositionMetres, web.PositionMetres);
        Assert.Equal(result.Plastic.Side, web.Side);
        Assert.False(result.EstablishesDefinitiveNtcSectionClass);
    }

    [Fact]
    public void CompressiveWebOverYieldNeedsD0M3FootnoteAlternativeEvidence()
    {
        Assert.Throws<NotSupportedException>(() => Classify(Request(m: 200)));
        var result = Classify(Request(m: 200),
            new Ntc2018SteelTensileStrainEvidenceD0M3(
                0.005, 210000, "Independent tensile strain and Young modulus evidence"));
        Assert.Equal(Ntc2018SteelTensionDominantElasticRegionD0M8.CompressionReachesFlatWeb,
            result.ElasticRegion);
        Assert.Equal(true, result.WebClassThreeScreen!.FootnoteStrainAlternativeSatisfied);
        Assert.False(result.EstablishesDefinitiveNtcSectionClass);
    }

    [Fact]
    public void AlmostZeroElasticStressAtPhysicalTransitionsIsUnsupported()
    {
        const double n = 220, area = 2848, inertia = 1943 * 10000d;
        double mExtremeZero = n * 1000d * inertia / (area * 1e6 * 100);
        double mWebZero = n * 1000d * inertia / (area * 1e6 * 79.5);
        Assert.Throws<NotSupportedException>(() => Classify(Request(m: mExtremeZero)));
        Assert.Throws<NotSupportedException>(() => Classify(Request(m: mWebZero)));
        Assert.Equal(Ntc2018SteelTensionDominantElasticRegionD0M8.NoCompressedElasticFibres,
            Classify(Request(m: mExtremeZero - 0.01)).ElasticRegion);
        Assert.Equal(Ntc2018SteelTensionDominantElasticRegionD0M8.CompressionConfinedToFlangeOrRoot,
            Classify(Request(m: (mExtremeZero + mWebZero) / 2)).ElasticRegion);
        Assert.Equal(Ntc2018SteelTensionDominantElasticRegionD0M8.CompressionReachesFlatWeb,
            Classify(Request(m: mWebZero + 0.01)).ElasticRegion);
    }

    [Fact]
    public void OtherPlasticDomainsAndUnprovedMaterialGeometryOrFEMRemainUnsupported()
    {
        Assert.Throws<NotSupportedException>(() => Classify(Request(n: -220)));
        Assert.Throws<NotSupportedException>(() => Classify(Request(n: 0)));
        Assert.Throws<NotSupportedException>(() => Classify(Request(n: 100)));
        Assert.Throws<NotSupportedException>(() => Classify(Request(n: 700)));
        Assert.Throws<NotSupportedException>(() => Classify(Request(m: 0)));
        Assert.Throws<NotSupportedException>(() => Classify(Request(v: 1)));
        Assert.Throws<NotSupportedException>(() => Classify(Request(section: "INP-200")));
        Assert.Throws<ArgumentException>(() => Classify(Request() with
        { Effects = Request().Effects with { MemberId = "OTHER" } }));
        Assert.Throws<NotSupportedException>(() => Classify(Request() with
        {
            Profile = Ntc2018SteelTensionProfileD1.Create() with
                { Code = new("EN 1993-1-1", "2005+A1:2014", "IT") }
        }));
        var p = Profiles().Find("IPE-200").Definition;
        var dim = new Ntc2018SteelISectionDimensionsD0(p.HeightMm, p.WidthMm,
            p.WebThicknessMm, p.FlangeThicknessMm, p.RootRadiusMm);
        var gross = new Ntc2018SteelGrossElasticPropertiesD0M3(
            p.AreaCm2 * 100, p.StrongInertiaCm4 * 10000,
            "Independently documented gross A and Iy");
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelTensionDominantElasticZonesD0M8.ScreenGeometry(
                Request(), dim,
                gross with { StrongAxisInertiaMm4 = 1e20 },
                "S235", 235, Evidence()));
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelTensionDominantElasticZonesD0M8.ScreenGeometry(
                Request(), dim,
                gross with { StrongAxisInertiaMm4 = 1 },
                "S235", 235, Evidence()));
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelTensionDominantElasticZonesD0M8.ScreenGeometry(
                Request(), dim,
                gross with { GrossAreaMm2 = gross.GrossAreaMm2 * 1.02 },
                "S235", 235, Evidence()));
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelTensionDominantElasticZonesD0M8.ScreenGeometry(
                Request(), dim, gross, "S235", 235,
                Evidence() with { StrongAxisMappedToFemMomentConfirmed = null }));
    }
}
