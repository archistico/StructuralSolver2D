using StructuralSolver2D.Core.Model.Sections;

namespace StructuralSolver2D.Core.Tests.Model.Sections;

public sealed class SectionGeometryM80Tests
{
    [Fact]
    public void Rectangular_KnownClosedForm_AndSolverProjection()
    {
        SectionGeometricPropertiesM80 p = SectionGeometryFactoryM80.Rectangular("RECT", 0.10, 0.20);
        Assert.Equal(0.02, p.Area, 12);
        Assert.Equal(0.10 * Math.Pow(0.20, 3) / 12.0, p.InPlaneInertia, 15);
        Assert.Equal(0.20 * Math.Pow(0.10, 3) / 12.0, p.OutOfPlaneInertia, 15);
        Assert.Equal(0.10 * Math.Pow(0.20, 2) / 6.0, p.InPlaneElasticSectionModulus, 15);
        Assert.Equal(0.20 * Math.Pow(0.10, 2) / 6.0, p.OutOfPlaneElasticSectionModulus, 15);
        Assert.Equal(0.20 / Math.Sqrt(12.0), p.InPlaneRadiusOfGyration, 14);
        Assert.Equal(0.10 / Math.Sqrt(12.0), p.OutOfPlaneRadiusOfGyration, 14);
        Assert.Same(p.Section, p.ToSolverSection());
    }

    [Fact]
    public void RectangularHollow_MatchesIndependentOuterMinusInner_ForPracticalWall()
    {
        const double b = 0.120, h = 0.200, t = 0.006;
        SectionGeometricPropertiesM80 p = SectionGeometryFactoryM80.RectangularHollow("RHS", b, h, t);
        double bi = b - 2.0 * t, hi = h - 2.0 * t;
        Assert.Equal(b * h - bi * hi, p.Area, 13);
        Assert.Equal((b * Math.Pow(h, 3) - bi * Math.Pow(hi, 3)) / 12.0, p.InPlaneInertia, 14);
        Assert.Equal((h * Math.Pow(b, 3) - hi * Math.Pow(bi, 3)) / 12.0, p.OutOfPlaneInertia, 14);
        Assert.Equal(p.InPlaneInertia / (h / 2.0), p.InPlaneElasticSectionModulus, 14);
    }

    [Fact]
    public void ThinWalledRhs_DoesNotCancelSmallButRepresentableWall()
    {
        const double b = 0.120, h = 0.200, t = 1e-9;
        SectionGeometricPropertiesM80 p = SectionGeometryFactoryM80.RectangularHollow("RHS_THIN", b, h, t);
        double approximatePerimeterArea = 2.0 * t * (b + h);
        Assert.InRange(p.Area / approximatePerimeterArea, 0.999999, 1.000001);
        Assert.True(double.IsFinite(p.InPlaneInertia) && p.InPlaneInertia > 0);
        Assert.True(double.IsFinite(p.OutOfPlaneInertia) && p.OutOfPlaneInertia > 0);
    }

    [Fact]
    public void DoublySymmetricI_MatchesIndependentCompositeRectangles()
    {
        const double b = 0.100, h = 0.200, tw = 0.006, tf = 0.010;
        SectionGeometricPropertiesM80 p = SectionGeometryFactoryM80.DoublySymmetricI("I_DEMO", b, h, tw, tf);
        double hc = h - 2.0 * tf;
        double a = 2.0 * b * tf + tw * hc;
        double iz = (b * Math.Pow(h, 3) - (b - tw) * Math.Pow(hc, 3)) / 12.0;
        double iy = (2.0 * tf * Math.Pow(b, 3) + hc * Math.Pow(tw, 3)) / 12.0;
        Assert.Equal(a, p.Area, 13);
        Assert.Equal(iz, p.InPlaneInertia, 14);
        Assert.Equal(iy, p.OutOfPlaneInertia, 14);
        Assert.True(p.InPlaneInertia > p.OutOfPlaneInertia);
    }

    [Fact]
    public void CircularSections_HaveEqualTwoAxisInertias_AndCorrectSectionModuli()
    {
        SectionGeometricPropertiesM80 solid = SectionGeometryFactoryM80.CircularSolid("C", 0.1);
        SectionGeometricPropertiesM80 hollow = SectionGeometryFactoryM80.CircularHollow("CHS", 0.1, 0.08);
        Assert.Equal(solid.InPlaneInertia, solid.OutOfPlaneInertia, 15);
        Assert.Equal(hollow.InPlaneInertia, hollow.OutOfPlaneInertia, 15);
        Assert.Equal(2.0 * hollow.InPlaneInertia / 0.1, hollow.InPlaneElasticSectionModulus, 14);
        Assert.Equal(hollow.InPlaneElasticSectionModulus, hollow.OutOfPlaneElasticSectionModulus, 14);
    }

    [Fact]
    public void RotatingRectangularDimensions_ExchangesPrincipalValuesAndPreservesArea()
    {
        SectionGeometricPropertiesM80 a = SectionGeometryFactoryM80.Rectangular("A", 0.08, 0.24);
        SectionGeometricPropertiesM80 b = SectionGeometryFactoryM80.Rectangular("B", 0.24, 0.08);
        Assert.Equal(a.Area, b.Area, 14);
        Assert.Equal(a.InPlaneInertia, b.OutOfPlaneInertia, 14);
        Assert.Equal(a.OutOfPlaneInertia, b.InPlaneInertia, 14);
        Assert.Equal(a.InPlaneElasticSectionModulus, b.OutOfPlaneElasticSectionModulus, 14);
    }

