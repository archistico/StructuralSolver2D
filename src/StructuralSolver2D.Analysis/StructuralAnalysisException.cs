using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis;

/// <summary>
/// Represents an error that prevents the structural analysis from being completed.
/// </summary>
public sealed class StructuralAnalysisException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StructuralAnalysisException"/> class.
    /// </summary>
    public StructuralAnalysisException(string message)
        : base(message)
    {
        ValidationIssues = Array.Empty<StructuralModelValidationIssue>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StructuralAnalysisException"/> class.
    /// </summary>
    public StructuralAnalysisException(
        string message,
        IReadOnlyList<StructuralModelValidationIssue> validationIssues)
        : base(message)
    {
        ValidationIssues = validationIssues;
    }

    /// <summary>
    /// Gets the validation issues that caused the analysis to fail, when available.
    /// </summary>
    public IReadOnlyList<StructuralModelValidationIssue> ValidationIssues { get; }
}
