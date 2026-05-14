using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Sections;

namespace StructuralSolver2D.Core.Tests.Model.Sections;

public sealed class StructuralSectionCatalogJsonSerializerTests
{
    [Fact]
    public void Serialize_ShouldWriteReadableCatalogJson()
    {
        var catalog = new StructuralSectionCatalog()
            .Add(StructuralSectionFactory.Rectangular("RECT_200x400", 0.20, 0.40));

        string json = StructuralSectionCatalogJsonSerializer.Serialize(catalog, "Example sections");

        Assert.Contains("\"schemaVersion\": 1", json, StringComparison.Ordinal);
        Assert.Contains("\"title\": \"Example sections\"", json, StringComparison.Ordinal);
        Assert.Contains("\"id\": \"RECT_200x400\"", json, StringComparison.Ordinal);
        Assert.Contains("\"area\":", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_ShouldRoundTripSections()
    {
        var original = new StructuralSectionCatalog()
            .Add(StructuralSectionFactory.Rectangular("RECT_200x400", 0.20, 0.40))
            .Add(StructuralSectionFactory.CircularHollow("CHS_100_80", 0.10, 0.08));

        string json = StructuralSectionCatalogJsonSerializer.Serialize(original);
        StructuralSectionCatalog loaded = StructuralSectionCatalogJsonSerializer.Deserialize(json);

        Assert.Equal(2, loaded.Count);
        Assert.Equal(original.Find("RECT_200x400"), loaded.Find("RECT_200x400"));
        Assert.Equal(original.Find("CHS_100_80"), loaded.Find("chs_100_80"));
    }

    [Fact]
    public void SaveAndLoad_ShouldPersistCatalogFile()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "sections.json");
        var catalog = new StructuralSectionCatalog()
            .Add(new StructuralSection("GEN", "Generic section", 0.012, 4.5e-6, Height: 0.30, Width: 0.20));

        StructuralSectionCatalogJsonSerializer.Save(catalog, path, "Temporary catalog");
        StructuralSectionCatalog loaded = StructuralSectionCatalogJsonSerializer.Load(path);

        Assert.True(File.Exists(path));
        Assert.Equal(catalog.Find("GEN"), loaded.Find("GEN"));
    }

    [Fact]
    public void Deserialize_WithUnsupportedSchemaVersion_ShouldThrowClearException()
    {
        string json = """
        {
          "schemaVersion": 99,
          "title": "Future catalog",
          "sections": []
        }
        """;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            StructuralSectionCatalogJsonSerializer.Deserialize(json));

        Assert.Contains("Unsupported section catalog schema version", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_WithDuplicateSectionIds_ShouldThrowClearException()
    {
        string json = """
        {
          "schemaVersion": 1,
          "title": "Duplicate catalog",
          "sections": [
            { "id": "S1", "name": "Section 1", "area": 0.01, "momentOfInertia": 0.000001 },
            { "id": "s1", "name": "Section 1 duplicate", "area": 0.02, "momentOfInertia": 0.000002 }
          ]
        }
        """;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            StructuralSectionCatalogJsonSerializer.Deserialize(json));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
