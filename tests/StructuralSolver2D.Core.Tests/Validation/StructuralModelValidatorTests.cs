using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Validation;

public sealed class StructuralModelValidatorTests
{
    [Fact]
    public void Validate_ShouldReturnValid_ForSimpleSupportedBeamModel()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel();
        StructuralModelValidator validator = new();

        StructuralModelValidationResult result = validator.Validate(model);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_ShouldDetectDuplicateNodeIds()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel();
        model.AddNode(new StructuralNode("A", 10.0, 0.0));
        StructuralModelValidator validator = new();

        StructuralModelValidationResult result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "NODE_DUPLICATE_ID");
    }

    [Fact]
    public void Validate_ShouldDetectMissingMemberNode()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel();
        model.Members.Clear();
        model.AddMember(new StructuralMember("M1", "A", "MISSING", "S235", "GENERIC"));
        StructuralModelValidator validator = new();

        StructuralModelValidationResult result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "MEMBER_END_NODE_NOT_FOUND");
    }

    [Fact]
    public void Validate_ShouldDetectZeroLengthMember()
    {
        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 0.0, 0.0))
            .AddMaterial(new StructuralMaterial("S235", "Steel S235", 210_000_000.0))
            .AddSection(new StructuralSection("GENERIC", "Generic beam", 0.003, 0.00002))
            .AddMember(new StructuralMember("M1", "A", "B", "S235", "GENERIC"));

        StructuralModelValidator validator = new();

        StructuralModelValidationResult result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "MEMBER_ZERO_LENGTH");
    }

    [Fact]
    public void Validate_ShouldDetectInvalidMaterialElasticModulus()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel();
        model.Materials.Clear();
        model.AddMaterial(new StructuralMaterial("S235", "Steel S235", 0.0));
        StructuralModelValidator validator = new();

        StructuralModelValidationResult result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "MATERIAL_INVALID_ELASTIC_MODULUS");
    }

    [Fact]
    public void Validate_ShouldDetectInvalidSectionProperties()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel();
        model.Sections.Clear();
        model.AddSection(new StructuralSection("GENERIC", "Generic beam", 0.0, -1.0));
        StructuralModelValidator validator = new();

        StructuralModelValidationResult result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "SECTION_INVALID_AREA");
        Assert.Contains(result.Errors, issue => issue.Code == "SECTION_INVALID_INERTIA");
    }

    [Fact]
    public void Validate_ShouldDetectSupportOnMissingNode()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel();
        model.Supports.Clear();
        model.AddSupport(StructuralSupport.Hinge("SX", "MISSING"));
        StructuralModelValidator validator = new();

        StructuralModelValidationResult result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "SUPPORT_NODE_NOT_FOUND");
    }


    [Fact]
    public void Validate_ShouldDetectInvalidSupportOrientation()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel();
        model.Supports.Clear();
        model.AddSupport(new StructuralSupport("S_BAD", "A", false, true, false, OrientationDegrees: double.NaN));
        StructuralModelValidator validator = new();

        StructuralModelValidationResult result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "SUPPORT_INVALID_ORIENTATION");
    }

    [Fact]
    public void Validate_ShouldWarnForSupportWithoutRestraints()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel();
        model.AddSupport(new StructuralSupport("S_FREE", "A", false, false, false));
        StructuralModelValidator validator = new();

        StructuralModelValidationResult result = validator.Validate(model);

        Assert.True(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "SUPPORT_WITHOUT_RESTRAINTS");
    }

    private static StructuralModel CreateValidSimpleSupportedBeamModel() =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 5.0, 0.0))
            .AddMaterial(new StructuralMaterial("S235", "Steel S235", 210_000_000.0))
            .AddSection(new StructuralSection("GENERIC", "Generic beam", 0.003, 0.00002))
            .AddMember(new StructuralMember("M1", "A", "B", "S235", "GENERIC"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
}
