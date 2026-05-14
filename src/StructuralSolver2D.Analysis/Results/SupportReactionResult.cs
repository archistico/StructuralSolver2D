namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Stores the reaction components associated with a support.
/// Internal units: Fx and Fy in kilonewton [kN], Mz in kilonewton meter [kNm].
/// </summary>
public sealed record SupportReactionResult(
    string SupportId,
    string NodeId,
    double Fx,
    double Fy,
    double Mz);
