using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Model.Generators;
using StructuralSolver2D.Core.Model.Sections;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Validation;

public sealed class CoreModelRegressionTests
{
    [Theory]
    [InlineData("LC1")]
    [InlineData("lc1")]
    public void Validate_ShouldRejectCaseAndCombinationSharingId(string combinationId)
    {
        var model = ParametricBeamGenerator.SimplySupportedBeam(5, 1)
            .AddLoadCombination(new StructuralLoadCombination(combinationId, "Combination",
                new[] { new StructuralLoadCombinationTerm("LC1", 2) }));

        var result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_COMBINATION_ID_CONFLICT" && issue.EntityId == combinationId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void Validate_ShouldRejectUndefinedMemberType(int type)
    {
        var model = ParametricBeamGenerator.SimplySupportedBeam(5, 1);
        model.Members[0] = model.Members[0] with { Type = (MemberType)type };

        var result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "MEMBER_INVALID_TYPE" && issue.EntityId == "M1");
    }

    [Theory]
    [InlineData(null, "N1", "MEMBER_START_NODE_NOT_FOUND")]
    [InlineData("N0", null, "MEMBER_END_NODE_NOT_FOUND")]
    [InlineData(null, null, "MEMBER_START_NODE_NOT_FOUND")]
    public void Validate_ShouldReportNullNodeReferenceWithoutThrowing(string? start, string? end, string code)
    {
        var model = ParametricBeamGenerator.SimplySupportedBeam(5, 1);
        model.Members[0] = model.Members[0] with { StartNodeId = start!, EndNodeId = end! };

        var result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == code);
    }

    [Fact]
    public void Catalog_ShouldRoundTripZeroInertiaAndValidateItsUseByMemberType()
    {
        const string json = """
            { "schemaVersion": 1, "title": "Truss sections", "sections": [
                { "id": "SEC1", "name": "Axial section", "area": 0.01, "momentOfInertia": 0 }
            ] }
            """;
        var catalog = StructuralSectionCatalogJsonSerializer.Deserialize(json);
        var restored = StructuralSectionCatalogJsonSerializer.Deserialize(StructuralSectionCatalogJsonSerializer.Serialize(catalog));
        Assert.Equal(0.0, restored.Find("SEC1").MomentOfInertia);

        var model = ParametricTrussGenerator.IsostaticTriangularTruss(6, 2);
        Assert.Equal(1, restored.ApplyToModel(model, replaceExisting: true));
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);

        model.Members[0] = model.Members[0] with { Type = MemberType.Frame2D };
        var result = new StructuralModelValidator().Validate(model);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "SECTION_INVALID_INERTIA");
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void CatalogAndValidator_ShouldRejectInvalidTrussInertia(double inertia)
    {
        var model = ParametricTrussGenerator.IsostaticTriangularTruss(6, 2);
        var section = model.Sections[0] with { MomentOfInertia = inertia };
        model.Sections[0] = section;

        Assert.Throws<ArgumentOutOfRangeException>(() => new StructuralSectionCatalog().Add(section));
        var result = new StructuralModelValidator().Validate(model);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "SECTION_INVALID_INERTIA");
    }
}
