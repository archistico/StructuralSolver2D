namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Stores the displacement result of a structural node.
/// Internal units: Ux and Uy in meters [m], Rz in radians [rad].
/// </summary>
public sealed record NodalDisplacementResult(
    string NodeId,
    double Ux,
    double Uy,
    double Rz);
