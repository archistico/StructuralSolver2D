using StructuralSolver2D.Core.Model.Materials;

namespace StructuralSolver2D.Core.Tests.Model.Materials;

public sealed class StructuralMaterialLibraryTests
{
    [Fact]
    public void SteelS235_ShouldCreateElasticSteelPreset()
    {
        var material = StructuralMaterialLibrary.SteelS235();

        Assert.Equal("S235", material.Id);
        Assert.Equal("Steel S235", material.Name);
        Assert.Equal(210_000_000.0, material.ElasticModulus);
        Assert.Equal(78.5, material.UnitWeight);
    }

    [Theory]
    [InlineData("S235")]
    [InlineData("S275")]
    [InlineData("S355")]
    public void SteelPresets_ShouldUseSameElasticModulus(string grade)
    {
        var material = grade switch
        {
            "S235" => StructuralMaterialLibrary.SteelS235(),
            "S275" => StructuralMaterialLibrary.SteelS275(),
            "S355" => StructuralMaterialLibrary.SteelS355(),
            _ => throw new ArgumentOutOfRangeException(nameof(grade))
        };

        Assert.Equal(210_000_000.0, material.ElasticModulus);
        Assert.Equal(78.5, material.UnitWeight);
    }

    [Fact]
    public void TimberC24_ShouldCreateElasticTimberPreset()
    {
        var material = StructuralMaterialLibrary.TimberC24();

        Assert.Equal("C24", material.Id);
        Assert.Equal("Timber C24", material.Name);
        Assert.Equal(11_000_000.0, material.ElasticModulus);
        Assert.Equal(4.2, material.UnitWeight);
    }

    [Fact]
    public void GlulamGL24h_ShouldCreateElasticGlulamPreset()
    {
        var material = StructuralMaterialLibrary.GlulamGL24h();

        Assert.Equal("GL24H", material.Id);
        Assert.Equal("Glulam GL24h", material.Name);
        Assert.Equal(11_500_000.0, material.ElasticModulus);
        Assert.Equal(4.3, material.UnitWeight);
    }

    [Fact]
    public void ConcreteC25_30_ShouldCreateElasticConcretePreset()
    {
        var material = StructuralMaterialLibrary.ConcreteC25_30();

        Assert.Equal("C25_30", material.Id);
        Assert.Equal("Concrete C25/30", material.Name);
        Assert.Equal(31_000_000.0, material.ElasticModulus);
        Assert.Equal(25.0, material.UnitWeight);
    }

    [Fact]
    public void GenericConcrete_ShouldCreateElasticConcretePreset()
    {
        var material = StructuralMaterialLibrary.GenericConcrete();

        Assert.Equal("CONCRETE", material.Id);
        Assert.Equal("Generic concrete", material.Name);
        Assert.Equal(30_000_000.0, material.ElasticModulus);
        Assert.Equal(25.0, material.UnitWeight);
    }

    [Fact]
    public void Presets_ShouldAllowCustomIdentifierAndName()
    {
        var material = StructuralMaterialLibrary.SteelS355("CUSTOM_STEEL", "Custom steel preset");

        Assert.Equal("CUSTOM_STEEL", material.Id);
        Assert.Equal("Custom steel preset", material.Name);
        Assert.Equal(210_000_000.0, material.ElasticModulus);
    }

    [Fact]
    public void Presets_ShouldRejectEmptyIdentifier()
    {
        Assert.Throws<ArgumentException>(() => StructuralMaterialLibrary.SteelS235(" "));
    }

    [Fact]
    public void Presets_ShouldRejectEmptyName()
    {
        Assert.Throws<ArgumentException>(() => StructuralMaterialLibrary.SteelS235(name: " "));
    }
}
