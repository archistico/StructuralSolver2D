using StructuralSolver2D.Combinations.Actions;
using StructuralSolver2D.Combinations.Generation;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Combinations.Audit;

/// <summary>
/// Captures a defensible pre-analysis baseline for a FINITE declared action
/// plan, then compares actual Core combination terms and the current model
/// against it. No member/fatigue/geometric or global normative pass is issued.
/// </summary>
public static class Ntc2018DeclaredCombinationAuditor
{
    public static Ntc2018CombinationAuditBaseline Capture(
        ActionInventory inventory, StructuralModel model, Ntc2018DeclaredCombinationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(plan);

        Ntc2018DeclaredAuditResult coverage = Ntc2018DeclaredPlanValidator.Validate(inventory, model, plan);
        if (coverage.Status != Ntc2018DeclaredAuditStatus.MatchedDeclaredPlan)
            throw new ArgumentException("Incomplete or invalid declared plan: " +
                string.Join(", ", coverage.Issues.Select(x => x.Code)), nameof(plan));

        StructuralModelValidationResult core = new StructuralModelValidator().Validate(model);
        if (!core.IsValid)
            throw new ArgumentException("Core FEM model is invalid: " +
                string.Join(", ", core.Errors.Select(x => x.Code)), nameof(model));

        List<Ntc2018AuditedCandidate> snapshots = new();
        foreach (Ntc2018CombinationScenario scenario in plan.Scenarios)
        {
            Ntc2018GeneratedCombination generated =
                Ntc2018BuildingCombinationGenerator.Generate(inventory, model, scenario);
            string fingerprint = CoreCombinationInputFingerprint.Compute(model, inventory, plan, generated.FemCombination);
            StructuralLoadCombinationTerm[] expectedTerms = generated.FemCombination.Terms
                .Select(x => new StructuralLoadCombinationTerm(x.LoadCaseId, x.Factor)).ToArray();
            snapshots.Add(new Ntc2018AuditedCandidate(scenario.CombinationId, scenario.Family,
                scenario.LeadingActionId, Array.AsReadOnly(expectedTerms), fingerprint));
        }

        return new Ntc2018CombinationAuditBaseline(plan.PlanId, plan.EffectId, snapshots);
    }

