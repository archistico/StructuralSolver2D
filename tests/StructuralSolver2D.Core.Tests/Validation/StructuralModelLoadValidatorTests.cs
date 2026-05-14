using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Validation;

public sealed class StructuralModelLoadValidatorTests
{
    [Fact]
    public void Validate_ShouldReturnValid_ForModelWithSupportedLoads()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("LC1", "Permanent loads"))
            .AddLoad(StructuralLoad.NodalForce("FX_A", "LC1", "A", StructuralLoadDirection.GlobalX, 5.0))
            .AddLoad(StructuralLoad.NodalMoment("MZ_A", "LC1", "A", 3.0))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q_M1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10.0))
            .AddLoad(StructuralLoad.PointLoadOnMember("P_M1", "LC1", "M1", StructuralLoadDirection.LocalY, -15.0, 0.5));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_ShouldDetectDuplicateLoadCaseIds()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("LC1", "Permanent loads"))
            .AddLoadCase(new StructuralLoadCase("lc1", "Duplicate permanent loads"));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_CASE_DUPLICATE_ID");
    }

    [Fact]
    public void Validate_ShouldDetectDuplicateLoadIds()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("LC1", "Permanent loads"))
            .AddLoad(StructuralLoad.NodalForce("L1", "LC1", "A", StructuralLoadDirection.GlobalY, -10.0))
            .AddLoad(StructuralLoad.NodalForce("l1", "LC1", "B", StructuralLoadDirection.GlobalY, -5.0));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_DUPLICATE_ID");
    }

    [Fact]
    public void Validate_ShouldDetectMissingLoadCase()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoad(StructuralLoad.NodalForce("L1", "MISSING", "A", StructuralLoadDirection.GlobalY, -10.0));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_CASE_NOT_FOUND");
    }

    [Fact]
    public void Validate_ShouldDetectNodalLoadOnMissingNode()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("LC1", "Permanent loads"))
            .AddLoad(StructuralLoad.NodalForce("L1", "LC1", "MISSING", StructuralLoadDirection.GlobalY, -10.0));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_TARGET_NODE_NOT_FOUND");
    }

    [Fact]
    public void Validate_ShouldDetectMemberLoadOnMissingMember()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("LC1", "Permanent loads"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "LC1", "MISSING", StructuralLoadDirection.GlobalY, -10.0));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_TARGET_MEMBER_NOT_FOUND");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_ShouldDetectInvalidLoadValue(double value)
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("LC1", "Permanent loads"))
            .AddLoad(StructuralLoad.NodalForce("L1", "LC1", "A", StructuralLoadDirection.GlobalY, value));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_INVALID_VALUE");
    }

    [Fact]
    public void Validate_ShouldDetectInvalidDirectionForNodalForce()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("LC1", "Permanent loads"))
            .AddLoad(StructuralLoad.NodalForce("L1", "LC1", "A", StructuralLoadDirection.MomentZ, -10.0));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_INVALID_DIRECTION_FOR_TYPE");
    }

    [Fact]
    public void Validate_ShouldDetectInvalidDirectionForNodalMoment()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("LC1", "Permanent loads"))
            .AddLoad(new StructuralLoad(
                "M1",
                "LC1",
                StructuralLoadType.NodalMoment,
                StructuralLoadTargetType.Node,
                "A",
                StructuralLoadDirection.GlobalY,
                5.0));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_INVALID_DIRECTION_FOR_TYPE");
    }

    [Fact]
    public void Validate_ShouldDetectPointLoadPositionOutOfRange()
    {
        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("LC1", "Permanent loads"))
            .AddLoad(StructuralLoad.PointLoadOnMember("P1", "LC1", "M1", StructuralLoadDirection.GlobalY, -10.0, 1.5));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_POSITION_OUT_OF_RANGE");
    }

    [Fact]
    public void Validate_ShouldDetectMissingPointLoadPosition()
    {
        StructuralLoad load = new(
            "P1",
            "LC1",
            StructuralLoadType.PointLoadOnMember,
            StructuralLoadTargetType.Member,
            "M1",
            StructuralLoadDirection.GlobalY,
            -10.0);

        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("LC1", "Permanent loads"))
            .AddLoad(load);

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_POSITION_REQUIRED");
    }

    [Fact]
    public void Validate_ShouldDetectPositionWhereNotAllowed()
    {
        StructuralLoad load = new(
            "Q1",
            "LC1",
            StructuralLoadType.UniformDistributedLoad,
            StructuralLoadTargetType.Member,
            "M1",
            StructuralLoadDirection.GlobalY,
            -10.0,
            0.5);

        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("LC1", "Permanent loads"))
            .AddLoad(load);

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_POSITION_NOT_ALLOWED");
    }

    [Fact]
    public void Validate_ShouldDetectWrongTargetTypeForMemberLoad()
    {
        StructuralLoad load = new(
            "Q1",
            "LC1",
            StructuralLoadType.UniformDistributedLoad,
            StructuralLoadTargetType.Node,
            "A",
            StructuralLoadDirection.GlobalY,
            -10.0);

        StructuralModel model = CreateValidSimpleSupportedBeamModel()
            .AddLoadCase(new StructuralLoadCase("LC1", "Permanent loads"))
            .AddLoad(load);

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, issue => issue.Code == "LOAD_INVALID_TARGET_TYPE_FOR_TYPE");
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
