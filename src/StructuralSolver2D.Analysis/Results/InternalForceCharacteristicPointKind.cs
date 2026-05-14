namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Describes why an internal-force diagram point is considered characteristic.
/// </summary>
public enum InternalForceCharacteristicPointKind
{
    /// <summary>
    /// Member start or end point.
    /// </summary>
    EndPoint,

    /// <summary>
    /// Zero crossing of N, V or M.
    /// </summary>
    ZeroCrossing,

    /// <summary>
    /// Minimum sampled value of N, V or M.
    /// </summary>
    SampledMinimum,

    /// <summary>
    /// Maximum sampled value of N, V or M.
    /// </summary>
    SampledMaximum,

    /// <summary>
    /// Maximum absolute sampled value of N, V or M.
    /// </summary>
    SampledMaximumAbsolute,

    /// <summary>
    /// Candidate bending-moment extremum detected from a zero shear point.
    /// </summary>
    BendingMomentExtremumCandidate,

    /// <summary>
    /// Candidate discontinuity detected from an abrupt change between adjacent samples.
    /// </summary>
    DiscontinuityCandidate,
}
