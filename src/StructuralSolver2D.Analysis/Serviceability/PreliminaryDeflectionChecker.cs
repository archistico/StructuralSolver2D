using StructuralSolver2D.Analysis.Results;

namespace StructuralSolver2D.Analysis.Serviceability;

/// <summary>
/// Performs preliminary serviceability deflection checks on sampled member displacement diagrams.
/// </summary>
/// <remarks>
/// This checker is intentionally limited to a simple sampled check of the form maximum deflection <= L / denominator.
/// It is an engineering aid for early feedback and must not be described as a complete code-compliant verification.
/// </remarks>
public sealed class PreliminaryDeflectionChecker
{
    private const double DefaultTolerance = 1e-12;

    /// <summary>
    /// Checks all supplied member displacement diagrams against the specified preliminary limit.
    /// </summary>
    /// <param name="displacementDiagrams">Sampled member displacement diagrams.</param>
    /// <param name="limit">Preliminary deflection limit.</param>
    /// <param name="tolerance">Absolute numerical tolerance used when comparing values near the limit.</param>
    /// <returns>One check result per member displacement diagram.</returns>
    public IReadOnlyList<DeflectionCheckResult> Check(
        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams,
        DeflectionLimit limit,
        double tolerance = DefaultTolerance)
    {
        ArgumentNullException.ThrowIfNull(displacementDiagrams);
        ArgumentNullException.ThrowIfNull(limit);

        if (!double.IsFinite(tolerance) || tolerance < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, "The tolerance must be a non-negative finite value.");
        }

        return displacementDiagrams
            .Select(diagram => Check(diagram, limit, tolerance))
            .ToList();
    }

    /// <summary>
    /// Checks one member displacement diagram against the specified preliminary limit.
    /// </summary>
    /// <param name="diagram">Sampled member displacement diagram.</param>
    /// <param name="limit">Preliminary deflection limit.</param>
    /// <param name="tolerance">Absolute numerical tolerance used when comparing values near the limit.</param>
    /// <returns>The preliminary check result for the member.</returns>
    public DeflectionCheckResult Check(
        MemberDisplacementDiagram diagram,
        DeflectionLimit limit,
        double tolerance = DefaultTolerance)
    {
        ArgumentNullException.ThrowIfNull(diagram);
        ArgumentNullException.ThrowIfNull(limit);

        if (!double.IsFinite(tolerance) || tolerance < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, "The tolerance must be a non-negative finite value.");
        }

        if (diagram.Samples.Count == 0)
        {
            throw new ArgumentException("The displacement diagram must contain at least one sample.", nameof(diagram));
        }

        if (!double.IsFinite(diagram.Length) || diagram.Length <= 0.0)
        {
            throw new ArgumentException("The displacement diagram length must be a positive finite value.", nameof(diagram));
        }

        MemberDisplacementSample criticalSample = diagram.Samples
            .OrderByDescending(sample => Math.Abs(GetDisplacement(sample, limit.Direction)))
            .First();

        double signedDeflection = GetDisplacement(criticalSample, limit.Direction);
        double maxAbsDeflection = Math.Abs(signedDeflection);
        double allowedDeflection = limit.GetAllowedDeflection(diagram.Length);
        DeflectionCheckStatus status = maxAbsDeflection <= allowedDeflection + tolerance
            ? DeflectionCheckStatus.Pass
            : DeflectionCheckStatus.Fail;

        return new DeflectionCheckResult(
            diagram.MemberId,
            limit.Direction,
            limit.Denominator,
            diagram.Length,
            allowedDeflection,
            maxAbsDeflection,
            signedDeflection,
            criticalSample.NormalizedPosition,
            criticalSample.Distance,
            status);
    }

    private static double GetDisplacement(MemberDisplacementSample sample, DeflectionCheckDirection direction) =>
        direction switch
        {
            DeflectionCheckDirection.LocalY => sample.LocalUy,
            DeflectionCheckDirection.LocalX => sample.LocalUx,
            DeflectionCheckDirection.GlobalX => sample.GlobalUx,
            DeflectionCheckDirection.GlobalY => sample.GlobalUy,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported deflection check direction."),
        };
}
