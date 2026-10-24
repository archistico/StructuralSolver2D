namespace StructuralSolver2D.Analysis.Results;

/// <summary>Side of a section at a concentrated member load. Values at a jump must not be averaged.</summary>
public enum SectionLimitM78
{
    Continuous,
    BeforePointLoad,
    AfterPointLoad,
}

/// <summary>
/// Exact stationary/end/jump candidate for a first-order linear Frame2D member under
/// uniform, linearly varying and concentrated member loads (including expanded self-weight).
/// </summary>
public sealed record ExactInternalForceExtremeM78(
    string MemberId,
    string ResultId,
    InternalForceQuantity Quantity,
    double Value,
    double Distance,
    double Position,
    SectionLimitM78 Side);

/// <summary>
/// Signed minimum, signed maximum and maximum magnitude for N, V and M.
/// Extrema are taken over both one-sided limits at interior concentrated forces.
/// The quantity's units are kN for N/V and kNm for M.
/// </summary>
public sealed class ExactMemberInternalForceExtremaM78
{
    public ExactMemberInternalForceExtremaM78(
        string memberId, string resultId,
        IReadOnlyList<ExactInternalForceExtremeM78> minima,
        IReadOnlyList<ExactInternalForceExtremeM78> maxima,
        IReadOnlyList<ExactInternalForceExtremeM78> maximumMagnitudes)
    {
        MemberId = memberId;
        ResultId = resultId;
        Minima = minima;
        Maxima = maxima;
        MaximumMagnitudes = maximumMagnitudes;
    }

    public string MemberId { get; }
    public string ResultId { get; }
    public IReadOnlyList<ExactInternalForceExtremeM78> Minima { get; }
    public IReadOnlyList<ExactInternalForceExtremeM78> Maxima { get; }
    public IReadOnlyList<ExactInternalForceExtremeM78> MaximumMagnitudes { get; }

    public ExactInternalForceExtremeM78 Minimum(InternalForceQuantity quantity) =>
        Minima.Single(extreme => extreme.Quantity == quantity);

    public ExactInternalForceExtremeM78 Maximum(InternalForceQuantity quantity) =>
        Maxima.Single(extreme => extreme.Quantity == quantity);

    public ExactInternalForceExtremeM78 MaximumAbsolute(InternalForceQuantity quantity) =>
        MaximumMagnitudes.Single(extreme => extreme.Quantity == quantity);
}
