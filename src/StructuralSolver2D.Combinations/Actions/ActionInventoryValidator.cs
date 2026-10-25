using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Combinations.Actions;

/// <summary>
/// Validates a declared *inventory* of characteristic actions against a Core
/// structural model. It never generates factors, alters the FEM or infers
/// favorable/unfavorable effects. That is work for P0-B3/B4 and P0-C.
/// </summary>
public static class ActionInventoryValidator
{
    public static ActionInventoryValidationResult Validate(ActionInventory? inventory, StructuralModel? model)
    {
        List<ActionInventoryIssue> issues = new();

        void Add(string code, ActionInventoryIssueLevel level, string? id, string message) =>
            issues.Add(new ActionInventoryIssue(code, level, id, message));

        if (inventory is null)
        {
            Add("B2_INVENTORY_MISSING", ActionInventoryIssueLevel.Invalid, null, "Action inventory is required.");
            return new ActionInventoryValidationResult(issues);
        }

        if (model is null)
        {
            Add("B2_MODEL_MISSING", ActionInventoryIssueLevel.Invalid, null, "Core structural model is required.");
            return new ActionInventoryValidationResult(issues);
        }

        if (!string.Equals(inventory.SchemaVersion, ActionInventoryProfiles.SchemaVersion, StringComparison.Ordinal))
            Add("B2_SCHEMA_UNSUPPORTED", ActionInventoryIssueLevel.Invalid, null, "Unknown action-inventory schema version.");

        if (!string.Equals(inventory.CodeEdition, ActionInventoryProfiles.CodeEdition, StringComparison.Ordinal))
            Add("B2_CODE_UNSUPPORTED", ActionInventoryIssueLevel.Unsupported, null, "Only the explicitly versioned NTC 2018 profile is recognized.");

        if (inventory.Actions is null || inventory.Actions.Count == 0)
        {
            Add("B2_ACTIONS_MISSING", ActionInventoryIssueLevel.Incomplete, null, "No typed actions were declared.");
            return new ActionInventoryValidationResult(issues);
        }

        HashSet<string> modelLoadCaseIds = model.LoadCases
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        HashSet<string> actionIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> declaredLoadCaseIds = new(StringComparer.OrdinalIgnoreCase);
        List<ActionDefinition> definedActions = new();

        foreach (ActionDefinition? action in inventory.Actions)
        {
            if (action is null)
            {
                Add("B2_NULL_ACTION", ActionInventoryIssueLevel.Invalid, null, "Null inventory action.");
                continue;
            }

            definedActions.Add(action);
            string? id = action.ActionId;
            if (string.IsNullOrWhiteSpace(id) || !string.Equals(id, id.Trim(), StringComparison.Ordinal))
                Add("B2_ACTION_ID_INVALID", ActionInventoryIssueLevel.Invalid, id, "Action id must be nonblank and trimmed.");
            else if (!actionIds.Add(id))
                Add("B2_DUPLICATE_ACTION_ID", ActionInventoryIssueLevel.Invalid, id, "Action ids must be unique (case-insensitive).");

            if (string.IsNullOrWhiteSpace(action.LoadCaseId) ||
                !string.Equals(action.LoadCaseId, action.LoadCaseId.Trim(), StringComparison.Ordinal))
                Add("B2_LOAD_CASE_ID_INVALID", ActionInventoryIssueLevel.Invalid, id, "Core load-case id must be nonblank and trimmed.");
            else
            {
                if (!declaredLoadCaseIds.Add(action.LoadCaseId))
                    Add("B2_DUPLICATE_LOAD_CASE", ActionInventoryIssueLevel.Invalid, id,
                        "One Core load case cannot represent multiple action definitions or normative factor categories.");
                if (!modelLoadCaseIds.Contains(action.LoadCaseId))
                    Add("B2_UNKNOWN_LOAD_CASE", ActionInventoryIssueLevel.Invalid, id, "Referenced FEM load case does not exist.");
            }

            if (string.IsNullOrWhiteSpace(action.SourceEvidenceId))
                Add("B2_SOURCE_EVIDENCE_MISSING", ActionInventoryIssueLevel.Incomplete, id,
                    "Characteristic origin/category require declared evidence.");
            if (string.IsNullOrWhiteSpace(action.HomogeneityEvidenceId))
                Add("B2_HOMOGENEITY_EVIDENCE_MISSING", ActionInventoryIssueLevel.Incomplete, id,
                    "Homogeneous nature and characteristic basis of all loads in this case must be evidenced externally.");

            if (!Enum.IsDefined(action.Nature))
                Add("B2_NATURE_INVALID", ActionInventoryIssueLevel.Invalid, id, "Action nature is not a defined G1/G2/Q value.");
            else if (action.Nature == ActionNature.Q)
            {
                if (action.VariableCategory is null || !Enum.IsDefined(action.VariableCategory.Value))
                    Add("B2_VARIABLE_CATEGORY_MISSING", ActionInventoryIssueLevel.Invalid, id, "Variable action needs a recognized category.");
                else if (action.VariableCategory is VariableActionCategory.RoofI or VariableActionCategory.RoofK)
                    Add("B2_CATEGORY_CASE_BY_CASE", ActionInventoryIssueLevel.Unsupported, id,
                        "NTC 2018 I/K requires case-specific psi values; no automatic default is available.");
                else if (action.VariableCategory == VariableActionCategory.SnowElevationUndetermined)
                    Add("B2_SNOW_ELEVATION_MISSING", ActionInventoryIssueLevel.Incomplete, id,
                        "Distinguish snow at/below 1000 m and above 1000 m; provide location/elevation evidence.");
            }
            else if (action.VariableCategory is not null)
                Add("B2_PERMANENT_HAS_VARIABLE_CATEGORY", ActionInventoryIssueLevel.Invalid, id,
                    "G1/G2 must not be assigned a variable-use psi category.");

            if (!Enum.IsDefined(action.Basis))
                Add("B2_BASIS_INVALID", ActionInventoryIssueLevel.Invalid, id, "Unrecognized action value basis.");
            else if (action.Basis != ActionValueBasis.Characteristic)
                Add("B2_NONCHARACTERISTIC_BASIS", ActionInventoryIssueLevel.Unsupported, id,
                    "P0-B2 only admits characteristic unfactored FEM cases; no implicit nominal-to-characteristic conversion.");

            if (action.TimberDuration.HasValue != !string.IsNullOrWhiteSpace(action.TimberDurationEvidenceId))
                Add("B2_TIMBER_DURATION_EVIDENCE", ActionInventoryIssueLevel.Incomplete, id,
                    "Duration and its evidence must be supplied together when requested.");
            if (action.TimberDuration.HasValue && !Enum.IsDefined(action.TimberDuration.Value))
                Add("B2_TIMBER_DURATION_INVALID", ActionInventoryIssueLevel.Invalid, id,
                    "Unrecognized duration class.");

            if (action.TreatG2AsG1ByEvidence && action.Nature != ActionNature.G2)
                Add("B2_G2_EXCEPTION_WRONG_KIND", ActionInventoryIssueLevel.Invalid, id,
                    "Only documented G2 may request the G1 coefficient exception.");
            if (action.TreatG2AsG1ByEvidence && string.IsNullOrWhiteSpace(action.G2CoefficientExceptionEvidenceId))
                Add("B2_G2_EXCEPTION_EVIDENCE_MISSING", ActionInventoryIssueLevel.Incomplete, id,
                    "Known G2 magnitude exception needs engineering evidence.");
            if (!action.TreatG2AsG1ByEvidence && !string.IsNullOrWhiteSpace(action.G2CoefficientExceptionEvidenceId))
                Add("B2_G2_EXCEPTION_CONTRADICTION", ActionInventoryIssueLevel.Invalid, id,
                    "G2 evidence cannot be present when the exception is disabled.");

            if (action.AlternativeGroupId is not null &&
                (string.IsNullOrWhiteSpace(action.AlternativeGroupId) ||
                 !string.Equals(action.AlternativeGroupId, action.AlternativeGroupId.Trim(), StringComparison.Ordinal)))
                Add("B2_ALTERNATIVE_GROUP_INVALID", ActionInventoryIssueLevel.Invalid, id,
                    "Alternative group, if specified, must be nonblank and trimmed.");
            if (!string.IsNullOrWhiteSpace(action.AlternativeGroupId) && action.Nature != ActionNature.Q)
                Add("B2_ALTERNATIVE_PERMANENT", ActionInventoryIssueLevel.Invalid, id,
                    "Alternative-group handling currently applies only to variable characteristic cases.");
        }

        foreach (string missingId in modelLoadCaseIds.Where(x => !declaredLoadCaseIds.Contains(x)))
            Add("B2_LOAD_CASE_UNCLASSIFIED", ActionInventoryIssueLevel.Incomplete, missingId,
                "Core load case has no typed characteristic action; do not claim normative combination completeness.");

        foreach (IGrouping<string, ActionDefinition> group in definedActions
                     .Where(x => !string.IsNullOrWhiteSpace(x.AlternativeGroupId))
                     .GroupBy(x => x.AlternativeGroupId!, StringComparer.OrdinalIgnoreCase))
        {
            ActionDefinition[] alternatives = group.ToArray();
            if (alternatives.Length < 2)
                Add("B2_ALTERNATIVE_INCOMPLETE", ActionInventoryIssueLevel.Incomplete, group.Key,
                    "Only one branch of a declared mutually exclusive alternative group was provided.");
            if (alternatives.Any(x => x.Nature != ActionNature.Q) ||
                alternatives.Select(x => x.VariableCategory).Distinct().Count() > 1)
                Add("B2_ALTERNATIVE_CATEGORY_MISMATCH", ActionInventoryIssueLevel.Invalid, group.Key,
                    "A declared alternative group must contain one variable category (e.g. wind plus/minus).");
        }

        return new ActionInventoryValidationResult(issues);
    }
}
