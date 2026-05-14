namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Represents a significant point on a sampled internal-force diagram.
/// </summary>
/// <param name="MemberId">Identifier of the member where the point occurs.</param>
/// <param name="Kind">Reason why the point is characteristic.</param>
/// <param name="Quantity">Associated internal-force quantity.</param>
/// <param name="Position">Normalized position along the member, from 0.0 to 1.0.</param>
/// <param name="Distance">Distance from the start node in meters [m].</param>
/// <param name="Value">Associated value. Units depend on <paramref name="Quantity"/>.</param>
/// <param name="Description">Human-readable explanation of the point.</param>
public sealed record InternalForceCharacteristicPoint(
    string MemberId,
    InternalForceCharacteristicPointKind Kind,
    InternalForceQuantity Quantity,
    double Position,
    double Distance,
    double Value,
    string Description);
