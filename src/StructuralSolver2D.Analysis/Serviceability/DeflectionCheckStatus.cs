namespace StructuralSolver2D.Analysis.Serviceability;

/// <summary>
/// Represents the outcome of a preliminary deflection check.
/// </summary>
public enum DeflectionCheckStatus
{
    /// <summary>
    /// The sampled maximum deflection is within the selected preliminary limit.
    /// </summary>
    Pass,

    /// <summary>
    /// The sampled maximum deflection exceeds the selected preliminary limit.
    /// </summary>
    Fail,
}
