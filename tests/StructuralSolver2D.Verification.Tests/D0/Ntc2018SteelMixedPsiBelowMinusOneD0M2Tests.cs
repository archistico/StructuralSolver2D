using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D0;

public sealed class Ntc2018SteelMixedPsiBelowMinusOneD0M2Tests
{
    private static DesignCheckRequest Request(string sectionId = "I-EXAMPLE", double signedN = 100) => new(
        new DesignMemberContext("M1", DesignMaterial.Steel, sectionId, 4),
        DesignLimitState.Ultimate, Ntc2018SteelTensionProfileD1.Create(),
        new DesignEffectSnapshot("FEM-A1", "ULS-1", DesignEffectOrigin.FemAnalysis,
            2, signedN, 0, 25)
        {
            MemberId = "M1", TargetKind = DesignEffectTargetKind.LoadCombination,
            SectionSide = DesignEffectSectionSide.Continuous
        }, Array.Empty<string>());

    private static Ntc2018SteelMixedClassificationEvidenceD0M1 Evidence() => new(
        UnperforatedConfirmed: true,
        HotRolledParallelFlangeDoublySymmetricConfirmed: true,
        StrongAxisMappedToFemMomentConfirmed: true,
        GeometryEvidence: "Independently checked I/H rolled unperforated geometry",
        GradeAndYieldEvidence: "Declared grade and yield stress of the actual product");

    private static Ntc2018SteelMixedPsiBelowMinusOneEvidenceD0M2 Footnote(
        double psi, double compression = 200, double strain = 0.02, double e = 210000,
        double? oppositeElasticTension = null,
        string? source = "Independent same-station compression stress / tensile strain / E") =>
        new(compression, oppositeElasticTension ?? -psi * compression, strain, e, source);

    private static Ntc2018SteelMixedStressParametersD0M1 Stress(
        double psi, Ntc2018SteelMixedPsiBelowMinusOneEvidenceD0M2? special = null,
        double? alpha = null) =>
        new(alpha ?? (psi < -1 ? 0.4 : 0.6), psi,
            "Plastic web stress block for same signed N/M",
            "Elastic web edge stress ratio for same signed N/M",
            ConsistentWithSameFemActionConfirmed: true,
            SameActionAndStationEvidence: "M1 FEM-A1 ULS-1 x=2 continuous, signed N and M nonzero V=0")
        { PsiBelowMinusOneEvidence = special };

    private static Ntc2018SteelISectionDimensionsD0 WebRatio(double webOverThickness) =>
        new(24 + 10 * webOverThickness, 100, 10, 8, 4);

    private static Ntc2018SteelMixedIClassificationD0M1 Classify(
        double psi, double webOverThickness,
        Ntc2018SteelMixedPsiBelowMinusOneEvidenceD0M2? special = null,
        double? signedN = null, double? alpha = null) =>
        Ntc2018SteelMixedIClassifierD0M1.ClassifyGeometry(
            Request(signedN: signedN ?? (psi < -1 ? 100 : -100)),
            WebRatio(webOverThickness), "S235", 235,
            Evidence(), Stress(psi, special, alpha));

    [Fact]
    public void PsiJustBelowMinusOne_UsesSpecialFormulaAndKeepsBoundaryClassBehavior()
    {
        const double psi = -1.0001;
        var c = Classify(psi, 124, Footnote(psi));
        Assert.InRange(c.Web.Class3Limit, 124d, 125d);
        Assert.Equal(3, c.Web.Class);
        Assert.Equal(3, c.Class);
        Assert.Equal("PsiBelowOrEqualMinusOneD0M2", c.Class3Branch);
        Assert.Contains("class3=psi<=-1", c.Trace);
        Assert.Contains("fy/E", c.Trace);
        Assert.False(c.ProvidesResistanceOrNmInteractionCheck);
        Assert.False(c.EstablishesCompleteMemberCompliance);
        Assert.Equal(100d, c.SignedAxialForceKn);
        Assert.Equal(4, Classify(psi, 125, Footnote(psi)).Class);
    }

    [Fact]
    public void PsiExactlyMinusOne_IsPureBendingLimit_NotMixedTensionOrCompression()
    {
        // NTC special analytical limit 124 epsilon is tested by Python golden;
        // at psi=-1 exact, a doubly symmetric linearly elastic I/H has N=0.
        Assert.Throws<NotSupportedException>(() => Classify(-1, 124, Footnote(-1), signedN: 100));
        Assert.Throws<NotSupportedException>(() => Classify(-1, 124, Footnote(-1), signedN: -100));
        Assert.Throws<NotSupportedException>(() => Classify(-1, 124, Footnote(-1), signedN: 0));
    }

    [Theory]
    [InlineData(-4, 620)]     // 62*(1+4)*sqrt(4) exactly
    [InlineData(-9, 1860)]    // 62*(1+9)*sqrt(9) exactly
    public void PsiNegativeSquare_HasDirectNtcThreshold(double psi, double limit)
    {
        var c = Classify(psi, limit, Footnote(psi));
        Assert.Equal(limit, c.Web.Class3Limit, 12);
        Assert.Equal(3, c.Web.Class);
    }

    [Theory]
    [InlineData(250, 3)]
    [InlineData(300, 4)]
    public void PsiMinusTwo_HasCorrectSquareRootAndClass(double ratio, int expected)
    {
        var c = Classify(-2, ratio, Footnote(-2));
        Assert.Equal(62d * 3d * Math.Sqrt(2d), c.Web.Class3Limit, 11);
        Assert.Equal(expected, c.Web.Class);
    }

