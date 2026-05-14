namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Represents one sampled internal-force value along a structural member.
/// Internal units: normal and shear forces in kilonewton [kN], bending moment in kilonewton meter [kNm].
/// </summary>
/// <param name="MemberId">Identifier of the sampled member.</param>
/// <param name="Position">Normalized position along the member, from 0.0 at the start node to 1.0 at the end node.</param>
/// <param name="Distance">Distance from the start node in meters [m].</param>
/// <param name="NormalForce">Axial force N. Positive values indicate tension.</param>
/// <param name="ShearForce">Shear force V in the local member coordinate system.</param>
/// <param name="BendingMoment">Bending moment M. Positive values indicate sagging for a horizontal left-to-right member.</param>
public sealed record MemberInternalForceSample(
    string MemberId,
    double Position,
    double Distance,
    double NormalForce,
    double ShearForce,
    double BendingMoment);
