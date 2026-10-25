using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D0;

public sealed class Ntc2018SteelMixedCompressionDominantD0M7Tests
{
    private static DesignCheckRequest Request(double n = -220, double m = 10,
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

    private static Ntc2018SteelMixedCompressionDominantClassD0M7 Classify(
        DesignCheckRequest? request = null) =>
        Ntc2018SteelMixedCompressionDominantClassifierD0M7.ClassifyCatalogProfile(
            request ?? Request(), Profiles(), Materials(), "S235", Evidence());

    [Theory]
    [InlineData(-220, Ntc2018SteelPlasticNeutralAxisRegionD0M6.TensionSideRootFillet)]
    [InlineData(-300, Ntc2018SteelPlasticNeutralAxisRegionD0M6.TensionSideFlange)]
    public void OnlyProvenAlphaOneCompressionDominantFromSameFemCanBeClassified(
        double n, Ntc2018SteelPlasticNeutralAxisRegionD0M6 expectedRegion)
    {
        var r = Classify(Request(n: n));
        Assert.Equal(expectedRegion, r.Plastic.Region);
        Assert.Equal(1, r.Plastic.PlasticCompressedFlatWebFractionAlpha);
        Assert.InRange(r.Elastic.Psi, -0.999999999999, 0.999999999999);
        Assert.Equal(1, r.Web.Class);
        Assert.Equal(1, r.Flange.Class);
        Assert.Equal(1, r.ClassWithinDeclaredScope);
        Assert.Equal(33, r.Web.Class1Limit, 10);
        Assert.Equal(38, r.Web.Class2Limit, 10);
        Assert.Equal(r.Elastic.Class3Limit, r.Web.Class3Limit, 10);
        Assert.Equal("FEM-A1", r.Plastic.AnalysisId);
        Assert.Equal("FEM-A1", r.Elastic.AnalysisId);
        Assert.Equal("ULS-1", r.Plastic.CombinationId);
        Assert.Contains("alpha=1 ENDPOINT ONLY", r.Trace);
        Assert.False(r.EstablishesSectionResistance);
        Assert.False(r.EstablishesNmOrShearInteraction);
        Assert.False(r.EstablishesCompleteMemberCompliance);
        Assert.False(r.EstablishesWholeStructureCompliance);
    }

    private static (Ntc2018SteelISectionDimensionsD0 Dimensions,
        Ntc2018SteelGrossElasticPropertiesD0M3 Gross, double CompressionKn)
        SyntheticSection(double webFlatMm)
    {
        const double tw = 10, tf = 8, r = 4, b = 100, fy = 235;
        double h = webFlatMm + 2 * (tf + r);
        double roots = 4 * r * r * (1 - Math.PI / 4);
        double area = 2 * b * tf + tw * (h - 2 * tf) + roots;
        double webRectangleH = h - 2 * tf;
        double flangeY = h / 2 - tf / 2;
        double rootY = h / 2 - tf - r / 2;
        double inertia = tw * Math.Pow(webRectangleH, 3) / 12 +
            2 * (b * Math.Pow(tf, 3) / 12 + b * tf * flangeY * flangeY) +
            roots * rootY * rootY;
        // The extra 10kN is small enough to place PNA inside the tensile-side
        // nominal root fillet while strictly clearing the straight web.
        double compressionKn = -(fy * tw * webFlatMm / 1000 + 10);
        return (new(h, b, tw, tf, r),
            new(area, inertia, "Independent symmetric nominal geometry A/I construction"),
            compressionKn);
    }

    [Theory]
    [InlineData(300, 1)]
    [InlineData(360, 2)]
    [InlineData(400, 3)]
    [InlineData(500, 4)]
    public void CompressionDominantNtcEndpointsGiveAllFourWebClasses(
        double webFlatMm, int expectedClass)
    {
        var synthetic = SyntheticSection(webFlatMm);
        var r = Ntc2018SteelMixedCompressionDominantClassifierD0M7.ClassifyGeometry(
            Request(n: synthetic.CompressionKn, m: 0.1,
                section: "I-ROLLED-SYNTHETIC"), synthetic.Dimensions,
            synthetic.Gross, "S235", 235, Evidence());
        Assert.Equal(Ntc2018SteelPlasticNeutralAxisRegionD0M6.TensionSideRootFillet,
            r.Plastic.Region);
        Assert.Equal(expectedClass, r.Web.Class);
        Assert.Equal(1, r.Flange.Class);
        Assert.Equal(expectedClass, r.ClassWithinDeclaredScope);
        Assert.Equal(expectedClass == 4, r.RequiresEffectiveSectionProperties);
        Assert.Equal(33, r.Web.Class1Limit, 12);
        Assert.Equal(38, r.Web.Class2Limit, 12);
        Assert.InRange(r.Web.Class3Limit, 42, 42.2);
        Assert.False(r.EstablishesSectionResistance);
    }

    [Fact]
    public void PnaOutsideWebByItselfDoesNotAuthorizeTensileAlphaZeroClassification()
    {
        var request = Request(n: 220);
        var plastic = Ntc2018SteelPlasticNeutralAxisCalculatorD0M6.CalculateCatalogProfile(
            request, Profiles(), Materials(), "S235", Evidence());
        Assert.Equal(0, plastic.PlasticCompressedFlatWebFractionAlpha);
        Assert.Throws<NotSupportedException>(() => Classify(request));
    }

    [Fact]
    public void PreexistingFlatWebClassifierAndNewEndpointRemainMutuallyExclusive()
    {
        var flat = Request(n: -100);
        var beyond = Request(n: -220);
        Assert.Throws<NotSupportedException>(() => Classify(flat));
        Assert.Equal(1, Ntc2018SteelMixedAutomaticSectionClassifierD0M5.ClassifyCatalogProfile(
            flat, Profiles(), Materials(), "S235", Evidence()).ClassWithinDeclaredScope);
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelMixedAutomaticSectionClassifierD0M5.ClassifyCatalogProfile(
                beyond, Profiles(), Materials(), "S235", Evidence()));
        Assert.Equal(1, Classify(beyond).ClassWithinDeclaredScope);
    }

