namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Contains minimum, maximum and maximum absolute sampled internal-force values for one member.
/// </summary>
public sealed class MemberInternalForceExtrema
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberInternalForceExtrema"/> class.
    /// </summary>
    public MemberInternalForceExtrema(
        string memberId,
        InternalForceExtreme minNormalForce,
        InternalForceExtreme maxNormalForce,
        InternalForceExtreme maxAbsNormalForce,
        InternalForceExtreme minShearForce,
        InternalForceExtreme maxShearForce,
        InternalForceExtreme maxAbsShearForce,
        InternalForceExtreme minBendingMoment,
        InternalForceExtreme maxBendingMoment,
        InternalForceExtreme maxAbsBendingMoment)
    {
        MemberId = memberId;
        MinNormalForce = minNormalForce;
        MaxNormalForce = maxNormalForce;
        MaxAbsNormalForce = maxAbsNormalForce;
        MinShearForce = minShearForce;
        MaxShearForce = maxShearForce;
        MaxAbsShearForce = maxAbsShearForce;
        MinBendingMoment = minBendingMoment;
        MaxBendingMoment = maxBendingMoment;
        MaxAbsBendingMoment = maxAbsBendingMoment;
    }

    /// <summary>
    /// Gets the member identifier.
    /// </summary>
    public string MemberId { get; }

    /// <summary>
    /// Gets the minimum sampled normal force N [kN].
    /// </summary>
    public InternalForceExtreme MinNormalForce { get; }

    /// <summary>
    /// Gets the maximum sampled normal force N [kN].
    /// </summary>
    public InternalForceExtreme MaxNormalForce { get; }

    /// <summary>
    /// Gets the maximum absolute sampled normal force N [kN].
    /// </summary>
    public InternalForceExtreme MaxAbsNormalForce { get; }

    /// <summary>
    /// Gets the minimum sampled shear force V [kN].
    /// </summary>
    public InternalForceExtreme MinShearForce { get; }

    /// <summary>
    /// Gets the maximum sampled shear force V [kN].
    /// </summary>
    public InternalForceExtreme MaxShearForce { get; }

    /// <summary>
    /// Gets the maximum absolute sampled shear force V [kN].
    /// </summary>
    public InternalForceExtreme MaxAbsShearForce { get; }

    /// <summary>
    /// Gets the minimum sampled bending moment M [kNm].
    /// </summary>
    public InternalForceExtreme MinBendingMoment { get; }

    /// <summary>
    /// Gets the maximum sampled bending moment M [kNm].
    /// </summary>
    public InternalForceExtreme MaxBendingMoment { get; }

    /// <summary>
    /// Gets the maximum absolute sampled bending moment M [kNm].
    /// </summary>
    public InternalForceExtreme MaxAbsBendingMoment { get; }

    /// <summary>
    /// Computes extrema from one sampled internal-force diagram.
    /// </summary>
    /// <param name="diagram">Sampled internal-force diagram.</param>
    /// <returns>Internal-force extrema for the diagram member.</returns>
    public static MemberInternalForceExtrema FromDiagram(MemberInternalForceDiagram diagram)
    {
        ArgumentNullException.ThrowIfNull(diagram);

        if (diagram.Samples.Count == 0)
        {
            throw new InvalidOperationException("Cannot compute extrema from an empty internal-force diagram.");
        }

        return new MemberInternalForceExtrema(
            diagram.MemberId,
            ToExtreme(diagram.Samples.MinBy(sample => sample.NormalForce)!, sample => sample.NormalForce),
            ToExtreme(diagram.Samples.MaxBy(sample => sample.NormalForce)!, sample => sample.NormalForce),
            ToExtreme(diagram.Samples.MaxBy(sample => Math.Abs(sample.NormalForce))!, sample => sample.NormalForce),
            ToExtreme(diagram.Samples.MinBy(sample => sample.ShearForce)!, sample => sample.ShearForce),
            ToExtreme(diagram.Samples.MaxBy(sample => sample.ShearForce)!, sample => sample.ShearForce),
            ToExtreme(diagram.Samples.MaxBy(sample => Math.Abs(sample.ShearForce))!, sample => sample.ShearForce),
            ToExtreme(diagram.Samples.MinBy(sample => sample.BendingMoment)!, sample => sample.BendingMoment),
            ToExtreme(diagram.Samples.MaxBy(sample => sample.BendingMoment)!, sample => sample.BendingMoment),
            ToExtreme(diagram.Samples.MaxBy(sample => Math.Abs(sample.BendingMoment))!, sample => sample.BendingMoment));
    }

    private static InternalForceExtreme ToExtreme(
        MemberInternalForceSample sample,
        Func<MemberInternalForceSample, double> valueSelector) =>
        new(sample.MemberId, sample.Position, sample.Distance, valueSelector(sample));
}
