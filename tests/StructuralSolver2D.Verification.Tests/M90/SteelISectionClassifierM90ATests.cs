using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.M90;

public sealed class SteelISectionClassifierM90ATests
{
    private static readonly DesignCodeEdition Edition =
        new("EN 1993-1-1", "2005+A1:2014", "IT-USER-DECLARED-NOT-AN-AUTOMATIC-CERTIFICATION");
    private static readonly CommercialRolledSteelCatalogM88B2 Profiles =
        CommercialRolledSteelCatalogM88B2.Load(Path.Combine(AppContext.BaseDirectory,
            "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory,
            "characteristic-strength-classes.json"));

    private static SteelISectionClassificationM90A FromCatalog(
        string id="IPE-200", string grade="S235",
        SteelISectionActionM90A action = SteelISectionActionM90A.PureUniformCompression) =>
        SteelISectionClassifierM90A.ClassifyCatalogProfile(Edition, Profiles, Materials, id, grade, action);

    private static SteelISectionClassificationM90A FromGeometry(
        SteelISectionDimensionsM90A dims, double fy=235,
        SteelISectionActionM90A action=SteelISectionActionM90A.PureUniformCompression) =>
        SteelISectionClassifierM90A.ClassifyGeometry("REFERENCE-EXAMPLE", "TEST-GRADE", dims,
            fy, action, "Independent worked example with declared dimensions", "Declared test fy; not a product certification");

    [Fact]
    public void Bernuzzi_A41_Ipe550_S275_InPureCompression_WebClassFourControls()
    {
        // Bernuzzi, Progetto e verifica delle strutture in acciaio, 2nd ed.,
        // application A4.1, printed pp. 129-130 (PDF pp. 145-146).
        var classification = FromGeometry(new(550, 210, 11.1, 17.2, 24), 275);
        Assert.Equal(1, classification.Flange.Class);
        Assert.Equal(4, classification.Web.Class);
        Assert.Equal(4, classification.Class);
        Assert.Equal("Web", classification.GoverningParts);
        Assert.True(classification.RequiresEffectiveSectionProperties);
        Assert.InRange(classification.Web.Slenderness, 42.125, 42.135);
        Assert.InRange(classification.Web.Class3Limit, 38.82, 38.84);
        Assert.False(classification.IsCompleteMemberVerification);
    }

    [Fact]
    public void Bernuzzi_A42_Hea280_S420_StrongAxisPureBending_FlangeClassThreeControls()
    {
        // Bernuzzi application A4.2, printed pp. 130-131 (PDF pp. 146-147).
        // S420 is deliberate INPUT to this worked example; not present in M88-C catalog.
        var classification = FromGeometry(new(270, 280, 8, 13, 24), 420,
            SteelISectionActionM90A.PureStrongAxisBending);
        Assert.Equal(3, classification.Flange.Class);
        Assert.Equal(1, classification.Web.Class);
        Assert.Equal(3, classification.Class);
        Assert.Equal("Flange", classification.GoverningParts);
        Assert.InRange(classification.Flange.Slenderness, 8.615, 8.62);
        Assert.InRange(classification.Web.Slenderness, 24.499, 24.501);
        Assert.False(classification.RequiresEffectiveSectionProperties);
        Assert.False(classification.AllowsAutomaticApproval);
    }

    [Theory]
    [InlineData("IPE-180", "S235", 1, 1)]
    [InlineData("IPE-180", "S355", 2, 1)]
    [InlineData("IPE-200", "S235", 1, 1)]
    [InlineData("IPE-200", "S355", 2, 1)]
    [InlineData("IPE-240", "S275", 2, 1)]
    [InlineData("IPE-270", "S235", 2, 1)]
    [InlineData("IPE-270", "S355", 3, 1)]
    [InlineData("HEA-280", "S235", 1, 1)]
    [InlineData("HEA-280", "S275", 2, 2)]
    [InlineData("HEA-280", "S355", 3, 3)]
    [InlineData("HEB-200", "S355", 1, 1)]
    public void SelectedBernuzziTables_AgreeWithOppOProfileDimensionsForTwoPureActions(
        string sectionId, string steelGrade, int compressionClass, int bendingClass)
    {
        // Spot values are transcribed test expectations from Bernuzzi Tables 4.4-4.6,
        // printed pp. 125-127; catalog dimensions come independently from OPPO.
        Assert.Equal(compressionClass, FromCatalog(sectionId, steelGrade).Class);
        Assert.Equal(bendingClass, FromCatalog(sectionId, steelGrade,
            SteelISectionActionM90A.PureStrongAxisBending).Class);
    }

    [Theory]
    [InlineData(33, 1)]
    [InlineData(38, 2)]
    [InlineData(42, 3)]
    [InlineData(43, 4)]
    public void WebPureCompression_ClassBoundaries_UseNoRelaxation(int ratio, int expected)
    {
        // Strong flange made non-governing; for S235 epsilon = 1.
        var dims = new SteelISectionDimensionsM90A(24 + ratio*10, 100, 10, 8, 4);
        var result = FromGeometry(dims);
        Assert.Equal(expected, result.Web.Class);
        Assert.Equal(expected, result.Class);
    }

