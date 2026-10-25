using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D0;

public sealed class Ntc2018SteelMixedPlasticWebAlphaD0M4Tests
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
        true, true, true, "Documented symmetric hot-rolled flange and root fillets",
        "Documented characteristic fy and steel grade");

    // Straight central web = 224-2*(8+4)=200mm, tw=10mm;
    // the 3694mm2 gross area includes the symmetric root fillets.
    private static Ntc2018SteelISectionDimensionsD0 Dimensions() =>
        new(224, 100, 10, 8, 4);

    private static Ntc2018SteelGrossPlasticAreaD0M4 Gross() =>
        new(3694, "Independent rounded I-section gross area in mm2");

    private static Ntc2018SteelMixedPlasticWebAlphaD0M4 Calc(
        DesignCheckRequest? request = null,
        Ntc2018SteelISectionDimensionsD0? dimensions = null,
        Ntc2018SteelGrossPlasticAreaD0M4? gross = null,
        Ntc2018SteelMixedClassificationEvidenceD0M1? evidence = null) =>
        Ntc2018SteelMixedPlasticWebAlphaCalculatorD0M4.CalculateGeometry(
            request ?? Request(), dimensions ?? Dimensions(), gross ?? Gross(),
            "S235", 235, evidence ?? Evidence());

    [Fact]
    public void CompressiveAxialForceMovesPlasticNeutralAxisTowardTensionSide()
    {
        var r = Calc();
        // N=-100kN -> delta=100000/(2*235*10), alpha=1/2+delta/200.
        Assert.Equal(100000d / 4700d, r.PlasticNeutralAxisShiftTowardTensionEdgeMm, 11);
        Assert.Equal(0.5 + 100000d / (2 * 235 * 10 * 200),
            r.PlasticCompressedFractionAlpha, 11);
        Assert.Equal(470d, r.AxialForceLimitForPlasticNeutralAxisInFlatWebKn);
        Assert.Equal(235d * 3694 / 1000, r.GrossAxialYieldForceKn);
        Assert.Equal(200d, r.FlatWebDepthMm);
        Assert.Contains("PLASTIC WEB ALPHA GEOMETRY ONLY", r.Trace);
        Assert.Contains("FEM=FEM-A1/ULS-1", r.Trace);
        Assert.False(r.EstablishesActualMomentResistance);
        Assert.False(r.EstablishesElasticPsi);
        Assert.False(r.EstablishesDefinitiveSectionClass);
        Assert.False(r.EstablishesCompleteMemberCompliance);
    }

    [Fact]
    public void TensileAxialForceContractsCompressedPlasticRegion()
    {
        var r = Calc(Request(n: 100));
        Assert.Equal(-100000d / 4700d,
            r.PlasticNeutralAxisShiftTowardTensionEdgeMm, 11);
        Assert.Equal(0.5 - 100000d / (2 * 235 * 10 * 200),
            r.PlasticCompressedFractionAlpha, 11);
        Assert.True(r.PlasticCompressedFractionAlpha < 0.5);
    }

    [Fact]
    public void FlippingMomentOrSectionSideDoesNotChangeAbsolutePlasticAlpha()
    {
        var positive = Calc();
        var negative = Calc(Request(m: -10));
        Assert.Equal(positive.PlasticCompressedFractionAlpha,
            negative.PlasticCompressedFractionAlpha);
        Assert.Equal(-10d, negative.SignedStrongAxisMomentKnm);
        var request = Request();
        var left = Calc(request with
        {
            Effects = request.Effects with { SectionSide = DesignEffectSectionSide.BeforePointLoad }
        });
        Assert.Equal(positive.PlasticCompressedFractionAlpha,
            left.PlasticCompressedFractionAlpha);
    }

    [Theory]
    [InlineData(-470)]
    [InlineData(470)]
    [InlineData(-471)]
    [InlineData(471)]
    [InlineData(-2000)]
    [InlineData(2000)]
    public void RootFilletAndFlangeNeutralAxisAreUnsupported(double n)
    {
        Assert.Throws<NotSupportedException>(() => Calc(Request(n: n)));
    }

    [Fact]
    public void PlasticWebBoundaryMustBeStrictlyInteriorNotNumericallyAmbiguous()
    {
        Assert.Throws<NotSupportedException>(() => Calc(Request(n: -470 * (1 - 1e-11))));
        var r = Calc(Request(n: -470 * (1 - 1e-7)));
        Assert.InRange(r.PlasticCompressedFractionAlpha, 0.999999, 1);
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(-100, 0, 0)]
    [InlineData(-100, 10, 1)]
    [InlineData(double.NaN, 10, 0)]
    [InlineData(double.PositiveInfinity, 10, 0)]
    [InlineData(-100, double.NaN, 0)]
    public void RequiresFiniteSameSectionNonzeroNmAndZeroShear(double n, double m, double v)
    {
        Assert.Throws<NotSupportedException>(() => Calc(Request(n, m, v)));
    }

    [Fact]
    public void RejectsImpossibleGrossSectionAreaOrGeometryOrMissingProvenance()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Calc(gross: new(0, "invalid")));
        Assert.Throws<ArgumentOutOfRangeException>(() => Calc(gross:
            new(double.NaN, "invalid")));
        Assert.Throws<NotSupportedException>(() => Calc(gross: new(3694, null)));
        Assert.Throws<ArgumentException>(() => Calc(gross: new(2500, "too little material")));
        Assert.Throws<ArgumentException>(() => Calc(gross: new(15000, "unrealistic fillets")));
        Assert.Throws<ArgumentException>(() => Calc(dimensions:
            new(24, 100, 10, 8, 4)));
        Assert.Throws<ArgumentException>(() => Calc(dimensions:
            new(224, 20, 10, 8, 6)));
        Assert.Throws<NotSupportedException>(() => Calc(evidence:
            Evidence() with { UnperforatedConfirmed = null }));
        Assert.Throws<NotSupportedException>(() => Calc(evidence:
            Evidence() with { HotRolledParallelFlangeDoublySymmetricConfirmed = false }));
        Assert.Throws<NotSupportedException>(() => Calc(evidence:
            Evidence() with { StrongAxisMappedToFemMomentConfirmed = false }));
    }

    [Fact]
    public void RejectsEc3IdentityAndNonmatchingFemProvenance()
    {
        var request = Request();
        Assert.Throws<NotSupportedException>(() => Calc(request with
        {
            Profile = Ntc2018SteelTensionProfileD1.Create() with
            { Code = new("EN 1993-1-1", "2005+A1:2014", "IT") }
        }));
        Assert.Throws<ArgumentException>(() => Calc(request with
        { Effects = request.Effects with { MemberId = "OTHER" } }));
        Assert.Throws<ArgumentException>(() => Calc(request with
        { Effects = request.Effects with { SectionSide = null } }));
        Assert.Throws<ArgumentException>(() => Calc(request with
        { Effects = request.Effects with { PositionMetres = 4.5 } }));
        Assert.Throws<ArgumentException>(() => Calc(request with
        { Effects = request.Effects with { TargetKind = DesignEffectTargetKind.LoadCase } }));
    }

    [Fact]
    public void SourceBackedIndicativeIpe200HasExactIndependentAlpha()
    {
        var profiles = CommercialRolledSteelCatalogM88B2.Load(
            Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
        var materials = CharacteristicMaterialCatalogM88C.Load(
            Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));
        var r = Ntc2018SteelMixedPlasticWebAlphaCalculatorD0M4.CalculateCatalogProfile(
            Request(section: "IPE-200"), profiles, materials, "S235", Evidence());
        // OPPO: h=200, b=100, tw=5.6, tf=8.5, r=12, A=2848 mm2.
        // c=159 mm, Nlimit=235*5.6*159/1000=209.244 kN.
        Assert.Equal(159d, r.FlatWebDepthMm);
        Assert.Equal(2848d, r.GrossAreaMm2);
        Assert.Equal(209.244d, r.AxialForceLimitForPlasticNeutralAxisInFlatWebKn, 11);
        Assert.Equal(0.5 + 100000d / (2 * 235 * 5.6 * 159),
            r.PlasticCompressedFractionAlpha, 11);
        Assert.Contains("OPPO", r.Trace);
        Assert.Contains("NOT CERTIFIED", r.Trace);
        Assert.False(r.EstablishesDefinitiveSectionClass);
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelMixedPlasticWebAlphaCalculatorD0M4.CalculateCatalogProfile(
                Request(section: "INP-200"), profiles, materials, "S235", Evidence()));
    }

    [Fact]
    public void SameIpe200FemInputKeepsPlasticAlphaAndElasticPsiDistinct()
    {
        var profiles = CommercialRolledSteelCatalogM88B2.Load(
            Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
        var materials = CharacteristicMaterialCatalogM88C.Load(
            Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));
        foreach (double n in new[] { -100d, 100d })
        {
            var request = Request(n: n, m: 10, section: "IPE-200");
            var alpha = Ntc2018SteelMixedPlasticWebAlphaCalculatorD0M4
                .CalculateCatalogProfile(request, profiles, materials, "S235", Evidence());
            var psi = Ntc2018SteelMixedElasticWebScreenerD0M3
                .ScreenCatalogProfile(request, profiles, materials, "S235", Evidence());
            Assert.Equal(alpha.AnalysisId, psi.AnalysisId);
            Assert.Equal(alpha.CombinationId, psi.CombinationId);
            Assert.Equal(alpha.PositionMetres, psi.PositionMetres);
            Assert.Equal(alpha.SignedAxialForceKn, psi.SignedAxialForceKn);
            Assert.Equal(alpha.SignedStrongAxisMomentKnm, psi.SignedMomentKnm);
            Assert.Equal(n < 0, alpha.PlasticCompressedFractionAlpha > 0.5);
            Assert.Equal(n < 0, psi.Psi > -1);
            Assert.False(alpha.EstablishesDefinitiveSectionClass);
            Assert.False(psi.EstablishesDefinitiveSectionClass);
        }
    }
}