    [Theory]
    [InlineData(0.10, 0.20, 0.05)]
    [InlineData(0.10, 0.20, 0.10)]
    [InlineData(0.10, 0.20, double.NaN)]
    public void RectangularHollow_RejectsImpossibleOrNonFiniteWalls(double width, double height, double thickness)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SectionGeometryFactoryM80.RectangularHollow("INVALID", width, height, thickness));
    }

    [Theory]
    [InlineData(0.10, 0.20, 0.11, 0.01)]
    [InlineData(0.10, 0.20, 0.006, 0.10)]
    [InlineData(0.10, 0.20, -0.006, 0.01)]
    public void IdealizedI_RejectsImpossibleFlangesAndWeb(double width, double height, double web, double flange)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SectionGeometryFactoryM80.DoublySymmetricI("INVALID", width, height, web, flange));
    }

    [Fact]
    public void Catalog_ParsesMillimeterDimensions_RoundTrips_AndExportsLegacyModelSection()
    {
        var input = new SectionProfileCatalogM80(new[]
        {
            new SectionProfileDefinitionM80("wood80", "Wood 80x240 demo", "TimberRectangular", WidthMm: 80, HeightMm: 240),
            new SectionProfileDefinitionM80("i100", "Idealized I", "DoublySymmetricI", WidthMm: 100,
                HeightMm: 200, WebThicknessMm: 6, FlangeThicknessMm: 10),
        }, "M80 sample");

        SectionProfileCatalogM80 decoded = SectionProfileCatalogM80.FromJson(input.ToJson());
        Assert.Equal("M80 sample", decoded.Title);
        Assert.Equal(2, decoded.Profiles.Count);
        Assert.Equal(0.08 * 0.24, decoded.Find("WOOD80").Properties.Area, 12);
        Assert.Equal(0.08 * Math.Pow(0.24, 3) / 12, decoded.Find("wood80").Properties.InPlaneInertia, 14);
        Assert.Equal("Illustrative", decoded.Find("i100").Definition.DataStatus);
        var legacy = decoded.ToSolverCatalog();
        Assert.Equal(2, legacy.Count);
        Assert.Equal(decoded.Find("i100").Properties.InPlaneInertia, legacy.Find("i100").MomentOfInertia, 14);
        string legacyJson = StructuralSectionCatalogJsonSerializer.Serialize(legacy);
        Assert.Equal(2, StructuralSectionCatalogJsonSerializer.Deserialize(legacyJson).Count);
    }

    [Fact]
    public void Catalog_RejectsUnjustifiedProvenance_AndUnknownShape()
    {
        Assert.Throws<ArgumentException>(() => new SectionProfileCatalogM80(new[]
        {
            new SectionProfileDefinitionM80("IPE?", "Not verified", "DoublySymmetricI", WidthMm: 100,
                HeightMm: 200, WebThicknessMm: 6, FlangeThicknessMm: 10, DataStatus: "SourceDocumented")
        }));

        Assert.Throws<NotSupportedException>(() => new SectionProfileCatalogM80(new[]
        {
            new SectionProfileDefinitionM80("X", "Invalid shape", "UnknownShape", WidthMm: 10, HeightMm: 20)
        }));
    }

    [Fact]
    public void Catalog_SourceDocumentedRecordsReferenceButDoesNotClaimValidation()
    {
        var catalog = new SectionProfileCatalogM80(new[]
        {
            new SectionProfileDefinitionM80("DEMO", "Source-referenced geometry", "Rectangular",
                WidthMm: 100, HeightMm: 200, DataStatus: "SourceDocumented",
                SourceDocument: "Example supplier data sheet", SourceEdition: "2026-A")
        });
        ResolvedSectionProfileM80 profile = SectionProfileCatalogM80.FromJson(catalog.ToJson()).Find("demo");
        Assert.Equal("SourceDocumented", profile.Definition.DataStatus);
        Assert.Equal("Example supplier data sheet", profile.Definition.SourceDocument);
        Assert.Equal("2026-A", profile.Definition.SourceEdition);
        Assert.Equal(0.02, profile.Properties.Area, 12);
    }

    [Fact]
    public void Catalog_RejectsDuplicateCaseInsensitiveIds_AndMissingRequiredDimensions()
    {
        Assert.Throws<InvalidOperationException>(() => new SectionProfileCatalogM80(new[]
        {
            new SectionProfileDefinitionM80("wood", "W1", "TimberRectangular", WidthMm: 80, HeightMm: 240),
            new SectionProfileDefinitionM80("WOOD", "W2", "TimberRectangular", WidthMm: 80, HeightMm: 240)
        }));
        Assert.Throws<ArgumentException>(() => new SectionProfileCatalogM80(new[]
        {
            new SectionProfileDefinitionM80("wood", "W1", "TimberRectangular", WidthMm: 80)
        }));
    }

    [Fact]
    public void Catalog_RejectsMalformedDocuments()
    {
        Assert.Throws<NotSupportedException>(() => SectionProfileCatalogM80.FromJson("""
        {"schemaVersion": 2, "title": "x", "profiles": []}
        """));
        Assert.Throws<InvalidOperationException>(() => SectionProfileCatalogM80.FromJson("""
        {"schemaVersion": 1, "title": "x"}
        """));
    }
}
