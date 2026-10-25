using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D0;

public sealed class Ntc2018SteelPlasticNeutralAxisD0M6Tests
{
    private static DesignCheckRequest Request(double n = -100, double m = 10,
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
        "Independently evidenced unperforated doubly symmetric rolled parallel-flange I/H",
        "Documented characteristic yield strength and steel grade");

    private static CommercialRolledSteelCatalogM88B2 Profiles() =>
        CommercialRolledSteelCatalogM88B2.Load(
            Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));

    private static CharacteristicMaterialCatalogM88C Materials() =>
        CharacteristicMaterialCatalogM88C.Load(
            Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));

    private static Ntc2018SteelPlasticNeutralAxisD0M6 Calculate(
        DesignCheckRequest? request = null) =>
        Ntc2018SteelPlasticNeutralAxisCalculatorD0M6.CalculateCatalogProfile(
            request ?? Request(), Profiles(), Materials(), "S235", Evidence());

    [Fact]
    public void StraightWebMatchesEstablishedD0M4Algebra()
    {
        var d6 = Calculate();
        var d4 = Ntc2018SteelMixedPlasticWebAlphaCalculatorD0M4.CalculateCatalogProfile(
            Request(), Profiles(), Materials(), "S235", Evidence());
        Assert.Equal(Ntc2018SteelPlasticNeutralAxisRegionD0M6.StraightWeb, d6.Region);
        Assert.Equal(d4.PlasticCompressedFractionAlpha,
            d6.PlasticCompressedFlatWebFractionAlpha, 10);
        Assert.Equal(d4.PlasticNeutralAxisShiftTowardTensionEdgeMm,
            d6.NeutralAxisShiftTowardTensionFaceMm, 10);
        Assert.True(d6.NominalAreaRelativeDiscrepancy < 0.001);
        Assert.Equal("FEM-A1", d6.AnalysisId);
        Assert.Equal("ULS-1", d6.CombinationId);
        Assert.False(d6.EstablishesFinalNtcSectionClass);
        Assert.False(d6.EstablishesPlasticMomentCapacity);
        Assert.False(d6.EstablishesSectionResistance);
        Assert.False(d6.EstablishesNmOrShearInteraction);
        Assert.False(d6.EstablishesCompleteMemberCompliance);
        Assert.False(d6.EstablishesWholeStructureCompliance);
    }

    [Theory]
    // Depth targets computed independently by transformed-angle numerical
    // quadrature in benchmarks/independent/p0d0m6, NOT by this C# routine.
    [InlineData(-220, Ntc2018SteelPlasticNeutralAxisRegionD0M6.TensionSideRootFillet, 1.0, 183.30835011)]
    [InlineData(-300, Ntc2018SteelPlasticNeutralAxisRegionD0M6.TensionSideFlange, 1.0, 192.14092544)]
    [InlineData(220, Ntc2018SteelPlasticNeutralAxisRegionD0M6.CompressionSideRootFillet, 0.0, 16.69165110)]
    [InlineData(300, Ntc2018SteelPlasticNeutralAxisRegionD0M6.CompressionSideFlange, 0.0, 7.85907464)]
    public void AxialForcePushesNeutralAxisThroughFilletAndFlange(
        double n, Ntc2018SteelPlasticNeutralAxisRegionD0M6 expectedRegion,
        double expectedAlpha, double independentDepthMm)
    {
        var result = Calculate(Request(n: n));
        Assert.Equal(expectedRegion, result.Region);
        Assert.Equal(expectedAlpha, result.PlasticCompressedFlatWebFractionAlpha, 12);
        Assert.InRange(Math.Abs(result.NeutralAxisDepthFromCompressionFaceMm -
            independentDepthMm), 0, 0.001);
        Assert.True(Math.Abs(result.NeutralAxisShiftTowardTensionFaceMm) >
            result.FlatWebDepthMm / 2);
        Assert.Equal(n < 0, result.NeutralAxisShiftTowardTensionFaceMm > 0);
        Assert.Contains("NOT a D0M1/D0M5 normative classification", result.Trace);
    }

    [Theory]
    [InlineData(220)]
    [InlineData(300)]
    public void TensionAndCompressionRemainMirrorSymmetric(double force)
    {
        var compression = Calculate(Request(n: -force));
        var tension = Calculate(Request(n: force));
        Assert.Equal(200, compression.NeutralAxisDepthFromCompressionFaceMm +
            tension.NeutralAxisDepthFromCompressionFaceMm, 9);
        Assert.Equal(1.0,
            compression.PlasticCompressedFlatWebFractionAlpha +
            tension.PlasticCompressedFlatWebFractionAlpha, 12);
        Assert.Equal(compression.NominalModeledAreaMm2,
            tension.NominalModeledAreaMm2, 12);
    }

    [Fact]
    public void ChangingStrongAxisMomentSignDoesNotChangeSymmetricPlasticAreaEquilibrium()
    {
        var a = Calculate(Request(n: -220, m: 10));
        var b = Calculate(Request(n: -220, m: -10));
        Assert.Equal(a.Region, b.Region);
        Assert.Equal(a.NeutralAxisDepthFromCompressionFaceMm,
            b.NeutralAxisDepthFromCompressionFaceMm, 12);
        Assert.Equal(-10d, b.SignedStrongAxisMomentKnm);
    }

    [Fact]
    public void FlatWebFilletTransitionIsContinuousAtFormerD0M4Cutoff()
    {
        double flatLimitKn = 235d * 5.6 * 159 / 1000;
        var tension = Calculate(Request(n: flatLimitKn));
        var compression = Calculate(Request(n: -flatLimitKn));
        Assert.InRange(Math.Abs(tension.NeutralAxisDepthFromCompressionFaceMm - 20.5),
            0, 1e-8);
        Assert.InRange(Math.Abs(compression.NeutralAxisDepthFromCompressionFaceMm - 179.5),
            0, 1e-8);
        Assert.Equal(0, tension.PlasticCompressedFlatWebFractionAlpha, 10);
        Assert.Equal(1, compression.PlasticCompressedFlatWebFractionAlpha, 10);
        // D0M4 intentionally guards the transition and remains unmodified.
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelMixedPlasticWebAlphaCalculatorD0M4.CalculateCatalogProfile(
                Request(n: -flatLimitKn), Profiles(), Materials(), "S235", Evidence()));
    }

    [Fact]
    public void ExtendingPlasticGeometryDoesNotSilentlyExtendD0M5NormativeClassification()
    {
        var req = Request(n: -220);
        Assert.Equal(Ntc2018SteelPlasticNeutralAxisRegionD0M6.TensionSideRootFillet,
            Calculate(req).Region);
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelMixedAutomaticSectionClassifierD0M5.ClassifyCatalogProfile(
                req, Profiles(), Materials(), "S235", Evidence()));
    }

    [Fact]
    public void CatalogIpeHeaHebNominalCircularRootAreasMatchSourceWithinTolerance()
    {
        foreach (string section in new[] { "IPE-80", "IPE-200", "HEA-100",
                     "HEA-240", "HEB-100", "HEB-260" })
        {
            var result = Calculate(Request(n: -20, section: section));
            Assert.InRange(result.NominalAreaRelativeDiscrepancy,
                0, Ntc2018SteelPlasticNeutralAxisCalculatorD0M6.MaximumNominalAreaRelativeDiscrepancy);
            Assert.Equal(Ntc2018SteelPlasticNeutralAxisRegionD0M6.StraightWeb, result.Region);
        }
    }

    [Fact]
    public void InvalidModelEditionShearEvidenceAndGrossAreaFailClosed()
    {
        Assert.Throws<NotSupportedException>(() => Calculate(Request(section: "INP-200")));
        Assert.Throws<NotSupportedException>(() => Calculate(Request(v: 1)));
        Assert.Throws<NotSupportedException>(() => Calculate(Request(n: 0)));
        Assert.Throws<NotSupportedException>(() => Calculate(Request(m: 0)));
        Assert.Throws<NotSupportedException>(() => Calculate(Request(n: 670)));
        Assert.Throws<NotSupportedException>(() => Calculate(Request(n: -670)));
        Assert.Throws<ArgumentException>(() => Calculate(Request() with
        { Effects = Request().Effects with { MemberId = "OTHER" } }));
        Assert.Throws<NotSupportedException>(() => Calculate(Request() with
        {
            Profile = Ntc2018SteelTensionProfileD1.Create() with
                { Code = new("EN 1993-1-1", "2005+A1:2014", "IT") }
        }));

        var dim = new Ntc2018SteelISectionDimensionsD0(200, 100, 5.6, 8.5, 12);
        var gross = new Ntc2018SteelGrossPlasticAreaD0M4(2848,
            "Independent nominal IPE200 published gross area");
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelPlasticNeutralAxisCalculatorD0M6.CalculateGeometry(
                Request(), dim, gross with { GrossAreaMm2 = 2900 }, "S235", 235, Evidence()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Ntc2018SteelPlasticNeutralAxisCalculatorD0M6.CalculateGeometry(
                Request(), dim, gross, "S235", double.NaN, Evidence()));
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelPlasticNeutralAxisCalculatorD0M6.CalculateGeometry(
                Request(), dim with { RootRadiusMm = 60 }, gross,
                "S235", 235, Evidence()));
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelPlasticNeutralAxisCalculatorD0M6.CalculateGeometry(
                Request(), dim, gross, "S235", 235,
                Evidence() with { StrongAxisMappedToFemMomentConfirmed = null }));
    }
}
