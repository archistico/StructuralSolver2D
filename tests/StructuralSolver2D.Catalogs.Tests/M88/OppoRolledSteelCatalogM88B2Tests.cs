using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Core.Model.Sections;

namespace StructuralSolver2D.Catalogs.Tests.M88;

public sealed class OppoRolledSteelCatalogM88B2Tests
{
    private static CommercialRolledSteelCatalogM88B2 Sample() =>
        CommercialRolledSteelCatalogM88B2.Load(
            Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));

    private static PublishedRolledSteelSectionM88B2 Entry() => Sample().Find("HEA-200").Definition;
    private static CommercialRolledSteelCatalogM88B2 One(PublishedRolledSteelSectionM88B2 item) =>
        new("test", new[] { item });

    [Fact]
    public void AllFortyEntries_HaveExplicitHistoricalSourceAndIndicativeStatus()
    {
        var sections = Sample();
        Assert.Equal(40, sections.Sections.Count);
        Assert.False(sections.IncludesNormativeVerification);
        foreach (string family in new[] { "IPE", "HEA", "HEB", "INP" })
        {
            Assert.Equal(10, sections.FindFamily(family).Count);
        }
        Assert.All(sections.Sections, item =>
        {
            var row = item.Definition;
            Assert.Equal("IndicativeThirdParty", row.DataStatus);
            Assert.Equal("OPPO - tabelle profilati metallici", row.SourceName);
            Assert.Equal("2026-10-24", row.SourceCheckedOn);
            Assert.Equal($"https://www.oppo.it/tabelle/profilati_{row.Family.ToLowerInvariant()}.htm", row.SourcePageUrl);
            Assert.True(row.RootRadiusMm > 0);
            Assert.True(row.StrongRadiusCm > 0);
            Assert.True(row.WeakRadiusCm > 0);
            Assert.Equal(row.Family == "INP", item.HasInclinedFlanges);
        });
    }

    [Theory]
    [InlineData("IPE-80", 7.64, 80.14, 8.49, 20.03, 3.69, 6.0)]
    [InlineData("IPE-270", 45.95, 5790, 419.9, 428.9, 62.20, 36.1)]
    [InlineData("HEA-100", 21.24, 349.2, 133.8, 72.76, 26.76, 16.7)]
    [InlineData("HEA-200", 53.83, 3692, 1326, 388.6, 133.6, 42.3)]
    [InlineData("HEB-100", 26.04, 449.5, 167.3, 89.91, 33.45, 20.4)]
    [InlineData("HEB-240", 106.0, 11260, 3923, 938.3, 326.9, 83.2)]
    [InlineData("INP-80", 7.57, 77.7, 6.28, 19.4, 2.99, 5.94)]
    [InlineData("INP-240", 46.1, 4250, 221, 354, 41.7, 36.2)]
    public void RepresentativeRows_MatchExplicitOppoTableValues(
        string id, double a, double ix, double iy, double wx, double wy, double mass)
    {
        var p = Sample().Find(id).Definition;
        Assert.Equal(a, p.AreaCm2, 9);
        Assert.Equal(ix, p.StrongInertiaCm4, 9);
        Assert.Equal(iy, p.WeakInertiaCm4, 9);
        Assert.Equal(wx, p.StrongElasticModulusCm3, 9);
        Assert.Equal(wy, p.WeakElasticModulusCm3, 9);
        Assert.Equal(mass, p.MassKgPerM, 9);
    }

    [Theory]
    [InlineData("IPE-200", "UNI 5398-78", 200, 100, 5.6, 8.5, 12)]
    [InlineData("HEA-200", "UNI 5397-78", 190, 200, 6.5, 10.0, 18)]
    [InlineData("HEB-200", "UNI 5397-78", 200, 200, 9.0, 15.0, 18)]
    [InlineData("INP-200", "UNI 5679-73", 200, 90, 7.5, 11.3, 7.5)]
    public void DimensionsAndHistoricalTableReference_AreFamilySpecific(
        string id, string table, double height, double width, double web, double flange, double root)
    {
        var p = Sample().Find(id).Definition;
        Assert.Equal(table, p.ReferenceTable);
        Assert.Equal(height, p.HeightMm);
        Assert.Equal(width, p.WidthMm);
        Assert.Equal(web, p.WebThicknessMm);
        Assert.Equal(flange, p.FlangeThicknessMm);
        Assert.Equal(root, p.RootRadiusMm);
    }

    [Theory]
    [InlineData("IPE-240", 39.12, 3892, 283.6, 324.3, 47.27)]
    [InlineData("HEA-200", 53.83, 3692, 1326, 388.6, 133.6)]
    [InlineData("HEB-200", 78.08, 5696, 2003, 569.6, 200.3)]
    [InlineData("INP-200", 33.4, 2140, 117, 214, 26)]
    public void SolverProperties_AreExactUnitConversionsWithoutIdealizedRecalculation(
        string id, double a, double strongI, double weakI, double strongW, double weakW)
    {
        var s = Sample().Find(id);
        Assert.Equal(a * 1e-4, s.Properties.Area, 12);
        Assert.Equal(strongI * 1e-8, s.Properties.InPlaneInertia, 12);
        Assert.Equal(weakI * 1e-8, s.Properties.OutOfPlaneInertia, 12);
        Assert.Equal(strongW * 1e-6, s.Properties.InPlaneElasticSectionModulus, 12);
        Assert.Equal(weakW * 1e-6, s.Properties.OutOfPlaneElasticSectionModulus, 12);
        Assert.Equal(s.Properties.InPlaneInertia, s.ToSolverSection().MomentOfInertia);
    }

    [Fact]
    public void HeaHebAndInp_RemainDifferentPhysicalShapesWithDifferentStiffness()
    {
        var data = Sample();
        var a = data.Find("HEA-200");
        var b = data.Find("HEB-200");
        var n = data.Find("INP-200");
        Assert.Equal(.190, a.ToSolverSection().Height!.Value, 10);
        Assert.Equal(.200, b.ToSolverSection().Height!.Value, 10);
        Assert.True(b.Properties.Area > a.Properties.Area);
        Assert.True(b.Properties.InPlaneInertia > a.Properties.InPlaneInertia);
        Assert.True(a.Properties.InPlaneInertia > n.Properties.InPlaneInertia);
        Assert.True(n.HasInclinedFlanges);
        Assert.False(a.HasInclinedFlanges);
        Assert.False(b.HasInclinedFlanges);
    }

    [Fact]
    public void ExistingM88aElasticPreset_CanBeSelectedButNormativeStrengthCannotBeInferred()
    {
        var materialCatalog = AnalysisCatalogM88A.Load(
            Path.Combine(AppContext.BaseDirectory, "analysis-starter.json"));
        var p = Sample().SelectForElasticAnalysis("HEB-200", materialCatalog, "S355");
        Assert.Equal(78.08e-4, p.Section.Area, 12);
        Assert.Equal(5696e-8, p.Section.MomentOfInertia, 12);
        Assert.Equal("IndicativeThirdParty", p.GeometryDataStatus);
        Assert.False(p.IncludesNormativeStrengthOrDesignFactors);
        Assert.Throws<InvalidOperationException>(() =>
            Sample().SelectForElasticAnalysis("INP-200", materialCatalog, "C24"));
    }

    [Fact]
    public void FromJsonToJson_PreservesEveryRowAndSource()
    {
        var original = Sample();
        var restored = CommercialRolledSteelCatalogM88B2.FromJson(original.ToJson());
        Assert.Equal(40, restored.Sections.Count);
        foreach (var p in original.Sections)
        {
            var other = restored.Find(p.Definition.Id);
            Assert.Equal(p.Definition, other.Definition);
            Assert.Equal(p.Properties, other.Properties);
        }
    }

    [Fact]
    public void UnknownEntriesAndAmbiguousDuplicates_AreNotSilentlyChosen()
    {
        Assert.Throws<KeyNotFoundException>(() => Sample().Find("HEM-200"));
        Assert.Throws<NotSupportedException>(() => Sample().FindFamily("HEM"));
        Assert.Throws<ArgumentException>(() => One(Entry() with { Id = "IPE-200" }));
        Assert.Throws<ArgumentException>(() => new CommercialRolledSteelCatalogM88B2("duplicate", new[]
        {
            Entry(), Entry() with { Id = "hea-200" }
        }));
    }

    [Fact]
    public void MissingSourceOrChangedHistoricalReference_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => One(Entry() with { SourcePageUrl = "https://www.oppo.it/tabelle/profilati_heb.htm" }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { ReferenceTable = "EN 10365:2026" }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { SourceName = "" }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { DataStatus = "Certified" }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { SourceCheckedOn = "" }));
        Assert.Throws<NotSupportedException>(() => One(Entry() with { Family = "UPN" }));
    }

    [Fact]
    public void InvalidNumbersAxisSwapAndBrokenGeometry_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => One(Entry() with { AreaCm2 = double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => One(Entry() with { RootRadiusMm = 0 }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { StrongInertiaCm4 = 10 }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { StrongElasticModulusCm3 = 4 }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { WeakRadiusCm = 0.3 }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { MassKgPerM = 100 }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { FlangeThicknessMm = 120 }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { WebThicknessMm = 300 }));
    }

    [Fact]
    public void UnsupportedJsonSchemaOrMissingRows_FailsClosed()
    {
        var json = Sample().ToJson();
        Assert.Throws<NotSupportedException>(() => CommercialRolledSteelCatalogM88B2.FromJson(
            json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2")));
        Assert.Throws<InvalidOperationException>(() => CommercialRolledSteelCatalogM88B2.FromJson(
            "{\"schemaVersion\":1,\"title\":\"no rows\"}"));
    }

    [Fact]
    public void M88BFirstIpeCatalog_IsStillUsableAndUnmodified()
    {
        var old = CommercialSteelCatalogM88B.Load(
            Path.Combine(AppContext.BaseDirectory, "steel-ipe-published-2026.json"));
        Assert.Equal(8, old.Sections.Count);
        Assert.Equal("PublishedThirdParty", old.Find("IPE-200").Definition.DataStatus);
        Assert.Equal("IndicativeThirdParty", Sample().Find("IPE-200").Definition.DataStatus);
        Assert.Equal(old.Find("IPE-200").Properties.Area, Sample().Find("IPE-200").Properties.Area, 12);
    }
}
