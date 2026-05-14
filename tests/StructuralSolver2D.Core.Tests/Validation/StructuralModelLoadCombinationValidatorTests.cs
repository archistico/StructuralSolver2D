using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Validation;

public sealed class StructuralModelLoadCombinationValidatorTests
{
    [Fact]
    public void Validate_ShouldReturnValid_ForManualLoadCombination()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("G1", "Permanent loads"))
            .AddLoadCase(new StructuralLoadCase("Q1", "Variable loads"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("G1_Q", "G1", "M1", StructuralLoadDirection.GlobalY, -10.0))
            .AddLoad(StructuralLoad.PointLoadOnMember("Q1_P", "Q1", "M1", StructuralLoadDirection.GlobalY, -12.0, 0.5))
            .AddLoadCombination(new StructuralLoadCombination(
                "ULS1",
                "ULS 1",
                new[]
                {
                    new StructuralLoadCombinationTerm("G1", 1.35),
                    new StructuralLoadCombinationTerm("Q1", 1.50),
                }));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ShouldDetectDuplicateLoadCombinationIds()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("G1", "Permanent loads"))
            .AddLoadCombination(new StructuralLoadCombination("ULS1", "ULS 1", new[] { new StructuralLoadCombinationTerm("G1", 1.35) }))
            .AddLoadCombination(new StructuralLoadCombination("uls1", "Duplicate", new[] { new StructuralLoadCombinationTerm("G1", 1.00) }));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_COMBINATION_DUPLICATE_ID");
    }

    [Fact]
    public void Validate_ShouldDetectCombinationReferencingMissingLoadCase()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("G1", "Permanent loads"))
            .AddLoadCombination(new StructuralLoadCombination("ULS1", "ULS 1", new[] { new StructuralLoadCombinationTerm("MISSING", 1.35) }));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_COMBINATION_LOAD_CASE_NOT_FOUND");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_ShouldDetectInvalidCombinationFactor(double factor)
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("G1", "Permanent loads"))
            .AddLoadCombination(new StructuralLoadCombination("ULS1", "ULS 1", new[] { new StructuralLoadCombinationTerm("G1", factor) }));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_COMBINATION_INVALID_FACTOR");
    }

    [Fact]
    public void Validate_ShouldDetectCombinationWithoutTerms()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("G1", "Permanent loads"))
            .AddLoadCombination(new StructuralLoadCombination("ULS1", "ULS 1", Array.Empty<StructuralLoadCombinationTerm>()));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_COMBINATION_WITHOUT_TERMS");
    }

    [Fact]
    public void Validate_ShouldDetectDuplicateTermsInsideCombination()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("G1", "Permanent loads"))
            .AddLoadCombination(new StructuralLoadCombination(
                "ULS1",
                "ULS 1",
                new[]
                {
                    new StructuralLoadCombinationTerm("G1", 1.35),
                    new StructuralLoadCombinationTerm("g1", 1.00),
                }));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_COMBINATION_DUPLICATE_TERM");
    }

    private static StructuralModel CreateValidSimpleSupportedBeamModel() =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 5.0, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic material", 210_000_000.0))
            .AddSection(new StructuralSection("SEC", "Generic section", 0.003, 0.00002))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
}
