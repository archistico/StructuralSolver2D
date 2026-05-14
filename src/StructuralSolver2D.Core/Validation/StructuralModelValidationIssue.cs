namespace StructuralSolver2D.Core.Validation;

/// <summary>
/// Describes a single validation issue found in a structural model.
/// </summary>
/// <param name="Severity">Issue severity.</param>
/// <param name="Code">Stable issue code useful for tests and UI messages.</param>
/// <param name="Message">Human-readable issue message.</param>
/// <param name="EntityId">Optional identifier of the affected entity.</param>
public sealed record StructuralModelValidationIssue(
    StructuralModelValidationSeverity Severity,
    string Code,
    string Message,
    string? EntityId = null);