    [Fact]
    public void NtcFootnoteAllowsCompressionNotExceedingYield_IncludingEquality()
    {
        var c = Classify(-1.0001, 150,
            Footnote(-1.0001, compression: 235, strain: 0.0012));
        Assert.Equal(4, c.Web.Class); // 150 exceeds class-3 limit near 124.
        Assert.Contains("sigmaC=235MPa<=fy=True", c.Trace);
    }

    [Fact]
    public void FootnoteAlternativeAOnlyCanHavePhysicallyConsistentElasticEdges()
    {
        // N tensile, elastic compression=100 MPa, tension=200 MPa,
        // tensile strain=200/E below fyk/E, yet sigmaC<=fyk.
        var c = Classify(-2, 150, Footnote(-2, compression: 100,
            strain: 200d / 210000));
        Assert.Equal(3, c.Web.Class);
        Assert.Contains("=False; E=", c.Trace);
    }

    [Fact]
    public void NtcFootnoteAllowsTensileStrainAboveYield_WhenCompressionIsHigher()
    {
        var c = Classify(-2, 150, Footnote(-2, compression: 300, strain: 0.003));
        Assert.Equal(3, c.Web.Class);
        Assert.Contains("fy/E=", c.Trace);
        Assert.Contains("=True", c.Trace);
    }

    [Fact]
    public void AtExactTensileYieldStrain_ExceedanceIsNotEstablished()
    {
        Assert.Throws<NotSupportedException>(() => Classify(-2, 100,
            Footnote(-2, compression: 300, strain: 235d / 210000)));
    }

    [Fact]
    public void MissingSpecialEvidenceCannotSilentlyEnableFormerlyUnsupportedBranch()
    {
        Assert.Throws<NotSupportedException>(() => Classify(-1, 100));
        Assert.Throws<NotSupportedException>(() => Classify(-2, 100));
    }

    [Fact]
    public void OppositePsiBranchMustMatchSignedNetAxialStateAndPlasticFraction()
    {
        Assert.Throws<NotSupportedException>(() => Classify(-2, 150,
            Footnote(-2), signedN: -100)); // compression cannot create psi<-1 on symmetric I/H
        Assert.Throws<NotSupportedException>(() => Classify(-0.5, 70,
            Footnote(-0.5), signedN: 100)); // tension cannot create psi>-1
        Assert.Throws<NotSupportedException>(() => Classify(-2, 150,
            Footnote(-2), alpha: 0.5)); // tensile N implies alpha<0.5
        Assert.Throws<NotSupportedException>(() => Classify(-2, 150,
            Footnote(-2), alpha: 0.6));
    }

    [Fact]
    public void UnsupportedOrIncompleteFootnoteEvidenceFailsClosed()
    {
        Assert.Throws<NotSupportedException>(() => Classify(-2, 100,
            Footnote(-2, compression: 300, strain: 0.0002)));
        Assert.Throws<NotSupportedException>(() => Classify(-2, 100,
            Footnote(-2, source: null)));
        Assert.Throws<NotSupportedException>(() => Classify(-2, 100,
            Footnote(-2, compression: double.PositiveInfinity)));
        Assert.Throws<NotSupportedException>(() => Classify(-2, 100,
            Footnote(-2, compression: 0)));
        Assert.Throws<NotSupportedException>(() => Classify(-2, 100,
            Footnote(-2, strain: -0.1)));
        Assert.Throws<NotSupportedException>(() => Classify(-2, 100,
            Footnote(-2, strain: double.NaN)));
        Assert.Throws<NotSupportedException>(() => Classify(-2, 100,
            Footnote(-2, e: 0)));
        Assert.Throws<NotSupportedException>(() => Classify(-2, 100,
            Footnote(-2, oppositeElasticTension: 0)));
        Assert.Throws<NotSupportedException>(() => Classify(-2, 100,
            Footnote(-2, oppositeElasticTension: 1000)));
    }

    [Fact]
    public void UnsupportedOverflowingThresholdMustFailClosed()
    {
        Assert.Throws<NotSupportedException>(() => Classify(-1e308, 100,
            Footnote(-1e308, compression: 1e-308, oppositeElasticTension: 1)));
    }

    [Fact]
    public void OldAboveMinusOneBranchRemainsUntouchedEvenWhenSpecialDataIsProvided()
    {
        var c = Classify(-0.5, 70, Footnote(-0.5, compression: 999, strain: 0));
        Assert.Equal(42d / (0.67 - 0.165), c.Web.Class3Limit, 11);
        Assert.Equal("PsiAboveMinusOneD0M1", c.Class3Branch);
        Assert.Equal(3, c.Class);
    }

    [Fact]
    public void CommercialIpeStillUsesCatalogGradeBandWithoutBecomingCertified()
    {
        var sections = CommercialRolledSteelCatalogM88B2.Load(
            Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
        var materials = CharacteristicMaterialCatalogM88C.Load(
            Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));
        var c = Ntc2018SteelMixedIClassifierD0M1.ClassifyCatalogProfile(
            Request("IPE-200"), sections, materials, "S235", Evidence(), Stress(-2, Footnote(-2)));
        Assert.Equal("PsiBelowOrEqualMinusOneD0M2", c.Class3Branch);
        Assert.Contains("OPPO=", c.Trace);
        Assert.Contains("NOT CERTIFIED", c.Trace);
        Assert.False(c.EstablishesCompleteMemberCompliance);
    }
}
