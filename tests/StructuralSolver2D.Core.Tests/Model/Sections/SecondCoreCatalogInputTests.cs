using StructuralSolver2D.Core.Model.Sections;

namespace StructuralSolver2D.Core.Tests.Model.Sections;

public sealed class SecondCoreCatalogInputTests
{
    [Theory]
    [InlineData("")]
    [InlineData(",\"momentOfInteria\":0.00002")]
    [InlineData(",\"momentOfInertia\":null")]
    public void Read_ShouldRejectUnspecifiedInertia(string field)
    {
        string json = "{\"schemaVersion\":1,\"sections\":[{\"id\":\"S\",\"name\":\"Section\",\"area\":0.01" + field + "}]}";
        var exception = Assert.Throws<InvalidOperationException>(() => StructuralSectionCatalogJsonSerializer.Deserialize(json));
        Assert.Contains("sections[0].momentOfInertia", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.00002)]
    public void Read_ShouldRoundTripExplicitInertia(double inertia)
    {
        string value = inertia.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        string json = "{\"schemaVersion\":1,\"sections\":[{\"id\":\"S\",\"name\":\"Section\",\"area\":0.01,\"MoMeNtOfInErTiA\":" + value + "}]}";
        var catalog = StructuralSectionCatalogJsonSerializer.Deserialize(json);
        var restored = StructuralSectionCatalogJsonSerializer.Deserialize(StructuralSectionCatalogJsonSerializer.Serialize(catalog));
        Assert.Equal(inertia, restored.Find("S").MomentOfInertia);
    }
}
