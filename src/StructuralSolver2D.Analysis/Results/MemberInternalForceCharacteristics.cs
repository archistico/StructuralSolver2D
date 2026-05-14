namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Contains characteristic internal-force diagram points for one member.
/// </summary>
public sealed class MemberInternalForceCharacteristics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberInternalForceCharacteristics"/> class.
    /// </summary>
    /// <param name="memberId">Member identifier.</param>
    /// <param name="length">Member length in meters [m].</param>
    /// <param name="points">Characteristic points ordered by distance and kind.</param>
    public MemberInternalForceCharacteristics(
        string memberId,
        double length,
        IReadOnlyList<InternalForceCharacteristicPoint> points)
    {
        MemberId = memberId;
        Length = length;
        Points = points;
    }

    /// <summary>
    /// Gets the member identifier.
    /// </summary>
    public string MemberId { get; }

    /// <summary>
    /// Gets the member length in meters [m].
    /// </summary>
    public double Length { get; }

    /// <summary>
    /// Gets the detected characteristic points.
    /// </summary>
    public IReadOnlyList<InternalForceCharacteristicPoint> Points { get; }
}
