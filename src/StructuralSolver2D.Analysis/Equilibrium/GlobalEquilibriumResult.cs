namespace StructuralSolver2D.Analysis.Equilibrium;

/// <summary>
/// Stores the global equilibrium residuals of a completed structural analysis.
/// Internal units: forces in kilonewton [kN], moments in kilonewton meter [kNm].
/// </summary>
public sealed record GlobalEquilibriumResult(
    string ResultId,
    double ReferenceX,
    double ReferenceY,
    double AppliedFx,
    double AppliedFy,
    double AppliedMz,
    double ReactionFx,
    double ReactionFy,
    double ReactionMz)
{
    /// <summary>
    /// Gets the residual horizontal force: applied loads plus reactions.
    /// </summary>
    public double ResidualFx => AppliedFx + ReactionFx;

    /// <summary>
    /// Gets the residual vertical force: applied loads plus reactions.
    /// </summary>
    public double ResidualFy => AppliedFy + ReactionFy;

    /// <summary>
    /// Gets the residual global moment about the selected reference point: applied loads plus reactions.
    /// </summary>
    public double ResidualMz => AppliedMz + ReactionMz;

    /// <summary>
    /// Gets the maximum absolute force residual.
    /// </summary>
    public double MaxAbsForceResidual => Math.Max(Math.Abs(ResidualFx), Math.Abs(ResidualFy));

    /// <summary>
    /// Gets the absolute moment residual.
    /// </summary>
    public double AbsMomentResidual => Math.Abs(ResidualMz);

    /// <summary>
    /// Returns true if all residual components are within the supplied absolute tolerance.
    /// </summary>
    public bool IsInEquilibrium(double tolerance) =>
        Math.Abs(ResidualFx) <= tolerance &&
        Math.Abs(ResidualFy) <= tolerance &&
        Math.Abs(ResidualMz) <= tolerance;
}
