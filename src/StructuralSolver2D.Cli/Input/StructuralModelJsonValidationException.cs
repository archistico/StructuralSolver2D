using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Cli.Input;

/// <summary>Pre-analysis JSON contract failure with stable, inspectable diagnostics.</summary>
public sealed class StructuralModelJsonValidationException : InvalidOperationException
{
    public StructuralModelValidationResult Validation { get; }

    public StructuralModelJsonValidationException(StructuralModelValidationResult validation)
        : base("Invalid structural JSON model: " + string.Join("; ", validation.Errors.Select(
            issue => $"{issue.Code}: {issue.Message}")))
    {
        Validation = validation ?? throw new ArgumentNullException(nameof(validation));
    }
}
