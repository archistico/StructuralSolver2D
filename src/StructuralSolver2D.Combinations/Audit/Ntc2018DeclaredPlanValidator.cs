using StructuralSolver2D.Combinations.Actions;
using StructuralSolver2D.Combinations.Generation;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Combinations.Audit;

/// <summary>
/// Validates closure of a finite *declared* plan over known actions, leaders
/// and mutually exclusive branches. It cannot independently prove which
/// actual loading patterns/structural effects an engineer should have chosen.
/// </summary>
public static class Ntc2018DeclaredPlanValidator
{
    public static Ntc2018DeclaredAuditResult Validate(
        ActionInventory inventory, StructuralModel model, Ntc2018DeclaredCombinationPlan plan)
    {
        List<Ntc2018DeclaredAuditIssue> issues = new();
        void Add(string code, Ntc2018DeclaredAuditIssueLevel level, string? id, string message) =>
            issues.Add(new Ntc2018DeclaredAuditIssue(code, level, id, message));

        if (inventory is null || model is null || plan is null)
        {
            Add("B4_INPUT_MISSING", Ntc2018DeclaredAuditIssueLevel.Invalid, null, "Model, typed inventory and declared plan are required.");
            return new Ntc2018DeclaredAuditResult(issues);
        }

        ActionInventoryValidationResult b2 = ActionInventoryValidator.Validate(inventory, model);
        if (!b2.IsValid)
        {
            Add("B4_INVENTORY_NOT_VALID", b2.Status == ActionInventoryStatus.Unsupported
                    ? Ntc2018DeclaredAuditIssueLevel.Unsupported : Ntc2018DeclaredAuditIssueLevel.Invalid,
                null, "Resolve P0-B2 action-inventory issues before auditing a combination plan.");
            return new Ntc2018DeclaredAuditResult(issues);
        }

        if (string.IsNullOrWhiteSpace(plan.PlanId) || string.IsNullOrWhiteSpace(plan.EffectId))
            Add("B4_PLAN_IDENTITY_MISSING", Ntc2018DeclaredAuditIssueLevel.Invalid, null,
                "The declared plan and scalar effect need unique identifiers.");
        if (string.IsNullOrWhiteSpace(plan.PhysicalCompatibilityEvidenceId))
            Add("B4_COMPATIBILITY_EVIDENCE_MISSING", Ntc2018DeclaredAuditIssueLevel.Incomplete, null,
                "Physical/spatial action coexistence requires external engineering evidence.");

        if (plan.RequiredFamilies is null || plan.RequiredFamilies.Count == 0 ||
            plan.RequiredFamilies.Count != plan.RequiredFamilies.Distinct().Count() ||
            plan.RequiredFamilies.Any(x => !Enum.IsDefined(x)))
            Add("B4_FAMILIES_INVALID", Ntc2018DeclaredAuditIssueLevel.Invalid, null,
                "Declare distinct, supported NTC action-combination families.");

        if (plan.InScopeActionIds is null || plan.OutOfScopeActionEvidence is null ||
            plan.NeverLeadingVariableEvidence is null || plan.Scenarios is null)
        {
            Add("B4_SCOPE_MISSING", Ntc2018DeclaredAuditIssueLevel.Invalid, null,
                "The full inventory, exclusions, leading exceptions and scenarios must be explicitly supplied.");
            return new Ntc2018DeclaredAuditResult(issues);
        }

        Dictionary<string, ActionDefinition> actions = inventory.Actions.ToDictionary(x => x.ActionId,
            StringComparer.OrdinalIgnoreCase);
        HashSet<string> inScope = new(StringComparer.OrdinalIgnoreCase);
        foreach (string id in plan.InScopeActionIds)
        {
            if (string.IsNullOrWhiteSpace(id) || !actions.ContainsKey(id) || !inScope.Add(id))
                Add("B4_SCOPE_ACTION_INVALID", Ntc2018DeclaredAuditIssueLevel.Invalid, id,
                    "Unregistered or duplicate in-scope action.");
        }

        Dictionary<string, string> outScope = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> item in plan.OutOfScopeActionEvidence)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || !actions.ContainsKey(item.Key) ||
                inScope.Contains(item.Key) || string.IsNullOrWhiteSpace(item.Value) ||
                !outScope.TryAdd(item.Key, item.Value))
                Add("B4_EXCLUSION_INVALID", Ntc2018DeclaredAuditIssueLevel.Invalid, item.Key,
                    "Out-of-scope classification requires a known action, explicit evidence and no overlap.");
        }
        foreach (ActionDefinition action in inventory.Actions)
            if (!inScope.Contains(action.ActionId) && !outScope.ContainsKey(action.ActionId))
                Add("B4_ACTION_UNCLASSIFIED", Ntc2018DeclaredAuditIssueLevel.Incomplete, action.ActionId,
                    "Inventory action is neither in scope nor explicitly excluded with evidence.");

        HashSet<string> neverLeading = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> item in plan.NeverLeadingVariableEvidence)
        {
            if (!actions.TryGetValue(item.Key, out ActionDefinition? action) ||
                !inScope.Contains(item.Key) || action.Nature != ActionNature.Q ||
                string.IsNullOrWhiteSpace(item.Value) || !neverLeading.Add(item.Key))
                Add("B4_LEADING_EXCLUSION_INVALID", Ntc2018DeclaredAuditIssueLevel.Invalid, item.Key,
                    "A never-leading exemption is only valid for an in-scope Q action with evidence.");
        }

        HashSet<string> scenarioIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (Ntc2018CombinationScenario? scenario in plan.Scenarios)
        {
            if (scenario is null || string.IsNullOrWhiteSpace(scenario.CombinationId) ||
                !scenarioIds.Add(scenario.CombinationId))
            {
                Add("B4_SCENARIO_DUPLICATE_OR_NULL", Ntc2018DeclaredAuditIssueLevel.Invalid, null,
                    "Scenarios require unique, nonblank combination IDs.");
                continue;
            }
            if (!string.Equals(scenario.EffectId, plan.EffectId, StringComparison.Ordinal))
                Add("B4_EFFECT_MISMATCH", Ntc2018DeclaredAuditIssueLevel.Invalid, scenario.CombinationId,
                    "One audit plan covers one declared scalar effect only.");
            if (plan.RequiredFamilies is not null && !plan.RequiredFamilies.Contains(scenario.Family))
                Add("B4_FAMILY_OUTSIDE_PLAN", Ntc2018DeclaredAuditIssueLevel.Invalid, scenario.CombinationId,
                    "Scenario family not declared by the audit plan.");
            if (scenario.PresentActionIds is null)
            {
                Add("B4_SCENARIO_ACTIONS_MISSING", Ntc2018DeclaredAuditIssueLevel.Invalid, scenario.CombinationId,
                    "Scenario has no selected action inventory.");
                continue;
            }
            HashSet<string> selected = scenario.PresentActionIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (selected.Count == 0 || selected.Count != scenario.PresentActionIds.Count ||
                scenario.PresentActionIds.Any(string.IsNullOrWhiteSpace))
                Add("B4_SCENARIO_SELECTION_INVALID", Ntc2018DeclaredAuditIssueLevel.Invalid,
                    scenario.CombinationId, "Selected action IDs must be nonblank, distinct and nonempty.");
            foreach (string id in selected)
                if (!inScope.Contains(id))
                    Add("B4_SCENARIO_OUTSIDE_SCOPE", Ntc2018DeclaredAuditIssueLevel.Invalid, scenario.CombinationId,
                        $"Selected action '{id}' does not belong to this declared effect scope.");

            bool hasVariable = selected.Any(id => !string.IsNullOrWhiteSpace(id) &&
                actions.TryGetValue(id, out ActionDefinition? action) &&
                action.Nature == ActionNature.Q);
            if (scenario.Family == Ntc2018CombinationFamily.SlsQuasiPermanent)
            {
                if (scenario.LeadingActionId is not null)
                    Add("B4_QUASI_HAS_LEADER", Ntc2018DeclaredAuditIssueLevel.Invalid, scenario.CombinationId,
                        "Quasi-permanent SLE cannot have a privileged leading action.");
            }
            else if (hasVariable && (string.IsNullOrWhiteSpace(scenario.LeadingActionId) ||
                !selected.Contains(scenario.LeadingActionId) ||
                !actions.TryGetValue(scenario.LeadingActionId, out ActionDefinition? leading) ||
                leading.Nature != ActionNature.Q))
                Add("B4_SCENARIO_LEADER_INVALID", Ntc2018DeclaredAuditIssueLevel.Invalid, scenario.CombinationId,
                    "A selected Q leader is required by this family.");
            else if (!hasVariable && scenario.LeadingActionId is not null)
                Add("B4_SCENARIO_LEADER_INVALID", Ntc2018DeclaredAuditIssueLevel.Invalid, scenario.CombinationId,
                    "A permanent-only scenario cannot select a Q leader.");

            if (scenario.LeadingActionId is not null && neverLeading.Contains(scenario.LeadingActionId))
                Add("B4_NEVER_LEADING_CONTRADICTION", Ntc2018DeclaredAuditIssueLevel.Invalid,
                    scenario.CombinationId, "A documented never-leading action cannot lead a scenario.");
            if (scenario.FavorableActionIds is null ||
                scenario.FavorableActionIds.Count != scenario.FavorableActionIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() ||
                scenario.FavorableActionIds.Any(id => !selected.Contains(id)) ||
                string.IsNullOrWhiteSpace(scenario.FavorabilityEvidenceId))
                Add("B4_FAVORABILITY_INVALID", Ntc2018DeclaredAuditIssueLevel.Invalid,
                    scenario.CombinationId, "Favorable actions must be selected, distinct and supported by effect evidence.");

            HashSet<string> activeAlternatives = new(StringComparer.OrdinalIgnoreCase);
            foreach (string id in selected)
                if (!string.IsNullOrWhiteSpace(id) && actions.TryGetValue(id, out ActionDefinition? item) &&
                    !string.IsNullOrWhiteSpace(item.AlternativeGroupId) &&
                    !activeAlternatives.Add(item.AlternativeGroupId))
                    Add("B4_ALTERNATIVES_COLLIDE", Ntc2018DeclaredAuditIssueLevel.Invalid,
                        scenario.CombinationId, "Mutually exclusive spatial patterns cannot coexist.");

            IReadOnlyDictionary<string, string> absent = scenario.PhysicallyAbsentActionEvidence ??
                new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> absence in absent)
                if (!actions.ContainsKey(absence.Key) || selected.Contains(absence.Key) ||
                    string.IsNullOrWhiteSpace(absence.Value))
                    Add("B4_ABSENCE_INVALID", Ntc2018DeclaredAuditIssueLevel.Invalid,
                        scenario.CombinationId, "Physical absence requires known, unselected action and evidence.");
            foreach (ActionDefinition action in inventory.Actions.Where(x => inScope.Contains(x.ActionId)))
            {
                if (selected.Contains(action.ActionId)) continue;
                bool physicalAbsence = absent.Any(x =>
                    string.Equals(x.Key, action.ActionId, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(x.Value));
                bool replacedByAlternative = !string.IsNullOrWhiteSpace(action.AlternativeGroupId) &&
                    selected.Any(other => !string.IsNullOrWhiteSpace(other) &&
                        actions.TryGetValue(other, out ActionDefinition? selectedAction) &&
                        string.Equals(selectedAction.AlternativeGroupId, action.AlternativeGroupId,
                            StringComparison.OrdinalIgnoreCase));
                if (!physicalAbsence && !replacedByAlternative)
                    Add("B4_SCENARIO_ACTION_UNRESOLVED", Ntc2018DeclaredAuditIssueLevel.Incomplete,
                        scenario.CombinationId, $"In-scope action '{action.ActionId}' is absent without explanation.");
            }
        }

        foreach (Ntc2018CombinationFamily family in plan.RequiredFamilies ?? Array.Empty<Ntc2018CombinationFamily>())
        {
            Ntc2018CombinationScenario[] familyScenarios = plan.Scenarios
                .Where(x => x is not null && x.Family == family).ToArray();
            if (familyScenarios.Length == 0)
            {
                Add("B4_FAMILY_NOT_COVERED", Ntc2018DeclaredAuditIssueLevel.Incomplete, null,
                    $"No declared scenario for {family}.");
                continue;
            }

            foreach (ActionDefinition action in inventory.Actions.Where(x => x.Nature == ActionNature.Q &&
                         inScope.Contains(x.ActionId) &&
                         (family == Ntc2018CombinationFamily.SlsQuasiPermanent || !neverLeading.Contains(x.ActionId))))
            {
                bool covered = family == Ntc2018CombinationFamily.SlsQuasiPermanent
                    ? familyScenarios.Any(x => x.PresentActionIds is not null &&
                        x.PresentActionIds.Contains(action.ActionId, StringComparer.OrdinalIgnoreCase))
                    : familyScenarios.Any(x => string.Equals(x.LeadingActionId, action.ActionId,
                        StringComparison.OrdinalIgnoreCase));
                if (!covered)
                    Add("B4_LEADING_VARIANT_MISSING", Ntc2018DeclaredAuditIssueLevel.Incomplete,
                        action.ActionId, $"{family}: missing applicable leader/pattern for action {action.ActionId}.");
            }
        }

        return new Ntc2018DeclaredAuditResult(issues);
    }
}
