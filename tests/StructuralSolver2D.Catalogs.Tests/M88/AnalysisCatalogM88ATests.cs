using System.Text.Json;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Core.Model.Sections;

namespace StructuralSolver2D.Catalogs.Tests.M88;

public sealed class AnalysisCatalogM88ATests
{
    private static string SamplePath => Path.Combine(AppContext.BaseDirectory, "analysis-starter.json");
    private static AnalysisCatalogM88A Sample() => AnalysisCatalogM88A.Load(SamplePath);
    private static ElasticMaterialEntryM88A Steel(string id = "S235") =>
        new(id, "Steel demo", CatalogMaterialFamilyM88A.Steel, 210_000_000, 78.5);
    private static AnalysisSectionEntryM88A Rect(string id = "R") =>
        new(CatalogMaterialFamilyM88A.Timber,
            new SectionProfileDefinitionM80(id, "Wood rectangle", "TimberRectangular", WidthMm: 80, HeightMm: 160));

    [Fact]
    public void StarterCatalog_IsAnalysisOnlyAndHasSeparateFamilies()
    {
        var catalog = Sample();
        Assert.True(catalog.Sections.Count >= 12);
        Assert.Equal(5, catalog.Materials.Count);
        Assert.False(catalog.IncludesNormativeMaterialStrengths);
        Assert.Contains(catalog.Sections, s => s.Family == CatalogMaterialFamilyM88A.Steel);
        Assert.Contains(catalog.Sections, s => s.Family == CatalogMaterialFamilyM88A.Timber);
    }

    [Fact]
    public void TimberRectangle_ConvertsMillimetresToInternalSIExactly()
    {
        var selected = Sample().SelectForElasticAnalysis("WOOD-80X160", "C24");
        Assert.Equal(0.08 * 0.16, selected.Section.Area, 12);
        Assert.Equal(0.08 * Math.Pow(0.16, 3) / 12, selected.Section.MomentOfInertia, 12);
        Assert.Equal(11_000_000, selected.Material.ElasticModulus);
        Assert.Equal(CatalogMaterialFamilyM88A.Timber, selected.Family);
        Assert.False(selected.IncludesNormativeStrengthOrDesignFactors);
    }

    [Fact]
    public void SteelHollowSection_UsesExistingExactIdealizedM80Formula()
    {
        var selected = Sample().SelectForElasticAnalysis("STEEL-RHS-100X50X4-IDEAL", "s355");
        Assert.Equal(0.1 * 0.05 - 0.092 * 0.042, selected.Section.Area, 12);
        Assert.Equal(210_000_000, selected.Material.ElasticModulus);
        Assert.Equal("Illustrative", selected.GeometryDataStatus);
    }

    [Fact]
    public void IShape_IsExplicitlyIllustrativeAndNotAnOfficialIPESection()
    {
        var selected = Sample().SelectForElasticAnalysis("STEEL-I-100-IDEAL", "S235");
        Assert.Equal(2 * 0.055 * 0.0057 + 0.0041 * (0.1 - 2 * 0.0057), selected.Section.Area, 12);
        Assert.Equal("Illustrative", selected.GeometryDataStatus);
        Assert.False(selected.IncludesNormativeStrengthOrDesignFactors);
    }

    [Fact]
    public void UnspecifiedOrUnknownMaterialFamily_IsRejectedFailClosed()
    {
        Assert.Throws<ArgumentException>(() => new AnalysisCatalogM88A("invalid",
            new[] { Steel() with { Family = CatalogMaterialFamilyM88A.Unknown } }, Array.Empty<AnalysisSectionEntryM88A>()));
        Assert.Throws<ArgumentException>(() => new AnalysisCatalogM88A("invalid",
            new[] { Steel() }, new[] { Rect() with { Family = CatalogMaterialFamilyM88A.Unknown } }));
    }

    [Fact]
    public void FamilyMustMatchGeometryType()
    {
        Assert.Throws<ArgumentException>(() => new AnalysisCatalogM88A("wrong shape",
            new[] { Steel() }, new[] { Rect() with { Family = CatalogMaterialFamilyM88A.Steel } }));
        Assert.Throws<ArgumentException>(() => new AnalysisCatalogM88A("wrong shape",
            new[] { Steel() }, new[]
            {
                new AnalysisSectionEntryM88A(CatalogMaterialFamilyM88A.Timber,
                    new SectionProfileDefinitionM80("I", "I", "DoublySymmetricI", WidthMm: 55, HeightMm: 100, WebThicknessMm: 4.1, FlangeThicknessMm: 5.7))
            }));
    }