    [Theory]
    [InlineData(72, 1)]
    [InlineData(83, 2)]
    [InlineData(124, 3)]
    [InlineData(125, 4)]
    public void WebPureStrongAxisBending_ClassBoundaries(int ratio, int expected)
    {
        var dims = new SteelISectionDimensionsM90A(24 + ratio*10, 100, 10, 8, 4);
        var result = FromGeometry(dims, action:SteelISectionActionM90A.PureStrongAxisBending);
        Assert.Equal(expected, result.Web.Class);
        Assert.Equal(expected, result.Class);
    }

    [Theory]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(14, 3)]
    [InlineData(15, 4)]
    public void OutstandFlange_ClassBoundaries(int ratio, int expected)
    {
        // flange c=(b-tw-2r)/2; b = 2*(tf*ratio) + tw + 2r.
        var dims = new SteelISectionDimensionsM90A(100, 16*ratio+18, 10, 8, 4);
        var result = FromGeometry(dims);
        Assert.Equal(expected, result.Flange.Class);
        Assert.Equal(expected, result.Class);
    }

    [Fact]
    public void EpsilonAndRoundedMaterialProperties_AreReportedWithProvenance()
    {
        var c = FromCatalog("IPE-200", "S355");
        Assert.InRange(c.Epsilon, 0.813, 0.814);
        Assert.Equal(355, c.YieldStrengthMpa);
        Assert.Contains("OPPO", c.GeometryReference);
        Assert.Contains("thickness=max(tf,tw)", c.YieldReference);
        Assert.Contains("5.5, Table 5.2", c.CodeReference);
        Assert.Contains("NOT checked", c.Evidence);
    }

    [Fact]
    public void PureBendingAndCompressionClassificationCanDifferForSameSection()
    {
        var c = FromCatalog("IPE-270", "S355");
        var b = FromCatalog("IPE-270", "S355", SteelISectionActionM90A.PureStrongAxisBending);
        Assert.Equal(3, c.Class);
        Assert.Equal(1, b.Class);
        Assert.Equal(c.Flange.Class, b.Flange.Class);
        Assert.NotEqual(c.Web.Class, b.Web.Class);
    }

    [Theory]
    [InlineData("INP-200")]
    public void TaperedFlangeProfilesNeverUseParallelFlangeRules(string id) =>
        Assert.Throws<NotSupportedException>(() => FromCatalog(id));

    [Fact]
    public void UnknownProfileAndSteelGrade_FailClosed()
    {
        Assert.Throws<KeyNotFoundException>(() => FromCatalog("IPE-999"));
        Assert.Throws<KeyNotFoundException>(() => FromCatalog("IPE-200", "S999"));
    }

    [Fact]
    public void IncorrectNormativeEditionOrMissingAnnex_CannotBeReinterpreted()
    {
        foreach (var code in new[] {
            new DesignCodeEdition("EN 1993-1-1", "2022", "IT"),
            new DesignCodeEdition("NTC", "2018", "IT"),
            new DesignCodeEdition("EN 1993-1-1", "2005+A1:2014", "")
        })
        {
            Assert.Throws<NotSupportedException>(() =>
                SteelISectionClassifierM90A.ClassifyCatalogProfile(code, Profiles, Materials, "IPE-200", "S235",
                    SteelISectionActionM90A.PureUniformCompression));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-235)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidFyIsRejected(double fy) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => FromGeometry(new(200,100,6,8,9),fy));

    [Fact]
    public void InvalidGeometryCannotBeClassified()
    {
        Assert.Throws<ArgumentException>(() => FromGeometry(new(50,100,6,18,9)));
        Assert.Throws<ArgumentException>(() => FromGeometry(new(200,20,6,10,8)));
        Assert.Throws<ArgumentOutOfRangeException>(() => FromGeometry(new(200,100,6,-8,9)));
        Assert.Throws<ArgumentOutOfRangeException>(() => FromGeometry(new(double.NaN,100,6,8,9)));
        Assert.Throws<ArgumentOutOfRangeException>(() => FromGeometry(new(200,100,6,8,double.PositiveInfinity)));
    }

    [Fact]
    public void UnknownLoadActionAndMissingProvenanceAreRejected()
    {
        Assert.Throws<NotSupportedException>(() => FromGeometry(new(200,100,6,8,9),
            action: (SteelISectionActionM90A)99));
        Assert.Throws<ArgumentException>(() => SteelISectionClassifierM90A.ClassifyGeometry(
            "X", "S235", new(200,100,6,8,9),235,
            SteelISectionActionM90A.PureUniformCompression, "", "Steel reference"));
    }

    [Fact]
    public void ItIsNeverAFullSteelCheck_AndClassFourRequiresSeparateEffectiveProperties()
    {
        var four = FromGeometry(new(550,210,11.1,17.2,24),275);
        Assert.True(four.RequiresEffectiveSectionProperties);
        Assert.False(four.IsCompleteMemberVerification);
        Assert.False(four.AllowsAutomaticApproval);
        Assert.Contains("effective widths for class 4",four.Evidence);
    }
}
