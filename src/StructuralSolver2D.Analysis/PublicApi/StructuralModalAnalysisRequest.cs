using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// Future public modal analysis input contract (M55 internal experimental solver is not exposed).
/// Intentionally independent from static StructuralAnalysisRequest and its load cases.
/// Temporal excitations and initial states are out of scope for schemaVersion 1.
/// </summary>
public sealed record StructuralModalAnalysisRequest(
    StructuralModel Model,
    StructuralDynamicsDefinition Dynamics,
    int RequestedModeCount = 3)
{
    public StructuralModelValidationResult Validate()
    {
        ArgumentNullException.ThrowIfNull(Model);
        ArgumentNullException.ThrowIfNull(Dynamics);
        if (RequestedModeCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(RequestedModeCount), "Requested mode count must be positive.");
        return new StructuralDynamicsValidator().Validate(Model, Dynamics);
    }
}
