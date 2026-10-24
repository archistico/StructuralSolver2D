using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Core.Model.Sections;

namespace StructuralSolver2D.Catalogs.Tests.M88;

public sealed class CommercialSteelCatalogM88BTests
{
    private static CommercialSteelCatalogM88B Sample() =>
        CommercialSteelCatalogM88B.Load(Path.Combine(AppContext.BaseDirectory, "steel-ipe-published-2026.json"));
    private static PublishedIpeSectionM88B Entry() => Sample().Find("IPE-100").Definition;
    private static CommercialSteelCatalogM88B One(PublishedIpeSectionM88B entry) => new("test", new[] { entry });

    [Fact]
    public void PublishedSample_ContainsEightRealIpeEntries_WithTraceableMetadata()
    {
        var cat = Sample();
        Assert.Equal(8, cat.Sections.Count);
        Assert.False(cat.IncludesNormativeVerification);
        Assert.All(cat.Sections, s =>
        {
            Assert.Equal("EN 10365:2026", s.Definition.DimensionStandard);
            Assert.Equal("PublishedThirdParty", s.Definition.DataStatus);
            Assert.StartsWith("https://", s.Definition.PropertySourceUrl);
            Assert.Equal("RolledIPEPublishedTable", s.Properties.Shape);
        });
    }

    [Theory]
    [InlineData("IPE-80", 7.64, 80.14, 8.49, 20.03, 3.69)]
    [InlineData("IPE-100", 10.32, 171.0, 15.92, 34.2, 5.79)]
    [InlineData("IPE-120", 13.21, 317.8, 27.67, 52.96, 8.65)]
    [InlineData("IPE-140", 16.43, 541.2, 44.92, 77.32, 12.31)]
    [InlineData("IPE-160", 20.09, 869.3, 68.31, 108.7, 16.66)]
    [InlineData("IPE-180", 23.95, 1317.0, 100.9, 146.3, 22.16)]
    [InlineData("IPE-200", 28.48, 1943.0, 142.4, 194.3, 28.47)]
    [InlineData("IPE-240", 39.12, 3892.0, 283.6, 324.3, 47.27)]
    public void PublishedProperties_ExactlyMatchTheFrozenReferenceTable(string name, double area, double iy, double iz, double wy, double wz)
    {
        var s = Sample().Find(name);
        Assert.Equal(area * 1e-4, s.Properties.Area, 13);
        Assert.Equal(iy * 1e-8, s.Properties.InPlaneInertia, 13);
        Assert.Equal(iz * 1e-8, s.Properties.OutOfPlaneInertia, 13);
        Assert.Equal(wy * 1e-6, s.Properties.InPlaneElasticSectionModulus, 13);
        Assert.Equal(wz * 1e-6, s.Properties.OutOfPlaneElasticSectionModulus, 13);
    }

    [Fact]
    public void PublishedRolledSection_DiffersFromIdealSharpCornerM80Geometry()
    {
        var s = Sample().Find("IPE-100");
        var ideal = SectionGeometryFactoryM80.DoublySymmetricI("IDEAL", .055, .1, .0041, .0057);
        Assert.True(s.Properties.Area > ideal.Area);
        Assert.NotEqual(ideal.InPlaneInertia, s.Properties.InPlaneInertia);
    }

    [Fact]
    public void SolverSelection_PreservesPublishedAreaStrongAxisInertiaAndMaterialLabelOnly()
    {
        var materials = AnalysisCatalogM88A.Load(Path.Combine(AppContext.BaseDirectory, "analysis-starter.json"));
        var selection = Sample().SelectForElasticAnalysis("ipe-200", materials, "S355");
        Assert.Equal(28.48e-4, selection.Section.Area, 12);
        Assert.Equal(1943e-8, selection.Section.MomentOfInertia, 12);
        Assert.Equal(0.2, selection.Section.Height!.Value);
        Assert.Equal(210_000_000, selection.Material.ElasticModulus);
        Assert.Equal("PublishedThirdParty", selection.GeometryDataStatus);
        Assert.False(selection.IncludesNormativeStrengthOrDesignFactors);
        Assert.Throws<InvalidOperationException>(() => Sample().SelectForElasticAnalysis("IPE-200", materials, "C24"));
    }

    [Fact]
    public void UnknownIdentifiersAndDuplicateNames_FailClosed()
    {
        Assert.Throws<KeyNotFoundException>(() => Sample().Find("HEB-200"));
        Assert.Throws<ArgumentException>(() => new CommercialSteelCatalogM88B("dupe", new[] { Entry(), Entry() with { Id = "ipe-100" } }));
    }

    [Fact]
    public void InvalidGeometryAndAxisSwaps_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => One(Entry() with { AreaCm2 = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => One(Entry() with { StrongInertiaCm4 = double.NaN }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { WeakInertiaCm4 = 1000 }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { StrongElasticModulusCm3 = 5 }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { MassKgPerM = 200 }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { WebThicknessMm = 60 }));
    }

    [Fact]
    public void SourceIsMandatory_NoImplicitNormativeApproval()
    {
        Assert.Throws<ArgumentException>(() => One(Entry() with { PropertySourceUrl = "http://unverified.invalid" }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { SourceCheckedOn = "" }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { DimensionStandard = "EN 10365" }));
        Assert.Throws<ArgumentException>(() => One(Entry() with { DataStatus = "ManufacturerCertified" }));
        Assert.Throws<NotSupportedException>(() => One(Entry() with { Family = "HEB" }));
    }

    [Fact]
    public void SerializationPreservesAllPublishedValuesAndMetadata()
    {
        var source = Sample();
        var copy = CommercialSteelCatalogM88B.FromJson(source.ToJson());
        Assert.Equal(source.Sections.Count, copy.Sections.Count);
        Assert.Equal(source.Find("IPE-240").Definition, copy.Find("ipe-240").Definition);
        Assert.Equal(source.Find("IPE-240").Properties, copy.Find("ipe-240").Properties);
    }

    [Fact]
    public void UnsupportedSchemaAndAbsentSections_AreRejected()
    {
        var json = Sample().ToJson();
        Assert.Throws<NotSupportedException>(() => CommercialSteelCatalogM88B.FromJson(json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2")));
        Assert.Throws<InvalidOperationException>(() => CommercialSteelCatalogM88B.FromJson("{\"schemaVersion\":1,\"title\":\"empty\"}"));
    }

    [Fact]
    public void OnlyPublishedStrongAxisDataAreUsedByDefault()
    {
        var p = Sample().Find("IPE-240");
        Assert.True(p.Properties.InPlaneInertia > p.Properties.OutOfPlaneInertia);
        Assert.Equal(p.Properties.InPlaneInertia, p.ToSolverSection().MomentOfInertia);
        Assert.Equal(0.240, p.ToSolverSection().Height!.Value, 10);
    }
}
