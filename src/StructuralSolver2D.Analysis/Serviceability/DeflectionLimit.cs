namespace StructuralSolver2D.Analysis.Serviceability;

/// <summary>
/// Defines a preliminary serviceability deflection limit in the form L / denominator.
/// </summary>
public sealed record DeflectionLimit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeflectionLimit"/> record.
    /// </summary>
    /// <param name="denominator">Limit denominator. For example, 250 means L/250.</param>
    /// <param name="direction">Displacement component checked against the limit.</param>
    public DeflectionLimit(
        double denominator,
        DeflectionCheckDirection direction = DeflectionCheckDirection.LocalY)
    {
        if (!double.IsFinite(denominator) || denominator <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator), denominator, "The limit denominator must be a positive finite value.");
        }

        Denominator = denominator;
        Direction = direction;
    }

    /// <summary>
    /// Gets the denominator of the L/denominator limit.
    /// </summary>
    public double Denominator { get; }

    /// <summary>
    /// Gets the displacement component checked against the limit.
    /// </summary>
    public DeflectionCheckDirection Direction { get; }

    /// <summary>
    /// Computes the allowed absolute deflection for the specified reference length.
    /// </summary>
    /// <param name="referenceLength">Reference length in meters.</param>
    /// <returns>Allowed absolute deflection in meters.</returns>
    public double GetAllowedDeflection(double referenceLength)
    {
        if (!double.IsFinite(referenceLength) || referenceLength <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(referenceLength), referenceLength, "The reference length must be a positive finite value.");
        }

        return referenceLength / Denominator;
    }

    /// <inheritdoc />
    public override string ToString() => $"L/{Denominator:0.###}";
}
