namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Contains sampled displacement values along one frame member.
/// The current implementation uses the standard finite-element interpolation of nodal displacements.
/// It is suitable for drawing the deformed shape, but internal values are not necessarily identical
/// to closed-form beam deflections caused by distributed loads unless the point is explicitly modeled as a node.
/// </summary>
public sealed class MemberDisplacementDiagram
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberDisplacementDiagram"/> class.
    /// </summary>
    public MemberDisplacementDiagram(
        string memberId,
        double length,
        IReadOnlyList<MemberDisplacementSample> samples)
    {
        MemberId = memberId;
        Length = length;
        Samples = samples;
    }

    /// <summary>
    /// Gets the member identifier.
    /// </summary>
    public string MemberId { get; }

    /// <summary>
    /// Gets the member length in meters.
    /// </summary>
    public double Length { get; }

    /// <summary>
    /// Gets sampled displacement values.
    /// </summary>
    public IReadOnlyList<MemberDisplacementSample> Samples { get; }

    /// <summary>
    /// Gets the maximum absolute global horizontal displacement among the samples.
    /// </summary>
    public double MaxAbsGlobalUx => Samples.Count == 0 ? 0.0 : Samples.Max(sample => Math.Abs(sample.GlobalUx));

    /// <summary>
    /// Gets the maximum absolute global vertical displacement among the samples.
    /// </summary>
    public double MaxAbsGlobalUy => Samples.Count == 0 ? 0.0 : Samples.Max(sample => Math.Abs(sample.GlobalUy));

    /// <summary>
    /// Returns the sample closest to the requested normalized position.
    /// </summary>
    /// <param name="normalizedPosition">Normalized position from 0.0 at the start node to 1.0 at the end node.</param>
    /// <returns>The closest available sample.</returns>
    public MemberDisplacementSample GetClosestSample(double normalizedPosition)
    {
        if (Samples.Count == 0)
        {
            throw new InvalidOperationException("The diagram does not contain any samples.");
        }

        return Samples
            .OrderBy(sample => Math.Abs(sample.NormalizedPosition - normalizedPosition))
            .First();
    }
}
