namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Stores local member end forces for a 2D frame element.
/// Internal units: axial/shear forces in kilonewton [kN], moments in kilonewton meter [kNm].
/// </summary>
public sealed record MemberEndForceResult(
    string MemberId,
    double StartAxial,
    double StartShear,
    double StartMoment,
    double EndAxial,
    double EndShear,
    double EndMoment);
