using StructuralSolver2D.Catalogs;

namespace StructuralSolver2D.Catalogs.Tests.M88;

public sealed class CharacteristicMaterialCatalogM88CTests
{
    private static CharacteristicMaterialCatalogM88C Sample() =>
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory,
            "characteristic-strength-classes.json"));

    private static CharacteristicMaterialCatalogM88C OneSteel(SteelStrengthClassM88C entry) =>
        new("test", new[] { entry }, Array.Empty<TimberStrengthClassM88C>());

    private static CharacteristicMaterialCatalogM88C OneTimber(TimberStrengthClassM88C entry) =>
        new("test", Array.Empty<SteelStrengthClassM88C>(), new[] { entry });

    [Fact]
    public void MaterialClasses_AreVersionedReferencedAndNotDesignApproved()
    {
        var catalog = Sample();
        Assert.Equal(3, catalog.Steel.Count);
        Assert.Equal(5, catalog.Timber.Count);
        Assert.False(catalog.IncludesDesignStrengthsOrPartialFactors);
        Assert.False(catalog.ConfersCodeCompliance);
        Assert.All(catalog.Steel, grade =>
        {
            Assert.Equal("EN 10025-2:2019", grade.ReferenceStandard);
            Assert.Equal("ReferenceNotCertified", grade.DataStatus);
            Assert.StartsWith("https://", grade.SourceUrl);
        });
        Assert.All(catalog.Timber, grade =>
        {
            Assert.Equal("ReferenceNotCertified", grade.DataStatus);
            Assert.StartsWith("https://", grade.SourceUrl);
        });
    }

    [Theory]
    [InlineData("S235", 235, 225, 215, 215)]
    [InlineData("S275", 275, 265, 255, 245)]
    [InlineData("S355", 355, 345, 335, 325)]
    public void Steel_StrengthBands_AreReferenceTableValues(
        string grade, double first, double second, double third, double fourth)
    {
        var catalog = Sample();
        Assert.Equal(first, catalog.GetSteelYieldMpa(grade, 16));
        Assert.Equal(second, catalog.GetSteelYieldMpa(grade, 40));
        Assert.Equal(third, catalog.GetSteelYieldMpa(grade, 63));
        Assert.Equal(fourth, catalog.GetSteelYieldMpa(grade, 80));
    }

    [Fact]
    public void SteelThicknessBoundary_DoesNotUseGradeNumberForThickFlanges()
    {
        var catalog = Sample();
        Assert.Equal(355, catalog.GetSteelYieldMpa("S355", 16.0));
        Assert.Equal(345, catalog.GetSteelYieldMpa("S355", 16.001));
        Assert.Equal(335, catalog.GetSteelYieldMpa("S355", 40.001));
        Assert.Equal(325, catalog.GetSteelYieldMpa("S355", 63.001));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidNominalThickness_IsRejected(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample().GetSteelYieldMpa("S235", value));

    [Fact]
    public void UnsupportedSteelThickness_IsNeverExtrapolated()
    {
        Assert.Throws<NotSupportedException>(() => Sample().GetSteelYieldMpa("S355", 80.01));
        Assert.Throws<KeyNotFoundException>(() => Sample().FindSteel("S420"));
    }

    [Theory]
    [InlineData("C16", 16, 8.5, 17, 3.2, 8000, 310)]
    [InlineData("C24", 24, 14.5, 21, 4, 11000, 350)]
    [InlineData("C30", 30, 19, 24, 4, 12000, 380)]
    [InlineData("GL24h", 24, 19.2, 24, 3.5, 11500, 385)]
    [InlineData("GL28h", 28, 22.3, 28, 3.5, 12600, 425)]
    public void Timber_CharacteristicProperties_AgreeWithDeclaredReferences(
        string id, double bending, double tension, double compression,
        double shear, double eMean, double rhoK)
    {
        var item = Sample().FindTimber(id);
        Assert.Equal(bending, item.BendingStrengthMpa);
        Assert.Equal(tension, item.TensionParallelMpa);
        Assert.Equal(compression, item.CompressionParallelMpa);
        Assert.Equal(shear, item.ShearStrengthMpa);
        Assert.Equal(eMean, item.ElasticModulusParallelMeanMpa);
        Assert.Equal(rhoK, item.DensityCharacteristicKgM3);
        Assert.Equal(id.EndsWith('h') ? StrengthMaterialKindM88C.HomogeneousGlulam :
            StrengthMaterialKindM88C.SolidSoftwood, item.Kind);
    }

    [Fact]
    public void Timber_MeanStiffnessIsNotFifthPercentile()
    {
        var c24 = Sample().FindTimber("C24");
        Assert.Equal(11000, c24.ElasticModulusParallelMeanMpa);
        Assert.Equal(7400, c24.ElasticModulusParallel05Mpa);
        Assert.Equal(690, c24.ShearModulusMeanMpa);
        Assert.Equal(420, c24.DensityMeanKgM3);
        var gl = Sample().FindTimber("GL24h");
        Assert.Equal(9600, gl.ElasticModulusParallel05Mpa);
        Assert.Equal(650, gl.ShearModulusMeanMpa);
    }

    [Fact]
    public void ExistingElasticPresetAndCharacteristicStrengths_AreDifferentCatalogs()
    {
        var elastic = AnalysisCatalogM88A.Load(Path.Combine(AppContext.BaseDirectory, "analysis-starter.json"));
        var c24 = Sample().FindTimber("C24");
        Assert.Equal(c24.ElasticModulusParallelMeanMpa * 1000,
            elastic.FindMaterial("C24").ElasticModulusKnPerM2);
        Assert.False(elastic.IncludesNormativeMaterialStrengths);
        Assert.False(Sample().ConfersCodeCompliance);
    }

    [Fact]
    public void JsonRoundTrip_PreservesAllEntriesAndBands()
    {
        var original = Sample();
        var restored = CharacteristicMaterialCatalogM88C.FromJson(original.ToJson());
        Assert.Equal(original.Steel.Count, restored.Steel.Count);
        Assert.Equal(original.Timber.Count, restored.Timber.Count);
        Assert.Equal(original.GetSteelYieldMpa("S275", 30), restored.GetSteelYieldMpa("S275", 30));
        Assert.Equal(original.FindTimber("GL28h"), restored.FindTimber("GL28h"));
        Assert.Equal(original.FindSteel("S235").SourceUrl, restored.FindSteel("S235").SourceUrl);
    }

    [Fact]
    public void DuplicateIdentifiers_AcrossMaterialFamiliesAreRejected()
    {
        var s = Sample().FindSteel("S235");
        var w = Sample().FindTimber("C24");
        Assert.Throws<ArgumentException>(() => new CharacteristicMaterialCatalogM88C(
            "duplicates", new[] { s, s with { Id = "s235" } }, Array.Empty<TimberStrengthClassM88C>()));
        Assert.Throws<ArgumentException>(() => new CharacteristicMaterialCatalogM88C(
            "duplicates", new[] { s with { Id = "C24" } }, new[] { w }));
    }

    [Fact]
    public void OverlappingOrGappedThicknessRangesAreRejected()
    {
        var s = Sample().FindSteel("S355");
        var bands = s.YieldByNominalThickness.ToArray();
        bands[1] = bands[1] with { LowerExclusiveMm = 15 };
        Assert.Throws<ArgumentException>(() => OneSteel(s with { YieldByNominalThickness = bands }));
        bands[1] = bands[1] with { LowerExclusiveMm = 17 };
        Assert.Throws<ArgumentException>(() => OneSteel(s with { YieldByNominalThickness = bands }));
    }

    [Fact]
    public void InvalidAndNonFiniteStrengthOrMissingProvenanceFailsClosed()
    {
        var s = Sample().FindSteel("S355");
        var bands = s.YieldByNominalThickness.ToArray();
        bands[0] = bands[0] with { YieldStrengthMpa = double.NaN };
        Assert.Throws<ArgumentOutOfRangeException>(() => OneSteel(s with { YieldByNominalThickness = bands }));
        Assert.Throws<ArgumentException>(() => OneSteel(s with { SourceUrl = "" }));
        Assert.Throws<ArgumentException>(() => OneSteel(s with { DataStatus = "Certified" }));
        Assert.Throws<ArgumentException>(() => OneSteel(s with { ReferenceStandard = "EN 10025-2:2026" }));
        var t = Sample().FindTimber("GL24h");
        Assert.Throws<ArgumentOutOfRangeException>(() => OneTimber(t with { BendingStrengthMpa = double.NaN }));
        Assert.Throws<ArgumentException>(() => OneTimber(t with { DensityCharacteristicKgM3 = 999 }));
        Assert.Throws<ArgumentException>(() => OneTimber(t with { Kind = StrengthMaterialKindM88C.SolidSoftwood }));
        Assert.Throws<ArgumentException>(() => OneTimber(t with { DataStatus = "Verified" }));
    }

    [Fact]
    public void UnknownEditionAndMissingArraysCannotBeAccepted()
    {
        var json = Sample().ToJson();
        Assert.Throws<NotSupportedException>(() => CharacteristicMaterialCatalogM88C.FromJson(
            json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 9")));
        Assert.Throws<InvalidOperationException>(() => CharacteristicMaterialCatalogM88C.FromJson(
            "{\"schemaVersion\":1,\"title\":\"missing\"}"));
        Assert.Throws<KeyNotFoundException>(() => Sample().FindTimber("GL32c"));
        Assert.Throws<KeyNotFoundException>(() => Sample().FindSteel("C24"));
    }
}
