using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.D0;

public sealed class Ntc2018SteelISectionClassificationD0Tests
{
    private static readonly CommercialRolledSteelCatalogM88B2 Sections =
        CommercialRolledSteelCatalogM88B2.Load(Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));

    private static readonly DesignCodeEdition Ntc = Ntc2018SteelISectionClassifierD0.Code;

    private static Ntc2018SteelClassificationEvidenceD0 Evidence() => new(
        UnperforatedSectionConfirmed: true,
        HotRolledParallelFlangeAndDoublySymmetricConfirmed: true,
        StrongAxisOfBendingConfirmed: true,
        SectionGeometryEvidence: "Independent issued geometry assumption for worked example",
        MaterialYieldEvidence: "Declared product strength and nominal thickness provenance",
        PureActionEvidence: "Pure compression or pure strong-axis bending independently established");

    private static Ntc2018SteelISectionClassificationD0 Catalog(
        string profile = "IPE-200", string grade = "S235",
        Ntc2018SteelPureActionD0 action = Ntc2018SteelPureActionD0.UniformCompression,
        Ntc2018SteelClassificationEvidenceD0? evidence = null,
        DesignCodeEdition? code = null) =>
        Ntc2018SteelISectionClassifierD0.ClassifyCatalogProfile(code ?? Ntc,
            Sections, Materials, profile, grade, action, evidence ?? Evidence());

    private static Ntc2018SteelISectionClassificationD0 Geom(
        Ntc2018SteelISectionDimensionsD0 dims,
        double fy = 235,
        Ntc2018SteelPureActionD0 action = Ntc2018SteelPureActionD0.UniformCompression,
        Ntc2018SteelClassificationEvidenceD0? evidence = null) =>
        Ntc2018SteelISectionClassifierD0.ClassifyGeometry(Ntc, "GEOMETRY-TEST", "DECLARED-GRADE",
            dims, fy, action, evidence ?? Evidence());

    [Fact]
    public void SourceEditionAndLimitTableAreNativeNtc2018()
    {
        var c = Catalog();
        Assert.Equal("DM 17 gennaio 2018", c.Code.DocumentId);
        Assert.Equal("NTC2018", c.Code.EditionId);
        Assert.Contains("4.2.III e 4.2.IV", c.EvidenceTrace);
        Assert.DoesNotContain("EN 1993-1-1:2005", c.EvidenceTrace);
        Assert.Equal(1d, c.Epsilon, 12); // S235 epsilon=sqrt(235/235).
        Assert.Equal(9d, c.Flange.Class1Limit, 12);
        Assert.Equal(10d, c.Flange.Class2Limit, 12);
        Assert.Equal(14d, c.Flange.Class3Limit, 12);
        Assert.Equal(33d, c.Web.Class1Limit, 12);
        Assert.Equal(38d, c.Web.Class2Limit, 12);
        Assert.Equal(42d, c.Web.Class3Limit, 12);
    }

    [Fact]
    public void StrongAxisPureBendingUsesDifferentWebLimits()
    {
        var c = Catalog(action: Ntc2018SteelPureActionD0.StrongAxisBending);
        Assert.Equal(72d, c.Web.Class1Limit, 12);
        Assert.Equal(83d, c.Web.Class2Limit, 12);
        Assert.Equal(124d, c.Web.Class3Limit, 12);
        Assert.Equal(Catalog().Flange.Class, c.Flange.Class);
    }

    [Theory]
    [InlineData(33, 1)]
    [InlineData(38, 2)]
    [InlineData(42, 3)]
    [InlineData(43, 4)]
    public void CompressionWebBoundaries_AreInclusive(int ratio, int expected)
    {
        var c = Geom(new(24 + ratio * 10, 100, 10, 8, 4));
        Assert.Equal(expected, c.Web.Class);
        Assert.Equal(expected, c.Class);
    }

    [Theory]
    [InlineData(72, 1)]
    [InlineData(83, 2)]
    [InlineData(124, 3)]
    [InlineData(125, 4)]
    public void BendingWebBoundaries_AreInclusive(int ratio, int expected)
    {
        var c = Geom(new(24 + ratio * 10, 100, 10, 8, 4),
            action: Ntc2018SteelPureActionD0.StrongAxisBending);
        Assert.Equal(expected, c.Web.Class);
        Assert.Equal(expected, c.Class);
    }

    [Theory]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(14, 3)]
    [InlineData(15, 4)]
    public void RolledFlangeBoundaries_AreInclusive(int ratio, int expected)
    {
        // c=(b-tw-2r)/2; with tf=8,tw=10,r=4 -> b=16*ratio+18.
        var c = Geom(new(100, 16 * ratio + 18, 10, 8, 4));
        Assert.Equal(expected, c.Flange.Class);
        Assert.Equal(expected, c.Class);
    }

    [Fact]
    public void ExampleIpe550S275_CompressionClassFourRequiresExternalEffectiveArea()
    {
        // Bernuzzi worked example A4.1: h550, b210, tw11.1, tf17.2, r24.
        var c = Geom(new(550, 210, 11.1, 17.2, 24), 275);
        Assert.Equal(1, c.Flange.Class);
        Assert.Equal(4, c.Web.Class);
        Assert.Equal(4, c.Class);
        Assert.True(c.RequiresEffectiveSectionProperties);
        Assert.False(c.ProvidesSectionResistance);
        Assert.False(c.EstablishesCompleteMemberCompliance);
        Assert.Equal("Web", c.GoverningParts);
    }

    [Fact]
    public void ExampleHea280S420_BendingFlangeClassThreeGoverns()
    {
        // Bernuzzi worked example A4.2: S420 fy explicitly declared outside catalog.
        var c = Geom(new(270, 280, 8, 13, 24), 420,
            Ntc2018SteelPureActionD0.StrongAxisBending);
        Assert.Equal(3, c.Flange.Class);
        Assert.Equal(1, c.Web.Class);
        Assert.Equal(3, c.Class);
        Assert.False(c.RequiresEffectiveSectionProperties);
        Assert.Equal("Flange", c.GoverningParts);
    }

    [Theory]
    [InlineData("IPE-180", "S235", 1, 1)]
    [InlineData("IPE-200", "S355", 2, 1)]
    [InlineData("IPE-270", "S355", 3, 1)]
    [InlineData("HEA-280", "S355", 3, 3)]
    [InlineData("HEB-200", "S355", 1, 1)]
    public void IndependentPublishedSectionCases_MatchScopedPureActions(
        string id, string grade, int expectedCompression, int expectedBending)
    {
        Assert.Equal(expectedCompression, Catalog(id, grade).Class);
        Assert.Equal(expectedBending, Catalog(id, grade,
            Ntc2018SteelPureActionD0.StrongAxisBending).Class);
    }

    [Fact]
    public void CatalogThicknessBandAndNonCertifiedProvenanceAreVisible()
    {
        var c = Catalog("IPE-200", "S355");
        Assert.Equal(355, c.YieldStrengthMpa);
        Assert.Equal(Math.Sqrt(235d / 355d), c.Epsilon, 12);
        Assert.Contains("OPPO=", c.GeometrySource);
        Assert.Contains("NON CERTIFICATO", c.GeometrySource);
        Assert.Contains("thickness=max(tf,tw)", c.YieldSource);
        Assert.Contains("NO resistance", c.EvidenceTrace);
    }

    [Fact]
    public void NtcDoesNotAcceptEc3OrOtherAnnexIdentity()
    {
        Assert.Throws<NotSupportedException>(() => Catalog(code: new(
            "EN 1993-1-1", "2005+A1:2014", "IT")));
        Assert.Throws<NotSupportedException>(() => Catalog(code: new(
            "DM 17 gennaio 2018", "NTC2018", "UNPROVEN")));
    }

    [Fact]
    public void TaperedInpAndUnknownCatalogValuesAreRejected()
    {
        Assert.Throws<NotSupportedException>(() => Catalog("INP-200"));
        Assert.Throws<KeyNotFoundException>(() => Catalog("IPE-999"));
        Assert.Throws<KeyNotFoundException>(() => Catalog("IPE-200", "S999"));
    }

    [Fact]
    public void MixedOrUnprovenLoadAndGeometryCannotBeClassified()
    {
        var d = new Ntc2018SteelISectionDimensionsD0(200, 100, 6, 8, 9);
        Assert.Throws<NotSupportedException>(() => Geom(d,
            action: (Ntc2018SteelPureActionD0)99));
        Assert.Throws<NotSupportedException>(() => Geom(d,
            evidence: Evidence() with { UnperforatedSectionConfirmed = null }));
        Assert.Throws<NotSupportedException>(() => Geom(d,
            evidence: Evidence() with { HotRolledParallelFlangeAndDoublySymmetricConfirmed = false }));
        Assert.Throws<NotSupportedException>(() => Geom(d,
            evidence: Evidence() with { SectionGeometryEvidence = "" }));
        Assert.Throws<NotSupportedException>(() => Geom(d,
            evidence: Evidence() with { MaterialYieldEvidence = null }));
        Assert.Throws<NotSupportedException>(() => Geom(d,
            evidence: Evidence() with { PureActionEvidence = "" }));
        Assert.Throws<NotSupportedException>(() => Geom(d,
            action: Ntc2018SteelPureActionD0.StrongAxisBending,
            evidence: Evidence() with { StrongAxisOfBendingConfirmed = null }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidFyIsRejected(double fy) => Assert.Throws<ArgumentOutOfRangeException>(
        () => Geom(new(200, 100, 6, 8, 9), fy));

    [Fact]
    public void DegenerateOrNonFiniteDimensionsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => Geom(new(50, 100, 6, 18, 9)));
        Assert.Throws<ArgumentException>(() => Geom(new(200, 20, 6, 10, 8)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Geom(new(200, 100, 6, -8, 9)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Geom(new(double.NaN, 100, 6, 8, 9)));
    }

    [Fact]
    public void OldEc3ClassifierRemainsSeparatelyIdentified()
    {
        var ec3 = SteelISectionClassifierM90A.ClassifyCatalogProfile(
            new("EN 1993-1-1", "2005+A1:2014", "IT-DECLARED"), Sections, Materials,
            "IPE-270", "S355", SteelISectionActionM90A.PureUniformCompression);
        var ntc = Catalog("IPE-270", "S355");
        Assert.Equal(ec3.Class, ntc.Class); // Only the narrow, audited pure case.
        Assert.NotEqual("DM 17 gennaio 2018", ec3.CodeReference);
        Assert.DoesNotContain("NTC2018", ec3.CodeReference);
        Assert.Equal("NTC2018", ntc.Code.EditionId);
    }
}
