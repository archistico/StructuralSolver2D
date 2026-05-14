namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Stores one sampled displacement point along a frame member.
/// Local displacements are expressed in the member coordinate system; global displacements are expressed in the global model axes.
/// Internal units: displacements in meters [m], rotation in radians [rad].
/// </summary>
public sealed record MemberDisplacementSample(
    string MemberId,
    double NormalizedPosition,
    double Distance,
    double LocalUx,
    double LocalUy,
    double LocalRz,
    double GlobalUx,
    double GlobalUy);
