using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D0;

public sealed class Ntc2018SteelMixedAutomaticSectionD0M5Tests
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
        "Independently documented rolled doubly symmetric unperforated I/H section and local strong axis",
        "Characteristic steel grade and fyk for actual thickness");

    private static CommercialRolledSteelCatalogM88B2 Profiles() =>
        CommercialRolledSteelCatalogM88B2.Load(
            Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));

    private static CharacteristicMaterialCatalogM88C Materials() =>
        CharacteristicMaterialCatalogM88C.Load(
            Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));

    private static Ntc2018SteelMixedAutomaticSectionD0M5 ClassifyCatalog(
        DesignCheckRequest? request = null,
        Ntc2018SteelTensileStrainEvidenceD0M3? strain = null,
        Ntc2018SteelMixedClassificationEvidenceD0M1? evidence = null) =>
        Ntc2018SteelMixedAutomaticSectionClassifierD0M5.ClassifyCatalogProfile(
            request ?? Request(), Profiles(), Materials(), "S235",
            evidence ?? Evidence(), strain);

    [Fact]
    public void Ipe200Compression_ComposesNtcAlphaAndPsiFromSameFemStation()
    {
        var r = ClassifyCatalog();
        Assert.Equal(1, r.ClassWithinDeclaredScope);
        Assert.Equal(1, r.Classification.Web.Class);
        Assert.Equal(1, r.Classification.Flange.Class);
        Assert.Equal(0.5 + 100000d / (2 * 235 * 5.6 * 159),
            r.Plastic.PlasticCompressedFractionAlpha, 11);
        Assert.Equal(-8029d / 105179d, r.Elastic.Psi, 11);
        Assert.Equal(r.Plastic.PlasticCompressedFractionAlpha, r.Classification.Alpha);
        Assert.Equal(r.Elastic.Psi, r.Classification.Psi);
        Assert.Equal("FEM-A1", r.Classification.AnalysisId);
        Assert.Equal("ULS-1", r.Classification.CombinationId);
        Assert.Contains("NTC2018", r.Trace);
        Assert.False(r.EstablishesSectionResistance);
        Assert.False(r.EstablishesNmOrShearInteraction);
        Assert.False(r.EstablishesCompleteMemberCompliance);
        Assert.False(r.EstablishesWholeStructureCompliance);
    }

    [Fact]
    public void Ipe200Tension_CompressionStressBelowYieldUsesNtcOrWithoutInventedStrain()
    {
        var r = ClassifyCatalog(Request(n: 100));
        Assert.Equal(1, r.ClassWithinDeclaredScope);
        Assert.True(r.Plastic.PlasticCompressedFractionAlpha < 0.5);
        Assert.True(r.Elastic.Psi < -1);
        Assert.Equal(true, r.Elastic.FootnoteStressAlternativeSatisfied);
        Assert.Null(r.Elastic.FootnoteStrainAlternativeSatisfied);
        Assert.Contains("stress only", r.Trace);
        Assert.Contains("NOT REQUIRED", r.Classification.Trace);
        Assert.False(r.EstablishesSectionResistance);
    }

    [Fact]
    public void AboveYieldCompressionRequiresExternalProofOfTensileStrain()
    {
        // Nominal IPE200 N=+100, M=+68.5 gives a compressed web edge >235MPa,
        // psi<-1, while alpha remains <1/2 and thresholds remain ordered.
        var request = Request(n: 100, m: 68.5);
        Assert.Throws<NotSupportedException>(() => ClassifyCatalog(request));
        Assert.Throws<NotSupportedException>(() => ClassifyCatalog(request,
            new(235d / 210000d, 210000, "Independent exact tensile yield strain")));
        var r = ClassifyCatalog(request,
            new(0.002, 210000, "Independently documented tensile strain and Young modulus"));
        Assert.Equal(false, r.Elastic.FootnoteStressAlternativeSatisfied);
        Assert.Equal(true, r.Elastic.FootnoteStrainAlternativeSatisfied);
        Assert.Contains("external strain", r.Trace);
        Assert.Contains("E=210000MPa", r.Classification.Trace);
        Assert.False(r.EstablishesSectionResistance);
    }

    private static (Ntc2018SteelISectionDimensionsD0 Dimensions,
        Ntc2018SteelGrossElasticPropertiesD0M3 Gross, double MomentKnm)
        SyntheticSection(double webFlatMm)
    {
        const double tw = 10, tf = 8, r = 4, b = 100;
        double h = webFlatMm + 2 * (tf + r);
        double grossA = 2 * b * tf + tw * (h - 2 * tf) + 14;
        // Independent sharp-rectangle inertia plus a small symmetric-root
        // correction. A geometric benchmark only, NOT certified profile data.
        double webH = h - 2 * tf;
        double yFlange = h / 2 - tf / 2;
        double yRoot = h / 2 - tf - r / 2;
        double inertia = tw * Math.Pow(webH, 3) / 12 +
            2 * (b * Math.Pow(tf, 3) / 12 + b * tf * yFlange * yFlange) +
            14 * yRoot * yRoot;
        // Choose B=3C for N=-100kN, giving psi=-1/2 exactly in algebra.
        double m = 3 * (100000d / grossA) * inertia /
            (1e6 * webFlatMm / 2);
        return (new(h, b, tw, tf, r),
            new(grossA, inertia, "Independently constructed symmetric I-section A/I reference"), m);
    }

    [Theory]
    [InlineData(600, 1)]
    [InlineData(700, 2)]
    [InlineData(800, 3)]
    [InlineData(900, 4)]
    public void IndependentSyntheticBoundaryExamplesYieldAllFourSectionClasses(
        double webFlatMm, int expectedClass)
    {
        var g = SyntheticSection(webFlatMm);
        var r = Ntc2018SteelMixedAutomaticSectionClassifierD0M5.ClassifyGeometry(
            Request(n: -100, m: g.MomentKnm, section: "I-SYNTHETIC"),
            g.Dimensions, g.Gross, "S235", 235, Evidence());
        Assert.Equal(-0.5d, r.Elastic.Psi, 10);
        Assert.Equal(expectedClass, r.Classification.Web.Class);
        Assert.Equal(1, r.Classification.Flange.Class);
        Assert.Equal(expectedClass, r.ClassWithinDeclaredScope);
        Assert.Equal(expectedClass == 4, r.RequiresEffectiveSectionProperties);
        Assert.False(r.EstablishesSectionResistance);
    }

    [Fact]
    public void OutOfScopeGeometryShearAndNoCompressedElasticEdgeFailClosed()
    {
        Assert.Throws<NotSupportedException>(() => ClassifyCatalog(Request(n: -209.244)));
        Assert.Throws<NotSupportedException>(() => ClassifyCatalog(Request(v: 1)));
        Assert.Throws<NotSupportedException>(() => ClassifyCatalog(Request(n: 0)));
        Assert.Throws<NotSupportedException>(() => ClassifyCatalog(Request(m: 0)));
        Assert.Throws<NotSupportedException>(() => ClassifyCatalog(Request(n: 100, m: 1)));
        Assert.Throws<NotSupportedException>(() => ClassifyCatalog(Request(section: "INP-200")));
    }

    [Fact]
    public void GeometryPropertiesMaterialAndEditionEvidenceAreMandatory()
    {
        var g = SyntheticSection(600);
        var request = Request(n: -100, m: g.MomentKnm, section: "I-SYNTHETIC");
        Assert.Throws<NotSupportedException>(() => ClassifyCatalog(evidence:
            Evidence() with { StrongAxisMappedToFemMomentConfirmed = null }));
        Assert.Throws<NotSupportedException>(() => ClassifyCatalog(evidence:
            Evidence() with { UnperforatedConfirmed = false }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Ntc2018SteelMixedAutomaticSectionClassifierD0M5.ClassifyGeometry(
                request, g.Dimensions, g.Gross with { GrossAreaMm2 = 0 },
                "S235", 235, Evidence()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Ntc2018SteelMixedAutomaticSectionClassifierD0M5.ClassifyGeometry(
                request, g.Dimensions, g.Gross, "S235", double.NaN, Evidence()));
        Assert.Throws<NotSupportedException>(() => ClassifyCatalog(Request() with
        {
            Profile = Ntc2018SteelTensionProfileD1.Create() with
                { Code = new("EN 1993-1-1", "2005+A1:2014", "IT") }
        }));
    }

    [Fact]
    public void FemIdentityMustBeSameCombinationSideMemberAndStation()
    {
        var req = Request();
        Assert.Throws<ArgumentException>(() => ClassifyCatalog(req with
        { Effects = req.Effects with { MemberId = "OTHER" } }));
        Assert.Throws<ArgumentException>(() => ClassifyCatalog(req with
        { Effects = req.Effects with { SectionSide = null } }));
        Assert.Throws<ArgumentException>(() => ClassifyCatalog(req with
        { Effects = req.Effects with { TargetKind = DesignEffectTargetKind.LoadCase } }));
        Assert.Throws<ArgumentException>(() => ClassifyCatalog(req with
        { Effects = req.Effects with { PositionMetres = 5 } }));
        Assert.Throws<NotSupportedException>(() => ClassifyCatalog(req with
        { Effects = req.Effects with { BendingMomentZKnm = double.NaN } }));
    }

    private static Ntc2018SteelMixedStressParametersD0M1 ManualStressOnly(
        Ntc2018SteelMixedStressOnlyFootnoteEvidenceD0M5? extra,
        Ntc2018SteelMixedPsiBelowMinusOneEvidenceD0M2? legacy = null) =>
        new(0.4, -2, "Independent plastic yield block", "Independent elastic edges",
            true, "Exact FEM same station/side/N/M")
        {
            PsiBelowMinusOneStressOnlyEvidence = extra,
            PsiBelowMinusOneEvidence = legacy
        };

    [Fact]
    public void ExistingD0M1ClassifierAcceptsOnlyProvedStressBasedOrAlternative()
    {
        var g = SyntheticSection(600);
        var req = Request(n: 100, m: 25, section: "I-SYNTHETIC");
        var first = new Ntc2018SteelMixedStressOnlyFootnoteEvidenceD0M5(
            100, 200, "Independent NTC compressed/tensile web edges");
        var classification = Ntc2018SteelMixedIClassifierD0M1.ClassifyGeometry(
            req, g.Dimensions, "S235", 235, Evidence(), ManualStressOnly(first));
        Assert.Equal("PsiBelowOrEqualMinusOneD0M2", classification.Class3Branch);
        Assert.Contains("NOT REQUIRED", classification.Trace);
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelMixedIClassifierD0M1.ClassifyGeometry(req,
                g.Dimensions, "S235", 235, Evidence(),
                ManualStressOnly(first with { CompressionStressMagnitudeMpa = 300,
                    OppositeElasticTensileStressMagnitudeMpa = 600 })));
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelMixedIClassifierD0M1.ClassifyGeometry(req,
                g.Dimensions, "S235", 235, Evidence(),
                ManualStressOnly(first with { OppositeElasticTensileStressMagnitudeMpa = 300 })));
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelMixedIClassifierD0M1.ClassifyGeometry(req,
                g.Dimensions, "S235", 235, Evidence(),
                ManualStressOnly(first with { ElasticEdgeStressEvidence = null })));
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018SteelMixedIClassifierD0M1.ClassifyGeometry(req,
                g.Dimensions, "S235", 235, Evidence(), ManualStressOnly(first,
                new(100, 200, 0.01, 210000, "Conflicting double evidence"))));
    }
}
