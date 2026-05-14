namespace StructuralSolver2D.Core.Validation;

/// <summary>
/// Represents the validation result for a structural model.
/// </summary>
/// <param name="Issues">Validation issues.</param>
public sealed record StructuralModelValidationResult(
    IReadOnlyList<StructuralModelValidationIssue> Issues)
{
    /// <summary>
    /// Gets a value indicating whether the model has no validation errors.
    /// Warnings do not make the model invalid.
    /// </summary>
    public bool IsValid => Issues.All(issue => issue.Severity != StructuralModelValidationSeverity.Error);

    /// <summary>
    /// Gets only blocking validation errors.
    /// </summary>
    public IEnumerable<StructuralModelValidationIssue> Errors =>
        Issues.Where(issue => issue.Severity == StructuralModelValidationSeverity.Error);

    /// <summary>
    /// Gets a successful validation result.
    /// </summary>
    public static StructuralModelValidationResult Success { get; } = new(Array.Empty<StructuralModelValidationIssue>());
}