    [Fact]
    public void IncompatibleMaterialFamily_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => Sample().SelectForElasticAnalysis("WOOD-80X160", "S235"));
        Assert.Throws<InvalidOperationException>(() => Sample().SelectForElasticAnalysis("STEEL-I-100-IDEAL", "C24"));
    }

    [Fact]
    public void MissingIdentifiers_AreNeverSilentlySubstituted()
    {
        Assert.Throws<KeyNotFoundException>(() => Sample().SelectForElasticAnalysis("NOT-FOUND", "S235"));
        Assert.Throws<KeyNotFoundException>(() => Sample().SelectForElasticAnalysis("WOOD-80X160", "NO-MATERIAL"));
    }

    [Fact]
    public void Identifiers_AreCaseInsensitiveAndNoDuplicatesAreAllowed()
    {
        var cat = Sample();
        Assert.Equal(cat.FindSection("wood-80x160"), cat.FindSection("WOOD-80X160"));
        Assert.Throws<ArgumentException>(() => new AnalysisCatalogM88A("duplicate material", new[] { Steel(), Steel("s235") }, Array.Empty<AnalysisSectionEntryM88A>()));
        Assert.Throws<InvalidOperationException>(() => new AnalysisCatalogM88A("duplicate section", new[] { Steel() }, new[] { Rect(), Rect("r") }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void InvalidElasticProperties_AreRejected(double modulus)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnalysisCatalogM88A("invalid", new[] { Steel() with { ElasticModulusKnPerM2 = modulus } }, Array.Empty<AnalysisSectionEntryM88A>()));
    }

    [Fact]
    public void InvalidDimensions_AreRejectedByM80RatherThanReimplemented()
    {
        var invalid = Rect() with { Geometry = Rect().Geometry with { HeightMm = -5 } };
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnalysisCatalogM88A("invalid", new[] { Steel() }, new[] { invalid }));
    }

    [Fact]
    public void UntraceableDocumentedMaterial_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new AnalysisCatalogM88A("invalid",
            new[] { Steel() with { DataStatus = "SourceDocumented" } }, Array.Empty<AnalysisSectionEntryM88A>()));
    }

    [Fact]
    public void UnsupportedSchema_CannotBeLoaded()
    {
        var json = Sample().ToJson();
        using var doc = JsonDocument.Parse(json);
        string modified = json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99", StringComparison.Ordinal);
        Assert.NotEqual(json, modified);
        Assert.Throws<NotSupportedException>(() => AnalysisCatalogM88A.FromJson(modified));
    }

    [Fact]
    public void JsonRoundTrip_PreservesProfileGeometryFamilyAndProvenance()
    {
        var catalog = Sample();
        var loaded = AnalysisCatalogM88A.FromJson(catalog.ToJson());
        Assert.Equal(catalog.Sections.Count, loaded.Sections.Count);
        Assert.Equal(catalog.Materials.Count, loaded.Materials.Count);
        Assert.Equal(catalog.FindSection("STEEL-I-100-IDEAL").Geometry.Properties.Area,
                     loaded.FindSection("STEEL-I-100-IDEAL").Geometry.Properties.Area, 14);
        Assert.Equal(catalog.FindMaterial("GL24H"), loaded.FindMaterial("gl24h"));
    }

    [Fact]
    public void UserDefinedEntry_UsesM80ForConstruction_AndCanSelectForAnalysis()
    {
        var catalog = new AnalysisCatalogM88A("custom", new[] { Steel() }, new[]
        {
            new AnalysisSectionEntryM88A(CatalogMaterialFamilyM88A.Steel,
                new SectionProfileDefinitionM80("CUSTOM", "Custom 50x100", "Rectangular", WidthMm: 50, HeightMm: 100))
        });
        var selection = catalog.SelectForElasticAnalysis("CUSTOM", "S235");
        Assert.Equal(0.005, selection.Section.Area, 12);
        Assert.Equal(0.05 * Math.Pow(0.1, 3) / 12, selection.Section.MomentOfInertia, 12);
    }
}