    public static Ntc2018DeclaredAuditResult Audit(
        ActionInventory inventory,
        StructuralModel currentModel,
        Ntc2018DeclaredCombinationPlan plan,
        Ntc2018CombinationAuditBaseline baseline)
    {
        List<Ntc2018DeclaredAuditIssue> issues = new();
        void Add(string code, Ntc2018DeclaredAuditIssueLevel level, string? id, string message) =>
            issues.Add(new Ntc2018DeclaredAuditIssue(code, level, id, message));

        if (inventory is null || currentModel is null || plan is null || baseline is null)
        {
            Add("B4_AUDIT_INPUT_MISSING", Ntc2018DeclaredAuditIssueLevel.Invalid, null,
                "Audit requires the current FEM model, plan and captured baseline.");
            return new Ntc2018DeclaredAuditResult(issues);
        }

        Ntc2018DeclaredAuditResult planCheck = Ntc2018DeclaredPlanValidator.Validate(inventory, currentModel, plan);
        issues.AddRange(planCheck.Issues);

        StructuralModelValidationResult core = new StructuralModelValidator().Validate(currentModel);
        if (!core.IsValid)
            Add("B4_CURRENT_CORE_MODEL_INVALID", Ntc2018DeclaredAuditIssueLevel.Invalid, null,
                "Current Core model validation failed: " + string.Join(", ", core.Errors.Select(x => x.Code)));

        if (!string.Equals(baseline.PlanId, plan.PlanId, StringComparison.Ordinal) ||
            !string.Equals(baseline.EffectId, plan.EffectId, StringComparison.Ordinal))
            Add("B4_BASELINE_PLAN_MISMATCH", Ntc2018DeclaredAuditIssueLevel.Invalid, null,
                "Baseline was not captured for this effect and declared plan.");

        HashSet<string> actualIds = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StructuralLoadCombination> actualById = new(StringComparer.OrdinalIgnoreCase);
        // Critically, only the combos truly present in the model entering FEM
        // count; a caller-supplied parallel list could conceal missing terms.
        foreach (StructuralLoadCombination? actual in currentModel.LoadCombinations)
        {
            if (actual is null || string.IsNullOrWhiteSpace(actual.Id))
            {
                Add("B4_NULL_OR_UNNAMED_COMBINATION", Ntc2018DeclaredAuditIssueLevel.Invalid, null,
                    "Every actual Core combination requires a unique ID.");
                continue;
            }
            if (!actualIds.Add(actual.Id))
                Add("B4_DUPLICATE_ACTUAL_COMBINATION", Ntc2018DeclaredAuditIssueLevel.Invalid, actual.Id,
                    "Duplicate combination IDs make coverage ambiguous.");
            else
                actualById.Add(actual.Id, actual);
        }

        HashSet<string> expectedIds = new(baseline.Candidates.Select(x => x.CombinationId),
            StringComparer.OrdinalIgnoreCase);
        foreach (Ntc2018AuditedCandidate expected in baseline.Candidates)
        {
            if (!actualById.TryGetValue(expected.CombinationId, out StructuralLoadCombination? actual))
            {
                Add("B4_REQUIRED_COMBINATION_MISSING", Ntc2018DeclaredAuditIssueLevel.Incomplete,
                    expected.CombinationId, "Declared scenario has no matching actual Core combination.");
                continue;
            }

            Dictionary<string, double> actualTerms = new(StringComparer.OrdinalIgnoreCase);
            foreach (StructuralLoadCombinationTerm term in actual.Terms)
            {
                if (term is null || string.IsNullOrWhiteSpace(term.LoadCaseId) ||
                    !double.IsFinite(term.Factor) || term.Factor == 0)
                {
                    Add("B4_TERM_INVALID", Ntc2018DeclaredAuditIssueLevel.Invalid,
                        expected.CombinationId, "Null, nonfinite, zero or unidentified FEM term.");
                    continue;
                }
                if (!actualTerms.TryAdd(term.LoadCaseId, term.Factor))
                    Add("B4_TERM_DUPLICATED", Ntc2018DeclaredAuditIssueLevel.Invalid,
                        expected.CombinationId, $"Repeated Core load case '{term.LoadCaseId}'.");
            }

            HashSet<string> expectedTerms = new(StringComparer.OrdinalIgnoreCase);
            foreach (StructuralLoadCombinationTerm term in expected.ExpectedCoreTerms)
            {
                expectedTerms.Add(term.LoadCaseId);
                if (!actualTerms.TryGetValue(term.LoadCaseId, out double factor))
                    Add("B4_REQUIRED_TERM_MISSING", Ntc2018DeclaredAuditIssueLevel.Incomplete,
                        expected.CombinationId, $"Missing required FEM term '{term.LoadCaseId}'.");
                else if (factor != term.Factor)
                    Add("B4_TERM_FACTOR_MISMATCH", Ntc2018DeclaredAuditIssueLevel.Invalid,
                        expected.CombinationId, $"Effective factor changed for '{term.LoadCaseId}'.");
            }
            foreach (string extra in actualTerms.Keys.Where(id => !expectedTerms.Contains(id)))
                Add("B4_UNEXPECTED_TERM", Ntc2018DeclaredAuditIssueLevel.Invalid,
                    expected.CombinationId, $"Unplanned extra FEM load-case term '{extra}'.");

            string actualHash = CoreCombinationInputFingerprint.Compute(currentModel, inventory, plan, actual);
            if (!string.Equals(expected.CoreInputSha256, actualHash, StringComparison.Ordinal))
                Add("B4_CORE_INPUT_FINGERPRINT_MISMATCH", Ntc2018DeclaredAuditIssueLevel.Invalid,
                    expected.CombinationId, "Plan, action inventory, loads, geometry, physical supports or actual Core terms changed after capture.");
        }

        foreach (string extra in actualById.Keys.Where(x => !expectedIds.Contains(x)))
            Add("B4_UNDECLARED_COMBINATION", Ntc2018DeclaredAuditIssueLevel.Invalid, extra,
                "Actual candidate is not part of the frozen declared plan.");

        // EVEN when there are zero issues, this is *only* a declared-plan
        // match, NEVER a complete building, member or code-conformity report.
        return new Ntc2018DeclaredAuditResult(issues);
    }
}
