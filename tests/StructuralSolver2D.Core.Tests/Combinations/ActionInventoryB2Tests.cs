using System.Text.Json;
using StructuralSolver2D.Combinations.Actions;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Combinations;

public sealed class ActionInventoryB2Tests
{
    [Fact]
    public void ValidCharacteristicInventory_WithTwoOppositeWindPatterns_IsAccepted()
    {
        StructuralModel model = Model("LC_G1", "LC_G2", "LC_Q", "LC_WP", "LC_WM");
        ActionInventory inventory = Inventory(
            G1(), G2(), Residential(),
            Variable("W+", "LC_WP", VariableActionCategory.Wind, "WIND"),
            Variable("W-", "LC_WM", VariableActionCategory.Wind, "WIND"));

        ActionInventoryValidationResult result = ActionInventoryValidator.Validate(inventory, model);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
        Assert.Equal(ActionInventoryStatus.Valid, result.Status);
        Assert.Empty(model.LoadCombinations); // Inventory never mutates the FEM.
    }

    [Fact]
    public void JsonRoundTrip_PreservesExplicitCategoryAndEvidenceWithoutChangingFEM()
    {
        ActionInventory original = Inventory(
            G1() with { TimberDuration = TimberActionDuration.Permanent,
                        TimberDurationEvidenceId = "TIMBER-DURATION-01" },
            G2() with { TreatG2AsG1ByEvidence = true,
                        G2CoefficientExceptionEvidenceId = "ENGINEER-APPROVED-G2-MAGNITUDE" },
            Residential());
        StructuralModel model = Model("LC_G1", "LC_G2", "LC_Q");

        string json = ActionInventoryJsonSerializer.Serialize(original);
        ActionInventory copy = ActionInventoryJsonSerializer.Deserialize(json);

        Assert.Contains("\"schemaVersion\"", json);
        Assert.Contains("\"ResidentialA\"", json);
        Assert.Equal(original.SchemaVersion, copy.SchemaVersion);
        Assert.Equal(original.CodeEdition, copy.CodeEdition);
        Assert.Equal(original.Actions.ToArray(), copy.Actions.ToArray());
        Assert.True(ActionInventoryValidator.Validate(copy, model).IsValid);
        Assert.Empty(model.LoadCombinations);
    }

    [Fact]
    public void ActionId_IsUniqueIgnoringCase()
    {
        ActionInventory inventory = Inventory(G1(), Residential() with { ActionId = "g1" });
        AssertIssue(inventory, Model("LC_G1", "LC_Q"), "B2_DUPLICATE_ACTION_ID", ActionInventoryStatus.Invalid);
    }

    [Fact]
    public void ALoadCaseCannotBeAssignedTwoDifferentNormativeCategories()
    {
        ActionInventory inventory = Inventory(G1(), Residential() with { LoadCaseId = "lc_g1" });
        AssertIssue(inventory, Model("LC_G1"), "B2_DUPLICATE_LOAD_CASE", ActionInventoryStatus.Invalid);
    }

    [Fact]
    public void UnknownFemLoadCase_IsRejected()
    {
        AssertIssue(Inventory(G1()), Model("OTHER"), "B2_UNKNOWN_LOAD_CASE", ActionInventoryStatus.Invalid);
    }

    [Fact]
    public void ExistingFemCaseWithoutAction_IsIncomplete_NotValidated()
    {
        AssertIssue(Inventory(G1()), Model("LC_G1", "UNCLASSIFIED"),
            "B2_LOAD_CASE_UNCLASSIFIED", ActionInventoryStatus.Incomplete);
    }

    [Fact]
    public void PermanentActionCannotHaveVariablePsiCategory()
    {
        AssertIssue(Inventory(G1() with { VariableCategory = VariableActionCategory.ResidentialA }),
            Model("LC_G1"), "B2_PERMANENT_HAS_VARIABLE_CATEGORY", ActionInventoryStatus.Invalid);
    }

    [Fact]
    public void VariableNeedsExplicitPsiCategory()
    {
        AssertIssue(Inventory(Residential() with { VariableCategory = null }),
            Model("LC_Q"), "B2_VARIABLE_CATEGORY_MISSING", ActionInventoryStatus.Invalid);
    }

