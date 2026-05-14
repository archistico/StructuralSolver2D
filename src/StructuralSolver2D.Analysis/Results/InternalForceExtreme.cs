namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Represents an extreme value of an internal-force quantity along a member.
/// Units depend on the represented quantity: forces are in kilonewton [kN], moments are in kilonewton meter [kNm].
/// </summary>
/// <param name="MemberId">Identifier of the member where the extreme value occurs.</param>
/// <param name="Position">Normalized position along the member, from 0.0 at the start node to 1.0 at the end node.</param>
/// <param name="Distance">Distance from the start node in meters [m].</param>
/// <param name="Value">Extreme value.</param>
public sealed record InternalForceExtreme(
    string MemberId,
    double Position,
    double Distance,
    double Value);
