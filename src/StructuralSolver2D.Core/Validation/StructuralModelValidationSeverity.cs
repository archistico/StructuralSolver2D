namespace StructuralSolver2D.Core.Validation;

/// <summary>
/// Severity level of a structural model validation issue.
/// </summary>
public enum StructuralModelValidationSeverity
{
    /// <summary>
    /// Informational validation message.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Non-blocking validation warning.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Blocking validation error.
    /// </summary>
    Error = 2
}
