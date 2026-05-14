namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Identifies the internal-force quantity associated with a characteristic diagram point.
/// </summary>
public enum InternalForceQuantity
{
    /// <summary>
    /// Axial force N [kN].
    /// </summary>
    NormalForce,

    /// <summary>
    /// Shear force V [kN].
    /// </summary>
    ShearForce,

    /// <summary>
    /// Bending moment M [kNm].
    /// </summary>
    BendingMoment,

    /// <summary>
    /// The point is not tied to a single internal-force quantity.
    /// </summary>
    Multiple,
}
