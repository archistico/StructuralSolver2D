using StructuralSolver2D.Combinations.Actions;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Combinations.Generation;

/// <summary>
/// Generates one explicit, code-profiled *candidate* from a declared scenario
/// and typed characteristic cases. Never claims a complete combination set.
/// It does not infer sign-dependent favorability or inspect FEM result vectors.
/// </summary>
public static class Ntc2018BuildingCombinationGenerator
{
    public static Ntc2018GeneratedCombination Generate(
        ActionInventory inventory,
        StructuralModel model,
        Ntc2018CombinationScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(scenario);

        ActionInventoryValidationResult input = ActionInventoryValidator.Validate(inventory, model);
        if (!input.IsValid)
            throw new ArgumentException("P0-B2 inventory is not valid: " +
                string.Join(", ", input.Issues.Select(i => i.Code)), nameof(inventory));

        if (string.IsNullOrWhiteSpace(scenario.CombinationId) ||
            string.IsNullOrWhiteSpace(scenario.EffectId) ||
            string.IsNullOrWhiteSpace(scenario.FavorabilityEvidenceId))
            throw new ArgumentException("Combination ID, scalar-effect identity and favorability evidence are mandatory.", nameof(scenario));

        if (model.LoadCases.Any(x => string.Equals(x.Id, scenario.CombinationId, StringComparison.OrdinalIgnoreCase)) ||
            model.LoadCombinations.Any(x => string.Equals(x.Id, scenario.CombinationId, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Generated combination ID collides with an existing FEM case or combination.", nameof(scenario));

        if (!Enum.IsDefined(scenario.Family))
            throw new NotSupportedException("Unsupported NTC combination family.");
        if (scenario.PresentActionIds is null || scenario.FavorableActionIds is null ||
            scenario.PresentActionIds.Count == 0)
            throw new ArgumentException("Declare a nonempty selection and an explicit favorable-action list.", nameof(scenario));

        Dictionary<string, ActionDefinition> actions = inventory.Actions
            .ToDictionary(x => x.ActionId, StringComparer.OrdinalIgnoreCase);
        HashSet<string> present = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> favorable = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> alternativeGroups = new(StringComparer.OrdinalIgnoreCase);
        foreach (string id in scenario.PresentActionIds)
        {
            if (string.IsNullOrWhiteSpace(id) || !actions.TryGetValue(id, out ActionDefinition? action))
                throw new ArgumentException($"Scenario contains unregistered action '{id}'.", nameof(scenario));
            if (!present.Add(id))
                throw new ArgumentException($"Duplicate action '{id}' in the scenario.", nameof(scenario));
            if (!string.IsNullOrWhiteSpace(action.AlternativeGroupId) &&
                !alternativeGroups.Add(action.AlternativeGroupId))
                throw new ArgumentException($"Mutually exclusive alternatives from group '{action.AlternativeGroupId}' were combined.", nameof(scenario));
        }

        foreach (string id in scenario.FavorableActionIds)
        {
            if (string.IsNullOrWhiteSpace(id) || !present.Contains(id) || !favorable.Add(id))
                throw new ArgumentException($"Invalid, duplicate or nonpresent favorable action '{id}'.", nameof(scenario));
        }

        Dictionary<string, string> absence = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> item in scenario.PhysicallyAbsentActionEvidence ??
                     new Dictionary<string, string>())
        {
            if (!actions.ContainsKey(item.Key) || present.Contains(item.Key) ||
                string.IsNullOrWhiteSpace(item.Value) || !absence.TryAdd(item.Key, item.Value))
                throw new ArgumentException($"Invalid physical-absence evidence for '{item.Key}'.", nameof(scenario));
        }

        bool hasVariable = scenario.PresentActionIds.Any(id => actions[id].Nature == ActionNature.Q);
        if (scenario.Family == Ntc2018CombinationFamily.SlsQuasiPermanent)
        {
            if (scenario.LeadingActionId is not null)
                throw new ArgumentException("Quasi-permanent SLE has no privileged leading variable.", nameof(scenario));
        }
        else if (hasVariable)
        {
            if (string.IsNullOrWhiteSpace(scenario.LeadingActionId) ||
                !present.Contains(scenario.LeadingActionId) ||
                actions[scenario.LeadingActionId].Nature != ActionNature.Q)
                throw new ArgumentException("A present variable leader is required for ULS, rare or frequent SLE.", nameof(scenario));
        }
        else if (scenario.LeadingActionId is not null)
            throw new ArgumentException("A permanent-only combination cannot have a variable leader.", nameof(scenario));

        List<StructuralLoadCombinationTerm> femTerms = new();
        List<Ntc2018GeneratedTerm> detailed = new();
        List<Ntc2018OmittedAction> omitted = new();
        foreach (string id in scenario.PresentActionIds)
        {
            ActionDefinition action = actions[id];
            bool isFavorable = favorable.Contains(id);

            if (action.Nature == ActionNature.Q && isFavorable)
            {
                omitted.Add(new Ntc2018OmittedAction(action.ActionId, action.LoadCaseId,
                    Ntc2018OmissionReason.FavorableEffect, scenario.FavorabilityEvidenceId));
                continue;
            }

            decimal gamma;
            decimal psi;
            Ntc2018ActionRole role;
            if (action.Nature != ActionNature.Q)
            {
                role = Ntc2018ActionRole.Permanent;
                gamma = scenario.Family == Ntc2018CombinationFamily.UlsStrA1
                    ? Ntc2018BuildingActionCoefficients.GammaStrA1(
                        action.Nature, isFavorable, action.TreatG2AsG1ByEvidence)
                    : 1m;
                psi = 1m;
            }
            else
            {
                (decimal psi0, decimal psi1, decimal psi2) =
                    Ntc2018BuildingActionCoefficients.Psi(action.VariableCategory!.Value);
                bool leading = string.Equals(id, scenario.LeadingActionId, StringComparison.OrdinalIgnoreCase);
                role = scenario.Family == Ntc2018CombinationFamily.SlsQuasiPermanent
                    ? Ntc2018ActionRole.QuasiPermanentVariable
                    : leading ? Ntc2018ActionRole.LeadingVariable : Ntc2018ActionRole.AccompanyingVariable;
                gamma = scenario.Family == Ntc2018CombinationFamily.UlsStrA1 ? 1.5m : 1m;
                psi = scenario.Family switch
                {
                    Ntc2018CombinationFamily.UlsStrA1 or Ntc2018CombinationFamily.SlsRare =>
                        leading ? 1m : psi0,
                    Ntc2018CombinationFamily.SlsFrequent => leading ? psi1 : psi2,
                    Ntc2018CombinationFamily.SlsQuasiPermanent => psi2,
                    _ => throw new NotSupportedException("Combination family not supported.")
                };
            }

            decimal factor = gamma * psi;
            if (factor == 0m)
            {
                omitted.Add(new Ntc2018OmittedAction(action.ActionId, action.LoadCaseId,
                    Ntc2018OmissionReason.ZeroPsi, Ntc2018BuildingActionCoefficients.CodeSource));
                continue;
            }
            if (factor < 0m)
                throw new InvalidOperationException("A normative coefficient cannot be negative; loads retain their original signs.");

            femTerms.Add(new StructuralLoadCombinationTerm(action.LoadCaseId, (double)factor));
            detailed.Add(new Ntc2018GeneratedTerm(action.ActionId, action.LoadCaseId, role,
                gamma, psi, factor, Ntc2018BuildingActionCoefficients.CodeSource,
                action.SourceEvidenceId,
                action.TreatG2AsG1ByEvidence ? action.G2CoefficientExceptionEvidenceId : null));
        }

        foreach (ActionDefinition action in inventory.Actions.Where(x => !present.Contains(x.ActionId)))
        {
            bool physicallyAbsent = absence.TryGetValue(action.ActionId, out string? evidence);
            omitted.Add(new Ntc2018OmittedAction(action.ActionId, action.LoadCaseId,
                physicallyAbsent ? Ntc2018OmissionReason.PhysicallyAbsentByEvidence
                                 : Ntc2018OmissionReason.NotSelectedInScenario,
                physicallyAbsent ? evidence! : "SCENARIO_ONLY_NOT_A_COMPLETENESS_AUDIT"));
        }

        if (femTerms.Count == 0)
            throw new InvalidOperationException("No nonzero FEM terms remain; a Core combination would be invalid.");

        StructuralLoadCombination fem = new(scenario.CombinationId, scenario.CombinationId,
            femTerms, "NTC 2018 candidate for an explicitly supplied effect; NOT audited for full normative coverage.");
        Ntc2018CombinationManifest manifest = new(scenario.CombinationId, scenario.EffectId,
            scenario.Family, inventory.CodeEdition, scenario.LeadingActionId,
            Array.AsReadOnly(detailed.ToArray()), Array.AsReadOnly(omitted.ToArray()));
        return new Ntc2018GeneratedCombination(fem, manifest);
    }
}