    [Theory]
    [InlineData(VariableActionCategory.RoofI)]
    [InlineData(VariableActionCategory.RoofK)]
    public void RoofCategoriesIAndK_AreUnsupportedWithoutEngineeringInput(VariableActionCategory category)
    {
        AssertIssue(Inventory(Residential() with { VariableCategory = category }),
            Model("LC_Q"), "B2_CATEGORY_CASE_BY_CASE", ActionInventoryStatus.Unsupported);
    }

    [Fact]
    public void SnowWithoutAltitude_IsIncomplete()
    {
        AssertIssue(Inventory(Variable("SNOW", "LC_SNOW", VariableActionCategory.SnowElevationUndetermined)),
            Model("LC_SNOW"), "B2_SNOW_ELEVATION_MISSING", ActionInventoryStatus.Incomplete);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptySourceEvidence_IsIncomplete(string evidence)
    {
        AssertIssue(Inventory(G1() with { SourceEvidenceId = evidence }),
            Model("LC_G1"), "B2_SOURCE_EVIDENCE_MISSING", ActionInventoryStatus.Incomplete);
    }

    [Fact]
    public void MissingHomogeneityEvidence_IsIncomplete()
    {
        AssertIssue(Inventory(G1() with { HomogeneityEvidenceId = "" }),
            Model("LC_G1"), "B2_HOMOGENEITY_EVIDENCE_MISSING", ActionInventoryStatus.Incomplete);
    }

    [Theory]
    [InlineData(ActionValueBasis.Design)]
    [InlineData(ActionValueBasis.Nominal)]
    public void PrefactoredOrNominalCases_AreNotSilentlyTreatedAsCharacteristic(ActionValueBasis basis)
    {
        AssertIssue(Inventory(G1() with { Basis = basis }),
            Model("LC_G1"), "B2_NONCHARACTERISTIC_BASIS", ActionInventoryStatus.Unsupported);
    }

    [Fact]
    public void TimberDurationWithoutSourceEvidence_IsIncomplete()
    {
        AssertIssue(Inventory(Residential() with { TimberDuration = TimberActionDuration.ShortTerm }),
            Model("LC_Q"), "B2_TIMBER_DURATION_EVIDENCE", ActionInventoryStatus.Incomplete);
    }

    [Fact]
    public void G2CoefficientExceptionWithoutEvidence_IsIncomplete()
    {
        AssertIssue(Inventory(G2() with { TreatG2AsG1ByEvidence = true }),
            Model("LC_G2"), "B2_G2_EXCEPTION_EVIDENCE_MISSING", ActionInventoryStatus.Incomplete);
    }

    [Fact]
    public void G2CoefficientExceptionOnG1_IsInvalid()
    {
        AssertIssue(Inventory(G1() with { TreatG2AsG1ByEvidence = true,
            G2CoefficientExceptionEvidenceId = "CLAIM" }),
            Model("LC_G1"), "B2_G2_EXCEPTION_WRONG_KIND", ActionInventoryStatus.Invalid);
    }

    [Fact]
    public void OnlyOneBranchOfDeclaredWindGroup_IsIncomplete()
    {
        AssertIssue(Inventory(Variable("W+", "LC_WP", VariableActionCategory.Wind, "WIND")),
            Model("LC_WP"), "B2_ALTERNATIVE_INCOMPLETE", ActionInventoryStatus.Incomplete);
    }

    [Fact]
    public void IncompatibleCategoriesInOneAlternativeGroup_AreInvalid()
    {
        AssertIssue(Inventory(
                Variable("W+", "LC_WP", VariableActionCategory.Wind, "WEATHER"),
                Variable("SNOW", "LC_S", VariableActionCategory.SnowAtOrBelow1000Metres, "WEATHER")),
            Model("LC_WP", "LC_S"), "B2_ALTERNATIVE_CATEGORY_MISMATCH", ActionInventoryStatus.Invalid);
    }

    [Fact]
    public void PermanentInAlternativeGroup_IsInvalid()
    {
        AssertIssue(Inventory(G1() with { AlternativeGroupId = "G" }),
            Model("LC_G1"), "B2_ALTERNATIVE_PERMANENT", ActionInventoryStatus.Invalid);
    }

    [Fact]
    public void UnrecognizedActionEdition_IsUnsupportedRatherThanSilentlyReusingNtc()
    {
        ActionInventory inventory = Inventory(G1()) with { CodeEdition = "EN1991-OTHER" };
        AssertIssue(inventory, Model("LC_G1"), "B2_CODE_UNSUPPORTED", ActionInventoryStatus.Unsupported);
    }

    [Fact]
    public void UnknownSchema_IsInvalid()
    {
        ActionInventory inventory = Inventory(G1()) with { SchemaVersion = "ntc2018-actions-v2" };
        AssertIssue(inventory, Model("LC_G1"), "B2_SCHEMA_UNSUPPORTED", ActionInventoryStatus.Invalid);
    }

    [Fact]
    public void UnsupportedAndInvalidIssues_HaveConservativePriority()
    {
        ActionInventory inventory = Inventory(Residential() with {
            VariableCategory = VariableActionCategory.RoofI, Basis = ActionValueBasis.Design,
            ActionId = "" });
        AssertIssue(inventory, Model("LC_Q"), "B2_ACTION_ID_INVALID", ActionInventoryStatus.Invalid);
    }

    [Fact]
    public void JsonUnknownField_AndNumericOrUnknownEnum_AreRejected()
    {
        string json = ActionInventoryJsonSerializer.Serialize(Inventory(Residential()));
        Assert.Throws<JsonException>(() => ActionInventoryJsonSerializer.Deserialize(
            json.Replace("\"sourceEvidenceId\"", "\"inventedEvidenceField\"", StringComparison.Ordinal)));
        Assert.Throws<JsonException>(() => ActionInventoryJsonSerializer.Deserialize(
            json.Replace("\"ResidentialA\"", "999", StringComparison.Ordinal)));
        Assert.Throws<JsonException>(() => ActionInventoryJsonSerializer.Deserialize(
            json.Replace("\"ResidentialA\"", "\"NotACategory\"", StringComparison.Ordinal)));
    }

    [Fact]
    public void LegacyManualNegativeFactor_RemainsPermitted_AndZeroFactorRejected()
    {
        StructuralModel model = Model("LC_G1");
        model.AddLoadCombination(new StructuralLoadCombination("MANUAL_NEG", "Manual negative",
            new[] { new StructuralLoadCombinationTerm("LC_G1", -0.5) }));
        model.AddLoadCombination(new StructuralLoadCombination("MANUAL_ZERO", "Manual zero",
            new[] { new StructuralLoadCombinationTerm("LC_G1", 0.0) }));

        StructuralModelValidationResult result = new StructuralModelValidator().Validate(model);

        Assert.Contains(result.Errors, x => x.Code == "LOAD_COMBINATION_INVALID_FACTOR" && x.EntityId == "MANUAL_ZERO");
        Assert.DoesNotContain(result.Errors, x => x.Code == "LOAD_COMBINATION_INVALID_FACTOR" && x.EntityId == "MANUAL_NEG");
    }

    private static ActionInventory Inventory(params ActionDefinition[] actions) =>
        new(ActionInventoryProfiles.SchemaVersion, ActionInventoryProfiles.CodeEdition, Array.AsReadOnly(actions));

    private static ActionDefinition G1() =>
        new("G1", "LC_G1", ActionNature.G1, null, ActionValueBasis.Characteristic, "SRC-G1", "HOMOG-G1");

    private static ActionDefinition G2() =>
        new("G2", "LC_G2", ActionNature.G2, null, ActionValueBasis.Characteristic, "SRC-G2", "HOMOG-G2");

    private static ActionDefinition Residential() =>
        Variable("QA", "LC_Q", VariableActionCategory.ResidentialA);

    private static ActionDefinition Variable(string id, string loadCaseId,
        VariableActionCategory category, string? alternativeGroup = null) =>
        new(id, loadCaseId, ActionNature.Q, category, ActionValueBasis.Characteristic,
            "SRC-" + id, "HOMOG-" + id, alternativeGroup);

    private static StructuralModel Model(params string[] loadCases)
    {
        StructuralModel model = new();
        foreach (string id in loadCases)
            model.AddLoadCase(new StructuralLoadCase(id, id));
        return model;
    }

    private static void AssertIssue(ActionInventory inventory, StructuralModel model,
        string code, ActionInventoryStatus status)
    {
        ActionInventoryValidationResult result = ActionInventoryValidator.Validate(inventory, model);
        Assert.Equal(status, result.Status);
        Assert.Contains(result.Issues, issue => issue.Code == code);
    }
}
