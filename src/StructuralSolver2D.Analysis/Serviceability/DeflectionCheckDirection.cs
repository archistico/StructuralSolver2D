namespace StructuralSolver2D.Analysis.Serviceability;

/// <summary>
/// Defines which sampled displacement component is used by a preliminary deflection check.
/// </summary>
public enum DeflectionCheckDirection
{
    /// <summary>
    /// Checks the absolute local transverse displacement of each member.
    /// This is usually the most useful component for beam-like members.
    /// </summary>
    LocalY,

    /// <summary>
    /// Checks the absolute local axial displacement of each member.
    /// </summary>
    LocalX,

    /// <summary>
    /// Checks the absolute global horizontal displacement.
    /// </summary>
    GlobalX,

    /// <summary>
    /// Checks the absolute global vertical displacement.
    /// </summary>
    GlobalY,
}
