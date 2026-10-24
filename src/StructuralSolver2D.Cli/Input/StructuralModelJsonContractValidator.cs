using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Cli.Input;

/// <summary>
/// M75 opt-in preflight for the standard model JSON contract. Does not run FEM,
/// mutate user data, or mistake an unanalysed model for a verified structure.
/// </summary>
public sealed class StructuralModelJsonContractValidator
{
    public StructuralModelValidationResult Validate(StructuralModelJsonFile file, bool allowModalWithoutLoadCase = false)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(file.Model);

        StructuralModelValidationResult baseResult = new StructuralModelValidator().Validate(file.Model);
        var issues = new List<StructuralModelValidationIssue>(baseResult.Issues);
        void Error(string code, string message, string? id = null) => issues.Add(new StructuralModelValidationIssue(
            StructuralModelValidationSeverity.Error, code, message, id));

        if (string.IsNullOrWhiteSpace(file.LoadCaseId))
        {
            if (!allowModalWithoutLoadCase)
                Error("INPUT_TARGET_REQUIRED", "The standard analysis requires a loadCaseId or loadCombinationId.");
        }
        else if (file.DefaultTargetIsCombination)
        {
            if (!file.Model.LoadCombinations.Any(item => item is not null &&
                string.Equals(item.Id, file.LoadCaseId, StringComparison.OrdinalIgnoreCase)))
                Error("INPUT_COMBINATION_NOT_FOUND", $"Default load combination '{file.LoadCaseId}' does not exist.", file.LoadCaseId);
        }
        else if (!file.Model.LoadCases.Any(item => item is not null &&
            string.Equals(item.Id, file.LoadCaseId, StringComparison.OrdinalIgnoreCase)))
        {
            Error("INPUT_LOAD_CASE_NOT_FOUND", $"Default load case '{file.LoadCaseId}' does not exist.", file.LoadCaseId);
        }

        // DynamicsValidator also includes structural errors; only enter it when geometry
        // and references are sound, and do not duplicate issues already reported above.
        if (file.Dynamics is not null && baseResult.IsValid)
        {
            StructuralModelValidationResult dynamicResult =
                new StructuralDynamicsValidator().Validate(file.Model, file.Dynamics);
            foreach (StructuralModelValidationIssue issue in dynamicResult.Issues)
                if (!issues.Contains(issue)) issues.Add(issue);
        }
        return new StructuralModelValidationResult(issues);
    }
}
