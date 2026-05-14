namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Contains sampled internal-force values for one member.
/// </summary>
public sealed class MemberInternalForceDiagram
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberInternalForceDiagram"/> class.
    /// </summary>
    /// <param name="memberId">Identifier of the sampled member.</param>
    /// <param name="length">Member length in meters [m].</param>
    /// <param name="samples">Internal-force samples ordered from start node to end node.</param>
    public MemberInternalForceDiagram(
        string memberId,
        double length,
        IReadOnlyList<MemberInternalForceSample> samples)
    {
        MemberId = memberId;
        Length = length;
        Samples = samples;
    }

    /// <summary>
    /// Gets the sampled member identifier.
    /// </summary>
    public string MemberId { get; }

    /// <summary>
    /// Gets the member length in meters [m].
    /// </summary>
    public double Length { get; }

    /// <summary>
    /// Gets the internal-force samples ordered from start node to end node.
    /// </summary>
    public IReadOnlyList<MemberInternalForceSample> Samples { get; }

    /// <summary>
    /// Gets the maximum absolute normal force among the samples.
    /// </summary>
    public double MaxAbsNormalForce => Samples.Count == 0 ? 0 : Samples.Max(sample => Math.Abs(sample.NormalForce));

    /// <summary>
    /// Gets the maximum absolute shear force among the samples.
    /// </summary>
    public double MaxAbsShearForce => Samples.Count == 0 ? 0 : Samples.Max(sample => Math.Abs(sample.ShearForce));

    /// <summary>
    /// Gets the maximum absolute bending moment among the samples.
    /// </summary>
    public double MaxAbsBendingMoment => Samples.Count == 0 ? 0 : Samples.Max(sample => Math.Abs(sample.BendingMoment));

    /// <summary>
    /// Gets the sample closest to the requested normalized position.
    /// </summary>
    /// <param name="position">Normalized position, from 0.0 to 1.0.</param>
    /// <returns>The closest available sample.</returns>
    public MemberInternalForceSample GetClosestSample(double position)
    {
        if (Samples.Count == 0)
        {
            throw new InvalidOperationException("The diagram does not contain any samples.");
        }

        return Samples.OrderBy(sample => Math.Abs(sample.Position - position)).First();
    }
}