    [Fact]
    public void NominalTransitionRequiresStrictCompressionBeyondFlatWeb()
    {
        double nFlat = 235d * 5.6 * 159 / 1000;
        Assert.Throws<NotSupportedException>(() => Classify(Request(n: -nFlat)));
        Assert.Throws<NotSupportedException>(() =>
            Classify(Request(n: -nFlat * (1 + 5e-11))));
        Assert.Equal(1, Classify(Request(n: -nFlat * (1 + 1e-5))).ClassWithinDeclaredScope);
    }

    [Fact]
    public void UnprovedFEMGeometryEditionShearOrMaterialCannotBeClassified()
    {
        Assert.Throws<NotSupportedException>(() => Classify(Request(v: 1)));
        Assert.Throws<NotSupportedException>(() => Classify(Request(m: 0)));
        Assert.Throws<NotSupportedException>(() => Classify(Request(n: 0)));
        Assert.Throws<NotSupportedException>(() => Classify(Request(n: -100)));
        Assert.Throws<NotSupportedException>(() => Classify(Request(n: -700)));
        Assert.Throws<NotSupportedException>(() => Classify(Request(section: "INP-200")));
        Assert.Throws<ArgumentException>(() => Classify(Request() with
        { Effects = Request().Effects with { MemberId = "OTHER" } }));
        Assert.Throws<NotSupportedException>(() => Classify(Request() with
        {
            Profile = Ntc2018SteelTensionProfileD1.Create() with
                { Code = new("EN 1993-1-1", "2005+A1:2014", "IT") }
        }));
        var geo = SyntheticSection(300);
        var r = Request(n: geo.CompressionKn, m: 0.1, section: "I-ROLLED-SYNTHETIC");
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelMixedCompressionDominantClassifierD0M7.ClassifyGeometry(
                r, geo.Dimensions,
                geo.Gross with { GrossAreaMm2 = geo.Gross.GrossAreaMm2 * 1.02 },
                "S235", 235, Evidence()));
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelMixedCompressionDominantClassifierD0M7.ClassifyGeometry(
                r, geo.Dimensions, geo.Gross, "S235", 235,
                Evidence() with { StrongAxisMappedToFemMomentConfirmed = null }));
    }
}
