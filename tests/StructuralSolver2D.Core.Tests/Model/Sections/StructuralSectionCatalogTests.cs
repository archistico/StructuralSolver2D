using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Sections;

namespace StructuralSolver2D.Core.Tests.Model.Sections;

public sealed class StructuralSectionCatalogTests
{
    [Fact]
    public void Add_ShouldStoreSectionsInInsertionOrder()
    {
        var catalog = new StructuralSectionCatalog()
            .Add(StructuralSectionFactory.Rectangular("R1", 0.20, 0.40))
            .Add(StructuralSectionFactory.CircularSolid("C1", 0.10));

        Assert.Equal(2, catalog.Count);
        Assert.Equal("R1", catalog.Sections[0].Id);
        Assert.Equal("C1", catalog.Sections[1].Id);
    }

    [Fact]
    public void Add_WithDuplicateId_ShouldThrowClearException()
    {
        var catalog = new StructuralSectionCatalog()
            .Add(StructuralSectionFactory.Rectangular("R1", 0.20, 0.40));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            catalog.Add(StructuralSectionFactory.Rectangular("r1", 0.30, 0.50)));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddOrReplace_ShouldReplaceExistingSection()
    {
        var catalog = new StructuralSectionCatalog()
            .Add(StructuralSectionFactory.Rectangular("R1", 0.20, 0.40));

        catalog.AddOrReplace(StructuralSectionFactory.Rectangular("r1", 0.30, 0.60));

        StructuralSection section = catalog.Find("R1");
        Assert.Equal(0.18, section.Area, precision: 12);
        Assert.Single(catalog.Sections);
    }

    [Fact]
    public void Find_ShouldMatchIdsCaseInsensitively()
    {
        var catalog = new StructuralSectionCatalog()
            .Add(StructuralSectionFactory.CircularHollow("CHS_100_80", 0.10, 0.08));

        StructuralSection section = catalog.Find("chs_100_80");

        Assert.Equal("CHS_100_80", section.Id);
    }

    [Fact]
    public void ApplyToModel_ShouldAddMissingSectionsAndSkipExistingOnes()
    {
        var model = new StructuralModel()
            .AddSection(StructuralSectionFactory.Rectangular("R1", 0.20, 0.40));
        var catalog = new StructuralSectionCatalog()
            .Add(StructuralSectionFactory.Rectangular("R1", 0.30, 0.60))
            .Add(StructuralSectionFactory.CircularSolid("C1", 0.10));

        int changed = catalog.ApplyToModel(model);

        Assert.Equal(1, changed);
        Assert.Equal(2, model.Sections.Count);
        Assert.Equal(0.08, model.Sections.Single(section => section.Id == "R1").Area, precision: 12);
    }

    [Fact]
    public void ApplyToModel_WithReplaceExisting_ShouldReplaceMatchingSections()
    {
        var model = new StructuralModel()
            .AddSection(StructuralSectionFactory.Rectangular("R1", 0.20, 0.40));
        var catalog = new StructuralSectionCatalog()
            .Add(StructuralSectionFactory.Rectangular("r1", 0.30, 0.60));

        int changed = catalog.ApplyToModel(model, replaceExisting: true);

        Assert.Equal(1, changed);
        Assert.Equal(0.18, model.Sections.Single().Area, precision: 12);
    }

    [Fact]
    public void Add_WithInvalidSection_ShouldThrowClearException()
    {
        var catalog = new StructuralSectionCatalog();
        var invalid = new StructuralSection("BAD", "Bad", 0.0, 1.0e-6);

        Assert.Throws<ArgumentOutOfRangeException>(() => catalog.Add(invalid));
    }
}
