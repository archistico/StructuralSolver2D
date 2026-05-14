namespace StructuralSolver2D.Analysis.Serviceability;

/// <summary>
/// Contains the result of a preliminary serviceability deflection check for one member.
/// </summary>
public sealed record DeflectionCheckResult(
    string MemberId,
    DeflectionCheckDirection Direction,
    double LimitDenominator,
    double ReferenceLength,
    double AllowedDeflection,
    double MaxAbsDeflection,
    double SignedDeflectionAtCriticalSample,
    double NormalizedPosition,
    double Distance,
    DeflectionCheckStatus Status)
{
    /// <summary>
    /// Gets the demand/capacity ratio. Values up to 1.0 pass the preliminary check.
    /// </summary>
    public double UtilizationRatio => AllowedDeflection == 0.0 ? double.PositiveInfinity : MaxAbsDeflection / AllowedDeflection;

    /// <summary>
    /// Gets a value indicating whether the preliminary deflection check passes.
    /// </summary>
    public bool IsPass => Status == DeflectionCheckStatus.Pass;
}
